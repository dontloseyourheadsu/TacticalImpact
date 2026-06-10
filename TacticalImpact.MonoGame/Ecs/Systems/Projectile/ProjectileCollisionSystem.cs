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
                    if (projComp.IsLightProjectile)
                    {
                        // Blinding lasers apply direct damage and blinding effect
                        ApplyDirectImpact(world, droneEntity, projComp);
                    }
                    else
                    {
                        // Plasma shells trigger explosive blast centered at the impact position
                        TriggerExplosion(world, projTransform.Position, 3.2f, projComp.Damage, 15f, projComp.ShooterEntity);
                    }
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

    private static void ApplyDirectImpact(EcsWorld world, int droneEntity, ProjectileComponent projectile)
    {
        var stats = world.GetComponent<DroneStatsComponent>(droneEntity);
        
        // 1. Damage calculations with Armor mitigation
        if (projectile.Damage > 0f)
        {
            // Armor formula: 1 / (1 + 0.20 * (ArmorLevel - 1))
            var mitigation = 1.0f / (1.0f + (stats.ArmorUpgradeLevel - 1) * 0.20f);
            var mitigatedDamage = projectile.Damage * mitigation;

            var damageToShield = MathF.Min(stats.CurrentShield, mitigatedDamage);
            stats.CurrentShield -= damageToShield;
            var remainingDamage = mitigatedDamage - damageToShield;
            stats.CurrentHealth = MathF.Max(0f, stats.CurrentHealth - remainingDamage);
            
            // Reset shield recharge cooldown
            stats.ShieldCooldown = stats.ShieldRechargeDelay;
        }

        // 2. Blinding effects
        if (world.HasComponent<DroneSensorComponent>(droneEntity))
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

    public static void TriggerExplosion(EcsWorld world, Vector3 position, float radius, float maxDamage, float force, int shooterEntity)
    {
        // 1. Deal damage and apply physics impulse to drones in radius
        var drones = new List<int>();
        foreach (var droneEntity in world.Query<TransformComponent, DronePhysicsComponent, DroneStatsComponent>())
        {
            drones.Add(droneEntity);
        }

        foreach (var droneEntity in drones)
        {
            var droneTransform = world.GetComponent<TransformComponent>(droneEntity);
            var stats = world.GetComponent<DroneStatsComponent>(droneEntity);
            var physics = world.GetComponent<DronePhysicsComponent>(droneEntity);

            var distance = Vector3.Distance(position, droneTransform.Position);
            if (distance > radius)
            {
                continue;
            }

            // Linear falloff based on distance
            var falloff = 1f - (distance / radius);
            
            // Damage calculations with Armor mitigation
            var damage = maxDamage * falloff;
            if (damage > 0f)
            {
                // Armor formula: 1 / (1 + 0.20 * (ArmorLevel - 1))
                var mitigation = 1.0f / (1.0f + (stats.ArmorUpgradeLevel - 1) * 0.20f);
                var mitigatedDamage = damage * mitigation;

                var damageToShield = MathF.Min(stats.CurrentShield, mitigatedDamage);
                stats.CurrentShield -= damageToShield;
                var remainingDamage = mitigatedDamage - damageToShield;
                stats.CurrentHealth = MathF.Max(0f, stats.CurrentHealth - remainingDamage);
                
                stats.ShieldCooldown = stats.ShieldRechargeDelay;
            }

            // Knockback Force (Velocity Impulse)
            var delta = droneTransform.Position - position;
            var direction = delta.LengthSquared() > 0.0001f
                ? Vector3.Normalize(delta)
                : Vector3.Up; // Push upwards if centered exactly
            
            // Impulse is inversely proportional to drone weight
            var knockbackImpulse = direction * (force * falloff / stats.Weight);
            physics.Velocity += knockbackImpulse;
        }

        // 2. Create the visual explosion entity
        var explosionEntity = world.CreateEntity();
        world.AddComponent(explosionEntity, new TransformComponent
        {
            BasePosition = position,
            Position = position
        });
        world.AddComponent(explosionEntity, new ExplosionVisualComponent
        {
            MaxRadius = radius,
            CurrentRadius = 0.15f,
            Age = 0f,
            MaxAge = 0.45f,
            Color = new Color(255, 120, 40)
        });
    }
}
