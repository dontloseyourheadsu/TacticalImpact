using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class ProjectileMovementSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        foreach (var entity in world.Query<TransformComponent, ProjectileComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            var projectile = world.GetComponent<ProjectileComponent>(entity);
            transform.Position += projectile.Velocity * deltaTimeSeconds;
        }
    }
}