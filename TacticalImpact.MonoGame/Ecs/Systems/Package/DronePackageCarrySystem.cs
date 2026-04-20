using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DronePackageCarrySystem : ISystem
{
    private bool _hasPendingPickup;
    private int _pendingPickupDroneEntity;
    private int _pendingPickupPackageEntity;

    private bool _hasPendingCarryCommand;
    private bool _pendingCarrySelectedOnly;
    private bool _pendingCarryDropAtDestination;
    private Vector3 _pendingCarryTarget;

    public void QueuePickupCommand(int droneEntity, int packageEntity)
    {
        _pendingPickupDroneEntity = droneEntity;
        _pendingPickupPackageEntity = packageEntity;
        _hasPendingPickup = true;
    }

    public void QueueCarryMoveCommand(Vector3 groundPoint, bool selectedOnly, bool dropAtDestination)
    {
        _pendingCarryTarget = groundPoint;
        _pendingCarrySelectedOnly = selectedOnly;
        _pendingCarryDropAtDestination = dropAtDestination;
        _hasPendingCarryCommand = true;
    }

    public bool IsDroneCarryingPackage(EcsWorld world, int droneEntity)
    {
        if (!world.HasComponent<DroneCarryComponent>(droneEntity))
        {
            return false;
        }

        return world.GetComponent<DroneCarryComponent>(droneEntity).CarriedPackageEntity != -1;
    }

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        ProcessPickupCommand(world);
        ProcessCarryCommand(world);

        foreach (var entity in world.Query<TransformComponent, DroneTargetComponent, DroneCarryComponent>())
        {
            if (!world.HasComponent<DroneRenderComponent>(entity))
            {
                continue;
            }

            var transform = world.GetComponent<TransformComponent>(entity);
            var target = world.GetComponent<DroneTargetComponent>(entity);
            var carry = world.GetComponent<DroneCarryComponent>(entity);
            var droneRender = world.GetComponent<DroneRenderComponent>(entity);

            switch (carry.State)
            {
                case DroneCarryState.Idle:
                    continue;

                case DroneCarryState.ApproachingPickup:
                    HandleApproachingPickup(world, entity, transform, target, carry, droneRender);
                    break;

                case DroneCarryState.AscendingWithPackage:
                    HandleAscendingWithPackage(world, transform, target, carry, droneRender);
                    break;

                case DroneCarryState.Carrying:
                    HandleCarrying(world, transform, target, carry, droneRender);
                    break;

                case DroneCarryState.ApproachingDrop:
                    HandleApproachingDrop(world, transform, target, carry, droneRender);
                    break;

                case DroneCarryState.DescendingToDrop:
                    HandleDescendingToDrop(world, transform, target, carry, droneRender);
                    break;

                case DroneCarryState.AscendingAfterDrop:
                    HandleAscendingAfterDrop(transform, target, carry);
                    break;
            }
        }
    }

    private void ProcessPickupCommand(EcsWorld world)
    {
        if (!_hasPendingPickup)
        {
            return;
        }

        _hasPendingPickup = false;

        if (!world.HasComponent<DroneCarryComponent>(_pendingPickupDroneEntity) ||
            !world.HasComponent<PackageComponent>(_pendingPickupPackageEntity))
        {
            return;
        }

        var package = world.GetComponent<PackageComponent>(_pendingPickupPackageEntity);
        if (package.IsCarried)
        {
            return;
        }

        var carry = world.GetComponent<DroneCarryComponent>(_pendingPickupDroneEntity);
        if (carry.CarriedPackageEntity != -1)
        {
            return;
        }

        carry.TargetPackageEntity = _pendingPickupPackageEntity;
        carry.State = DroneCarryState.ApproachingPickup;
    }

    private void ProcessCarryCommand(EcsWorld world)
    {
        if (!_hasPendingCarryCommand)
        {
            return;
        }

        _hasPendingCarryCommand = false;
        var carryTarget = new Vector3(_pendingCarryTarget.X, 0f, _pendingCarryTarget.Z);

        if (_pendingCarrySelectedOnly)
        {
            foreach (var entity in world.Query<DroneCarryComponent, DroneSelectionComponent>())
            {
                var selection = world.GetComponent<DroneSelectionComponent>(entity);
                if (!selection.IsSelected)
                {
                    continue;
                }

                QueueCarryDestination(world, entity, carryTarget, _pendingCarryDropAtDestination);
            }

            return;
        }

        foreach (var entity in world.Query<DroneCarryComponent>())
        {
            QueueCarryDestination(world, entity, carryTarget, _pendingCarryDropAtDestination);
        }
    }

    private static void QueueCarryDestination(EcsWorld world, int entity, Vector3 carryTarget, bool dropAtDestination)
    {
        var carry = world.GetComponent<DroneCarryComponent>(entity);
        if (carry.CarriedPackageEntity == -1)
        {
            return;
        }

        carry.CarryTargetPosition = new Vector3(carryTarget.X, carry.SecureHeight, carryTarget.Z);

        carry.State = dropAtDestination
            ? DroneCarryState.ApproachingDrop
            : DroneCarryState.Carrying;
    }

    private static void HandleApproachingPickup(
        EcsWorld world,
        int droneEntity,
        TransformComponent transform,
        DroneTargetComponent target,
        DroneCarryComponent carry,
        DroneRenderComponent droneRender)
    {
        if (!world.HasComponent<TransformComponent>(carry.TargetPackageEntity) ||
            !world.HasComponent<PackageComponent>(carry.TargetPackageEntity) ||
            !world.HasComponent<PackageRenderComponent>(carry.TargetPackageEntity))
        {
            ResetCarry(carry);
            return;
        }

        var package = world.GetComponent<PackageComponent>(carry.TargetPackageEntity);
        if (package.IsCarried)
        {
            ResetCarry(carry);
            return;
        }

        var packageTransform = world.GetComponent<TransformComponent>(carry.TargetPackageEntity);
        var packageRender = world.GetComponent<PackageRenderComponent>(carry.TargetPackageEntity);
        var attachOffset = ComputeAttachOffset(droneRender, packageRender);

        var desiredPosition = new Vector3(packageTransform.Position.X, packageTransform.Position.Y + attachOffset, packageTransform.Position.Z);
        target.TargetPosition = desiredPosition;

        var horizontalDistance = new Vector2(transform.Position.X - desiredPosition.X, transform.Position.Z - desiredPosition.Z).Length();
        var verticalDistance = MathF.Abs(transform.Position.Y - desiredPosition.Y);
        if (horizontalDistance > carry.PickupTolerance || verticalDistance > carry.PickupTolerance)
        {
            return;
        }

        package.IsCarried = true;
        package.CarrierEntity = droneEntity;
        carry.CarriedPackageEntity = carry.TargetPackageEntity;
        carry.TargetPackageEntity = -1;
        carry.CarryTargetPosition = new Vector3(transform.Position.X, carry.SecureHeight, transform.Position.Z);
        carry.State = DroneCarryState.AscendingWithPackage;
    }

    private static void HandleAscendingWithPackage(
        EcsWorld world,
        TransformComponent transform,
        DroneTargetComponent target,
        DroneCarryComponent carry,
        DroneRenderComponent droneRender)
    {
        if (!TryGetCarriedPackage(world, carry, out var packageTransform, out var packageRender, out _))
        {
            ResetCarry(carry);
            return;
        }

        target.TargetPosition = new Vector3(transform.Position.X, carry.SecureHeight, transform.Position.Z);
        SyncCarriedPackage(transform, packageTransform, droneRender, packageRender);

        if (MathF.Abs(transform.Position.Y - carry.SecureHeight) <= 0.14f)
        {
            carry.CarryTargetPosition = new Vector3(transform.Position.X, carry.SecureHeight, transform.Position.Z);
            carry.State = DroneCarryState.Carrying;
        }
    }

    private static void HandleCarrying(
        EcsWorld world,
        TransformComponent transform,
        DroneTargetComponent target,
        DroneCarryComponent carry,
        DroneRenderComponent droneRender)
    {
        if (!TryGetCarriedPackage(world, carry, out var packageTransform, out var packageRender, out _))
        {
            ResetCarry(carry);
            return;
        }

        target.TargetPosition = carry.CarryTargetPosition;
        SyncCarriedPackage(transform, packageTransform, droneRender, packageRender);
    }

    private static void HandleApproachingDrop(
        EcsWorld world,
        TransformComponent transform,
        DroneTargetComponent target,
        DroneCarryComponent carry,
        DroneRenderComponent droneRender)
    {
        if (!TryGetCarriedPackage(world, carry, out var packageTransform, out var packageRender, out _))
        {
            ResetCarry(carry);
            return;
        }

        target.TargetPosition = carry.CarryTargetPosition;
        SyncCarriedPackage(transform, packageTransform, droneRender, packageRender);

        var horizontalDistance = new Vector2(
            transform.Position.X - carry.CarryTargetPosition.X,
            transform.Position.Z - carry.CarryTargetPosition.Z).Length();

        if (horizontalDistance <= 0.18f)
        {
            carry.State = DroneCarryState.DescendingToDrop;
        }
    }

    private static void HandleDescendingToDrop(
        EcsWorld world,
        TransformComponent transform,
        DroneTargetComponent target,
        DroneCarryComponent carry,
        DroneRenderComponent droneRender)
    {
        if (!TryGetCarriedPackage(world, carry, out var packageTransform, out var packageRender, out var package))
        {
            ResetCarry(carry);
            return;
        }

        var droneDropHeight = ComputeDropReleaseDroneHeight(droneRender, packageRender);
        target.TargetPosition = new Vector3(carry.CarryTargetPosition.X, droneDropHeight, carry.CarryTargetPosition.Z);
        SyncCarriedPackage(transform, packageTransform, droneRender, packageRender);

        if (MathF.Abs(transform.Position.Y - droneDropHeight) > carry.DropSnapTolerance)
        {
            return;
        }

        var packageHalfHeight = packageRender.Size.Y * 0.5f;
        var packagePosition = new Vector3(carry.CarryTargetPosition.X, packageHalfHeight, carry.CarryTargetPosition.Z);

        packageTransform.BasePosition = packagePosition;
        packageTransform.Position = packagePosition;
        package.IsCarried = false;
        package.CarrierEntity = -1;

        carry.CarriedPackageEntity = -1;
        carry.CarryTargetPosition = new Vector3(carry.CarryTargetPosition.X, carry.SecureHeight, carry.CarryTargetPosition.Z);
        carry.State = DroneCarryState.AscendingAfterDrop;
    }

    private static void HandleAscendingAfterDrop(
        TransformComponent transform,
        DroneTargetComponent target,
        DroneCarryComponent carry)
    {
        target.TargetPosition = carry.CarryTargetPosition;

        if (MathF.Abs(transform.Position.Y - carry.CarryTargetPosition.Y) <= 0.14f)
        {
            carry.State = DroneCarryState.Idle;
        }
    }

    private static bool TryGetCarriedPackage(
        EcsWorld world,
        DroneCarryComponent carry,
        out TransformComponent packageTransform,
        out PackageRenderComponent packageRender,
        out PackageComponent package)
    {
        packageTransform = null!;
        packageRender = null!;
        package = null!;

        if (carry.CarriedPackageEntity == -1 ||
            !world.HasComponent<TransformComponent>(carry.CarriedPackageEntity) ||
            !world.HasComponent<PackageRenderComponent>(carry.CarriedPackageEntity) ||
            !world.HasComponent<PackageComponent>(carry.CarriedPackageEntity))
        {
            return false;
        }

        packageTransform = world.GetComponent<TransformComponent>(carry.CarriedPackageEntity);
        packageRender = world.GetComponent<PackageRenderComponent>(carry.CarriedPackageEntity);
        package = world.GetComponent<PackageComponent>(carry.CarriedPackageEntity);
        return true;
    }

    private static void SyncCarriedPackage(
        TransformComponent droneTransform,
        TransformComponent packageTransform,
        DroneRenderComponent droneRender,
        PackageRenderComponent packageRender)
    {
        var attachOffset = ComputeAttachOffset(droneRender, packageRender);
        var attachedPosition = new Vector3(
            droneTransform.Position.X,
            droneTransform.Position.Y - attachOffset,
            droneTransform.Position.Z);

        packageTransform.BasePosition = attachedPosition;
        packageTransform.Position = attachedPosition;
    }

    private static float ComputeAttachOffset(DroneRenderComponent droneRender, PackageRenderComponent packageRender)
    {
        var droneHalfHeight = droneRender.Size * 0.5f;
        var packageHalfHeight = packageRender.Size.Y * 0.5f;
        return droneHalfHeight + packageHalfHeight + 0.05f;
    }

    private static float ComputeDropReleaseDroneHeight(DroneRenderComponent droneRender, PackageRenderComponent packageRender)
    {
        var droneHalfHeight = droneRender.Size * 0.5f;
        var packageHalfHeight = packageRender.Size.Y * 0.5f;
        return droneHalfHeight + packageHalfHeight * 2f + 0.05f;
    }

    private static void ResetCarry(DroneCarryComponent carry)
    {
        carry.State = DroneCarryState.Idle;
        carry.TargetPackageEntity = -1;
        carry.CarriedPackageEntity = -1;
    }
}
