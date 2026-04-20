using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneGroupMovementSystem : ISystem
{
    private float _timePassed;

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        _timePassed += deltaTimeSeconds;

        foreach (var entity in world.Query<TransformComponent, DroneMotionComponent, DroneTargetComponent>())
        {
            if (world.HasComponent<DroneCarryComponent>(entity))
            {
                var carry = world.GetComponent<DroneCarryComponent>(entity);
                if (carry.State != DroneCarryState.Idle)
                {
                    continue;
                }
            }

            var transform = world.GetComponent<TransformComponent>(entity);
            var motion = world.GetComponent<DroneMotionComponent>(entity);
            var target = world.GetComponent<DroneTargetComponent>(entity);

            var t = _timePassed + motion.TimeOffset;
            var xOffset = MathF.Sin(t * motion.MoveSpeed) * motion.MoveDistance;
            var zOffset = MathF.Cos(t * (motion.MoveSpeed * 0.87f) + motion.TimeOffset * 1.3f) * (motion.MoveDistance * 0.85f);
            var yOffset = MathF.Sin(t * motion.FloatSpeed) * motion.FloatAmplitude;
            var movementVector = new Vector3(xOffset, yOffset, zOffset);

            target.TargetPosition = transform.BasePosition + movementVector;
        }
    }
}