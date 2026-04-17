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
            var transform = world.GetComponent<TransformComponent>(entity);
            var motion = world.GetComponent<DroneMotionComponent>(entity);
            var target = world.GetComponent<DroneTargetComponent>(entity);

            var zOffset = MathF.Sin((_timePassed + motion.TimeOffset) * motion.MoveSpeed) * motion.MoveDistance;
            var yOffset = MathF.Sin((_timePassed + motion.TimeOffset) * motion.FloatSpeed) * motion.FloatAmplitude;
            var movementVector = new Vector3(0f, yOffset, zOffset);

            target.TargetPosition = transform.BasePosition + movementVector;
        }
    }
}