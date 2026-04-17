using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs;
using TacticalImpact.MonoGame.Ecs.Components;
using TacticalImpact.MonoGame.Ecs.Systems;

namespace TacticalImpact.MonoGame.Features.Playground;

public sealed class PlaygroundFeatureModule
{
    public IReadOnlyList<ISystem> CreateSystems()
    {
        return
        [
            new DroneGroupMovementSystem(),
            new DronePhysicsSystem()
        ];
    }

    public void InitializeEntities(EcsWorld world)
    {
        SpawnDrone(world, new Vector3(-3f, 2f, 0f), 0f);
        SpawnDrone(world, new Vector3(0f, 2f, 0f), 0f);
        SpawnDrone(world, new Vector3(3f, 2f, 0f), 0f);
    }

    private static void SpawnDrone(EcsWorld world, Vector3 startPosition, float timeOffset)
    {
        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            BasePosition = startPosition,
            Position = startPosition
        });
        world.AddComponent(entity, new DroneTargetComponent
        {
            TargetPosition = startPosition
        });
        world.AddComponent(entity, new DroneMotionComponent
        {
            MoveDistance = 5.0f,
            MoveSpeed = 1.0f,
            FloatAmplitude = 0.5f,
            FloatSpeed = 2.0f,
            TimeOffset = timeOffset
        });
        world.AddComponent(entity, new DronePhysicsComponent
        {
            PositionGain = 2.0f,
            VelocityGain = 5.0f
        });
        world.AddComponent(entity, new DroneRenderComponent
        {
            Size = 0.8f,
            BaseColor = new Color(79, 177, 255)
        });
    }
}