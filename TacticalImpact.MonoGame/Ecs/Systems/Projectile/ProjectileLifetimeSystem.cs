using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class ProjectileLifetimeSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var toDestroy = new List<int>();

        foreach (var entity in world.Query<TransformComponent, ProjectileComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            var projectile = world.GetComponent<ProjectileComponent>(entity);
            
            projectile.RemainingLifeSeconds -= deltaTimeSeconds;
            if (projectile.RemainingLifeSeconds <= 0f)
            {
                toDestroy.Add(entity);
                
                // Trigger area explosion if the projectile explodes on fuse expiry (e.g. Grenades)
                if (projectile.ExplodesOnExpiry)
                {
                    ProjectileCollisionSystem.TriggerExplosion(
                        world, 
                        transform.Position, 
                        3.6f, 
                        projectile.Damage, 
                        16f, 
                        projectile.ShooterEntity);
                }
            }
        }

        for (var i = 0; i < toDestroy.Count; i++)
        {
            world.DestroyEntity(toDestroy[i]);
        }
    }
}