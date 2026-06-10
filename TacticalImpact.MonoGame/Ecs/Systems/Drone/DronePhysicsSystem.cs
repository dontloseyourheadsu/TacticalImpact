using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DronePhysicsSystem : ISystem
{
    private float _timeAccumulator;
    
    // Globally exposed wind value for HUD/UI rendering
    public static Vector3 CurrentWind { get; private set; } = Vector3.Zero;

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        _timeAccumulator += deltaTimeSeconds;

        // Base wind direction: blowing along X axis with slight Z component
        var baseWind = new Vector3(2.5f, 0f, 0.6f);
        
        // Dynamic gust oscillations
        var gustStrength = MathF.Sin(_timeAccumulator * 0.4f) * 2.0f; // Slow drift
        var noiseStrength = MathF.Cos(_timeAccumulator * 1.8f) * 0.5f; // Fast turbulence
        
        CurrentWind = baseWind + new Vector3(gustStrength + noiseStrength, 0f, gustStrength * 0.3f);

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

            // Apply wind drift if the drone has statistics (mass/wind resistance)
            if (world.HasComponent<DroneStatsComponent>(entity))
            {
                var stats = world.GetComponent<DroneStatsComponent>(entity);
                // Wind acceleration = WindVelocity * (WindResistance / Weight)
                var windAcceleration = CurrentWind * (stats.WindResistance / stats.Weight);
                physics.Velocity += windAcceleration * deltaTimeSeconds;
            }

            var dampingFactor = MathF.Exp(-physics.LinearDamping * deltaTimeSeconds);
            physics.Velocity *= dampingFactor;
            transform.Position += physics.Velocity * deltaTimeSeconds;
        }
    }
}