using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs;
using TacticalImpact.MonoGame.Ecs.Components;
using TacticalImpact.MonoGame.Ecs.Systems;

namespace TacticalImpact.MonoGame.Features.Playground;

public sealed class PlaygroundFeatureModule
{
    public DroneSelectionSystem SelectionSystem { get; } = new();
    public DroneCommandSystem CommandSystem { get; } = new(2f);
    public DronePackageCarrySystem PackageCarrySystem { get; } = new();
    public DroneShootingSystem ShootingSystem { get; } = new();

    public IReadOnlyList<ISystem> CreateSystems()
    {
        return
        [
            SelectionSystem,
            CommandSystem,
            PackageCarrySystem,
            ShootingSystem,
            new DroneGroupMovementSystem(),
            new DronePhysicsSystem(),
            new DroneCollisionResolutionSystem(),
            new DroneZoneDetectionSystem(),
            new ProjectileMovementSystem(),
            new ProjectileLifetimeSystem()
        ];
    }

    public void InitializeEntities(EcsWorld world)
    {
        SpawnDrone(world, new Vector3(-3f, 2f, 0f), 0f);
        SpawnDrone(world, new Vector3(0f, 2f, 0f), 0f);
        SpawnDrone(world, new Vector3(3f, 2f, 0f), 0f);

        SpawnPackage(world, new Vector3(-5f, 0f, 3f));
        SpawnPackage(world, new Vector3(0f, 0f, 5f));
        SpawnPackage(world, new Vector3(5f, 0f, -2f));

        SpawnZone(world, new Vector3(0f, 0f, 0f), 2f, new Color(79, 177, 255), 1.0f);
        SpawnZone(world, new Vector3(-8f, 0f, 4f), 3.2f, new Color(109, 236, 143), 1.15f);
        SpawnZone(world, new Vector3(8f, 0f, -5f), 1.25f, new Color(255, 182, 74), 0.9f);
    }

    private static void SpawnDrone(EcsWorld world, Vector3 startPosition, float timeOffset)
    {
        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            BasePosition = startPosition,
            Position = startPosition
        });
        world.AddComponent(entity, new DroneTargetComponent
        {
            TargetPosition = startPosition
        });
        world.AddComponent(entity, new DroneMotionComponent
        {
            MoveDistance = 5.0f,
            MoveSpeed = 1.0f,
            FloatAmplitude = 0.5f,
            FloatSpeed = 2.0f,
            TimeOffset = timeOffset
        });
        world.AddComponent(entity, new DronePhysicsComponent
        {
            PositionGain = 3.2f,
            VelocityGain = 8.4f,
            LinearDamping = 2.8f
        });
        world.AddComponent(entity, new DroneRenderComponent
        {
            Size = 0.8f,
            BaseColor = new Color(79, 177, 255)
        });
        world.AddComponent(entity, new CollisionComponent
        {
            Layer = 1,
            CollidesWithMask = 1,
            Radius = 0.55f
        });
        world.AddComponent(entity, new DroneSelectionComponent
        {
            IsSelected = false
        });
        world.AddComponent(entity, new DroneZoneStatusComponent());
        world.AddComponent(entity, new DroneCarryComponent
        {
            CarryTargetPosition = startPosition
        });
        world.AddComponent(entity, new WeaponComponent
        {
            CooldownSeconds = 0.35f,
            ProjectileSpeed = 16f,
            ProjectileLifeSeconds = 2.25f,
            MuzzleHeight = 0.35f,
            CooldownRemaining = 0f
        });

        var motion = world.GetComponent<DroneMotionComponent>(entity);
        motion.MoveDistance = 0.14f;
        motion.MoveSpeed = 0.72f;
        motion.FloatAmplitude = 0.07f;
        motion.FloatSpeed = 1.45f;
    }

    private static void SpawnPackage(EcsWorld world, Vector3 groundPosition)
    {
        var render = new PackageRenderComponent();
        var packageHalfHeight = render.Size.Y * 0.5f;
        var placedPosition = new Vector3(groundPosition.X, packageHalfHeight, groundPosition.Z);

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            BasePosition = placedPosition,
            Position = placedPosition
        });
        world.AddComponent(entity, new PackageComponent());
        world.AddComponent(entity, new PackageZoneStatusComponent());
        world.AddComponent(entity, render);
    }

    private static void SpawnZone(EcsWorld world, Vector3 center, float radius, Color color, float height)
    {
        var entity = world.CreateEntity();
        world.AddComponent(entity, new ZoneComponent
        {
            Center = new Vector3(center.X, 0f, center.Z),
            Radius = radius,
            HoverMinHeight = 0.75f,
            HologramHeight = height,
            HologramColor = color
        });
    }
}