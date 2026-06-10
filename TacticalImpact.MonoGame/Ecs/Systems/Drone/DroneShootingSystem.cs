using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneShootingSystem : ISystem
{
    private bool _hasPendingShot;
    private bool _selectedOnly;
    private Vector3 _pendingTarget;

    public void QueueShootCommand(Vector3 target, bool selectedOnly)
    {
        _pendingTarget = target;
        _selectedOnly = selectedOnly;
        _hasPendingShot = true;
    }

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        foreach (var entity in world.Query<WeaponComponent>())
        {
            var weapon = world.GetComponent<WeaponComponent>(entity);
            weapon.CooldownRemaining = MathF.Max(0f, weapon.CooldownRemaining - deltaTimeSeconds);
        }

        if (!_hasPendingShot)
        {
            return;
        }

        if (_selectedOnly)
        {
            var shooters = new List<int>();
            foreach (var entity in world.Query<TransformComponent, WeaponComponent, DroneSelectionComponent>())
            {
                shooters.Add(entity);
            }

            for (var i = 0; i < shooters.Count; i++)
            {
                var entity = shooters[i];
                var selection = world.GetComponent<DroneSelectionComponent>(entity);
                if (!selection.IsSelected)
                {
                    continue;
                }

                TryShoot(world, entity);
            }
        }
        else
        {
            var shooters = new List<int>();
            foreach (var entity in world.Query<TransformComponent, WeaponComponent>())
            {
                shooters.Add(entity);
            }

            for (var i = 0; i < shooters.Count; i++)
            {
                var entity = shooters[i];
                TryShoot(world, entity);
            }
        }

        _hasPendingShot = false;
    }

    private void TryShoot(EcsWorld world, int shooterEntity)
    {
        var weapon = world.GetComponent<WeaponComponent>(shooterEntity);
        if (weapon.CooldownRemaining > 0f)
        {
            return;
        }

        var shooterTransform = world.GetComponent<TransformComponent>(shooterEntity);
        var spawnPosition = shooterTransform.Position + new Vector3(0f, weapon.MuzzleHeight, 0f);
        var toTarget = _pendingTarget - spawnPosition;
        var direction = toTarget.LengthSquared() > 0.0001f
            ? Vector3.Normalize(toTarget)
            : Vector3.Forward;

        // Check if the shooter is fully blinded. If so, add significant dispersion to the shot direction.
        var isBlinded = world.HasComponent<DroneSensorComponent>(shooterEntity) &&
                        world.GetComponent<DroneSensorComponent>(shooterEntity).IsFullyBlinded;
        if (isBlinded)
        {
            var random = System.Random.Shared;
            var spread = new Vector3(
                (float)(random.NextDouble() - 0.5) * 0.75f,
                (float)(random.NextDouble() - 0.5) * 0.25f,
                (float)(random.NextDouble() - 0.5) * 0.75f
            );
            direction = Vector3.Normalize(direction + spread);
        }

        var projectileEntity = world.CreateEntity();
        world.AddComponent(projectileEntity, new TransformComponent
        {
            BasePosition = spawnPosition,
            Position = spawnPosition
        });
        world.AddComponent(projectileEntity, new ProjectileComponent
        {
            Velocity = direction * weapon.ProjectileSpeed,
            RemainingLifeSeconds = weapon.ProjectileLifeSeconds,
            ShooterEntity = shooterEntity,
            Damage = weapon.Damage,
            IsLightProjectile = weapon.Type == WeaponType.BlindingLaser,
            BlindDuration = weapon.BlindDuration
        });

        // Set projectile visual properties based on weapon type
        var color = weapon.Type == WeaponType.BlindingLaser 
            ? new Color(255, 245, 120) // Bright yellow light laser
            : new Color(255, 110, 50); // Red/Orange plasma charge

        var radius = weapon.Type == WeaponType.BlindingLaser ? 0.12f : 0.22f;

        world.AddComponent(projectileEntity, new ProjectileRenderComponent
        {
            Radius = radius,
            Color = color
        });

        weapon.CooldownRemaining = weapon.CooldownSeconds;
    }
}