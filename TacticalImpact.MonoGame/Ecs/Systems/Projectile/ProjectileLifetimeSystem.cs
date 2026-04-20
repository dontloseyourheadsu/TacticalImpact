using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class ProjectileLifetimeSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var toDestroy = new List<int>();

        foreach (var entity in world.Query<ProjectileComponent>())
        {
            var projectile = world.GetComponent<ProjectileComponent>(entity);
            projectile.RemainingLifeSeconds -= deltaTimeSeconds;
            if (projectile.RemainingLifeSeconds <= 0f)
            {
                toDestroy.Add(entity);
            }
        }

        for (var i = 0; i < toDestroy.Count; i++)
        {
            world.DestroyEntity(toDestroy[i]);
        }
    }
}