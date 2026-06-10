using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class ProjectileMovementSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var toDestroy = new List<int>();

        foreach (var entity in world.Query<TransformComponent, ProjectileComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            var projectile = world.GetComponent<ProjectileComponent>(entity);
            
            // Move projectile
            transform.Position += projectile.Velocity * deltaTimeSeconds;

            // Ground collision detection (ground is at Y = 0)
            if (transform.Position.Y <= 0.05f)
            {
                toDestroy.Add(entity);

                // Trigger a ground explosion if this is a plasma cannon shell
                if (!projectile.IsLightProjectile)
                {
                    // Create contact position on the ground surface
                    var contactPos = new Vector3(transform.Position.X, 0.01f, transform.Position.Z);
                    ProjectileCollisionSystem.TriggerExplosion(
                        world, 
                        contactPos, 
                        3.2f, 
                        projectile.Damage, 
                        15f, 
                        projectile.ShooterEntity);
                }
            }
        }

        // Clean up exploded/expired projectiles
        for (var i = 0; i < toDestroy.Count; i++)
        {
            world.DestroyEntity(toDestroy[i]);
        }
    }
}