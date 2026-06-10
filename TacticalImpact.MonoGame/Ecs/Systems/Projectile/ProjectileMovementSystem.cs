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

            // 1. Homing missile steering
            if (projectile.HomingTargetEntity != -1)
            {
                if (world.HasComponent<TransformComponent>(projectile.HomingTargetEntity))
                {
                    var targetTransform = world.GetComponent<TransformComponent>(projectile.HomingTargetEntity);
                    var toTarget = targetTransform.Position - transform.Position;
                    
                    if (toTarget.LengthSquared() > 0.01f)
                    {
                        var desiredDirection = Vector3.Normalize(toTarget);
                        var currentSpeed = projectile.Velocity.Length();
                        
                        // Smoothly steer current velocity towards desired target direction
                        var steerForce = 4.5f; // Steering rotation rate
                        var currentDir = Vector3.Normalize(projectile.Velocity);
                        var newDir = Vector3.Normalize(Vector3.Lerp(currentDir, desiredDirection, steerForce * deltaTimeSeconds));
                        
                        projectile.Velocity = newDir * currentSpeed;
                    }
                }
                else
                {
                    // Target was destroyed; lose lock
                    projectile.HomingTargetEntity = -1;
                }
            }

            // 2. Ballistic grenade physics
            if (projectile.IsGrenade)
            {
                // Apply gravity force
                var gravity = new Vector3(0f, -9.8f, 0f);
                projectile.Velocity += gravity * deltaTimeSeconds;
            }

            // Move projectile
            transform.Position += projectile.Velocity * deltaTimeSeconds;

            // 3. Ground collision and bouncing
            if (transform.Position.Y <= 0.05f)
            {
                if (projectile.IsGrenade)
                {
                    // Grenade bouncing
                    if (projectile.Velocity.Y < 0f)
                    {
                        transform.Position = new Vector3(transform.Position.X, 0.05f, transform.Position.Z);
                        
                        // Reverse vertical velocity with damping (elastic coefficient = 0.45)
                        var bounceY = -projectile.Velocity.Y * 0.45f;
                        
                        // Apply friction to horizontal sliding (friction coefficient = 0.75)
                        var slideX = projectile.Velocity.X * 0.75f;
                        var slideZ = projectile.Velocity.Z * 0.75f;
                        
                        projectile.Velocity = new Vector3(slideX, bounceY, slideZ);
                    }
                }
                else
                {
                    // Other projectiles explode or disintegrate immediately on ground impact
                    toDestroy.Add(entity);

                    if (!projectile.IsLightProjectile)
                    {
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
        }

        // Clean up exploded/expired projectiles
        for (var i = 0; i < toDestroy.Count; i++)
        {
            world.DestroyEntity(toDestroy[i]);
        }
    }
}