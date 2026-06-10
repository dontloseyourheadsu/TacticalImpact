using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class ProjectileCollisionSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var projectiles = new List<int>();
        foreach (var projEntity in world.Query<TransformComponent, ProjectileComponent, ProjectileRenderComponent>())
        {
            projectiles.Add(projEntity);
        }

        var drones = new List<int>();
        foreach (var droneEntity in world.Query<TransformComponent, CollisionComponent, DroneStatsComponent>())
        {
            drones.Add(droneEntity);
        }

        var toDestroyProjectiles = new HashSet<int>();

        for (var i = 0; i < projectiles.Count; i++)
        {
            var projEntity = projectiles[i];
            var projTransform = world.GetComponent<TransformComponent>(projEntity);
            var projComp = world.GetComponent<ProjectileComponent>(projEntity);
            var projRender = world.GetComponent<ProjectileRenderComponent>(projEntity);

            for (var j = 0; j < drones.Count; j++)
            {
                var droneEntity = drones[j];
                
                // Do not collide with the shooter of this projectile
                if (droneEntity == projComp.ShooterEntity)
                {
                    continue;
                }

                var droneTransform = world.GetComponent<TransformComponent>(droneEntity);
                var droneCollision = world.GetComponent<CollisionComponent>(droneEntity);

                var distance = Vector3.Distance(projTransform.Position, droneTransform.Position);
                var minDistance = projRender.Radius + droneCollision.Radius;

                if (distance < minDistance)
                {
                    // Collision detected!
                    ApplyImpact(world, droneEntity, projComp);
                    toDestroyProjectiles.Add(projEntity);
                    break; // Projectile can only hit one drone
                }
            }
        }

        foreach (var projEntity in toDestroyProjectiles)
        {
            world.DestroyEntity(projEntity);
        }
    }

    private static void ApplyImpact(EcsWorld world, int droneEntity, ProjectileComponent projectile)
    {
        var stats = world.GetComponent<DroneStatsComponent>(droneEntity);
        
        // 1. Damage calculations
        if (projectile.Damage > 0f)
        {
            var damageToShield = MathF.Min(stats.CurrentShield, projectile.Damage);
            stats.CurrentShield -= damageToShield;
            var remainingDamage = projectile.Damage - damageToShield;
            stats.CurrentHealth = MathF.Max(0f, stats.CurrentHealth - remainingDamage);
            
            // Reset shield recharge cooldown
            stats.ShieldCooldown = stats.ShieldRechargeDelay;
        }

        // 2. Blinding effects
        if (projectile.IsLightProjectile && world.HasComponent<DroneSensorComponent>(droneEntity))
        {
            var sensor = world.GetComponent<DroneSensorComponent>(droneEntity);
            var duration = projectile.BlindDuration;
            
            if (sensor.HasAntiGlareFilter)
            {
                duration *= 0.25f; // Anti-glare filter reduces duration by 75%
            }

            sensor.BlindedDurationRemaining = MathF.Max(sensor.BlindedDurationRemaining, duration);
        }
    }
}
