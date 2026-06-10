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

    public void TriggerReload(EcsWorld world, int droneEntity)
    {
        if (world.HasComponent<WeaponComponent>(droneEntity))
        {
            var weapon = world.GetComponent<WeaponComponent>(droneEntity);
            if (!weapon.IsReloading && weapon.CurrentAmmo < weapon.MaxAmmo)
            {
                weapon.IsReloading = true;
                weapon.ReloadRemainingSeconds = weapon.ReloadTimeSeconds;
            }
        }
    }

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        // 1. Process weapon cooldowns and reloading states
        foreach (var entity in world.Query<WeaponComponent>())
        {
            var weapon = world.GetComponent<WeaponComponent>(entity);
            weapon.CooldownRemaining = MathF.Max(0f, weapon.CooldownRemaining - deltaTimeSeconds);

            if (weapon.IsReloading)
            {
                weapon.ReloadRemainingSeconds = MathF.Max(0f, weapon.ReloadRemainingSeconds - deltaTimeSeconds);
                if (weapon.ReloadRemainingSeconds <= 0f)
                {
                    weapon.CurrentAmmo = weapon.MaxAmmo;
                    weapon.IsReloading = false;
                }
            }
        }

        if (!_hasPendingShot)
        {
            return;
        }

        // 2. Perform shooting command processing
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
        
        // Block shooting while reloading
        if (weapon.IsReloading)
        {
            return;
        }

        // Trigger auto-reload if out of ammunition
        if (weapon.CurrentAmmo <= 0)
        {
            weapon.IsReloading = true;
            weapon.ReloadRemainingSeconds = weapon.ReloadTimeSeconds;
            return;
        }

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

        // Apply blindness penalty to shooting dispersion
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

        // Target locking for homing missiles
        int homingTarget = -1;
        if (weapon.Type == WeaponType.MissileLauncher)
        {
            float closestDist = float.MaxValue;
            foreach (var otherDrone in world.Query<TransformComponent, DroneStatsComponent>())
            {
                if (otherDrone == shooterEntity)
                {
                    continue;
                }

                var otherPos = world.GetComponent<TransformComponent>(otherDrone).Position;
                var dist = Vector3.Distance(_pendingTarget, otherPos);
                
                // Locks onto the drone closest to the click point (up to 15.0m away)
                if (dist < closestDist && dist < 15.0f)
                {
                    closestDist = dist;
                    homingTarget = otherDrone;
                }
            }
        }

        // Deduct ammunition
        weapon.CurrentAmmo--;

        var projectileEntity = world.CreateEntity();
        world.AddComponent(projectileEntity, new TransformComponent
        {
            BasePosition = spawnPosition,
            Position = spawnPosition
        });

        // Configure projectile launch velocity
        var launchVelocity = direction * weapon.ProjectileSpeed;
        if (weapon.Type == WeaponType.GrenadeLauncher)
        {
            // Lob the grenade upwards slightly
            launchVelocity = new Vector3(launchVelocity.X, launchVelocity.Y + 5.5f, launchVelocity.Z);
        }

        world.AddComponent(projectileEntity, new ProjectileComponent
        {
            Velocity = launchVelocity,
            RemainingLifeSeconds = weapon.ProjectileLifeSeconds,
            ShooterEntity = shooterEntity,
            Damage = weapon.Damage,
            IsLightProjectile = weapon.Type == WeaponType.BlindingLaser,
            BlindDuration = weapon.BlindDuration,
            HomingTargetEntity = homingTarget,
            IsGrenade = weapon.Type == WeaponType.GrenadeLauncher,
            ExplodesOnExpiry = weapon.Type == WeaponType.GrenadeLauncher
        });

        // Set visual properties and sizes based on weapon type
        Color color;
        float radius;
        switch (weapon.Type)
        {
            case WeaponType.BlindingLaser:
                color = new Color(255, 245, 120); // Bright Yellow
                radius = 0.12f;
                break;
            case WeaponType.MachineGun:
                color = new Color(200, 240, 255); // Cyan tracer
                radius = 0.09f;
                break;
            case WeaponType.MissileLauncher:
                color = new Color(100, 255, 120); // Glowing green engine
                radius = 0.25f;
                break;
            case WeaponType.GrenadeLauncher:
                color = new Color(150, 150, 150); // Metallic gray
                radius = 0.16f;
                break;
            case WeaponType.PlasmaCannon:
            default:
                color = new Color(255, 110, 50); // Red/Orange plasma
                radius = 0.22f;
                break;
        }

        world.AddComponent(projectileEntity, new ProjectileRenderComponent
        {
            Radius = radius,
            Color = color
        });

        weapon.CooldownRemaining = weapon.CooldownSeconds;

        // Auto-reload immediately if we fired the last round
        if (weapon.CurrentAmmo <= 0)
        {
            weapon.IsReloading = true;
            weapon.ReloadRemainingSeconds = weapon.ReloadTimeSeconds;
        }
    }
}