using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DronePhysicsSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        foreach (var entity in world.Query<TransformComponent, DroneTargetComponent, DronePhysicsComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            var target = world.GetComponent<DroneTargetComponent>(entity);
            var physics = world.GetComponent<DronePhysicsComponent>(entity);

            var error = target.TargetPosition - transform.Position;
            var desiredVelocity = error * physics.PositionGain;
            var velocityDiff = desiredVelocity - physics.Velocity;

            var acceleration = velocityDiff * physics.VelocityGain;
            physics.Velocity += acceleration * deltaTimeSeconds;
            transform.Position += physics.Velocity * deltaTimeSeconds;
        }
    }
}