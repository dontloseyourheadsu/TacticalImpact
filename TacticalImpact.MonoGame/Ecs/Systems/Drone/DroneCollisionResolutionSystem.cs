using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneCollisionResolutionSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<TransformComponent, CollisionComponent, DronePhysicsComponent>())
        {
            entities.Add(entity);
        }

        for (var i = 0; i < entities.Count; i++)
        {
            var entityA = entities[i];
            var transformA = world.GetComponent<TransformComponent>(entityA);
            var collisionA = world.GetComponent<CollisionComponent>(entityA);
            var physicsA = world.GetComponent<DronePhysicsComponent>(entityA);

            for (var j = i + 1; j < entities.Count; j++)
            {
                var entityB = entities[j];
                var transformB = world.GetComponent<TransformComponent>(entityB);
                var collisionB = world.GetComponent<CollisionComponent>(entityB);
                var physicsB = world.GetComponent<DronePhysicsComponent>(entityB);

                if (!CanCollide(collisionA, collisionB))
                {
                    continue;
                }

                ResolvePair(transformA, physicsA, collisionA, transformB, physicsB, collisionB, entityA, entityB);
            }
        }
    }

    private static bool CanCollide(CollisionComponent a, CollisionComponent b)
    {
        var aHitsB = (a.CollidesWithMask & b.Layer) != 0;
        var bHitsA = (b.CollidesWithMask & a.Layer) != 0;
        return aHitsB && bHitsA;
    }

    private static void ResolvePair(
        TransformComponent transformA,
        DronePhysicsComponent physicsA,
        CollisionComponent collisionA,
        TransformComponent transformB,
        DronePhysicsComponent physicsB,
        CollisionComponent collisionB,
        int entityA,
        int entityB)
    {
        var delta = transformB.Position - transformA.Position;
        var distance = delta.Length();
        var minDistance = collisionA.Radius + collisionB.Radius;

        if (distance >= minDistance)
        {
            return;
        }

        var separationDirection = distance > 0.0001f
            ? delta / distance
            : BuildFallbackDirection(entityA, entityB);

        var overlap = minDistance - Math.Max(distance, 0.0001f);
        var separation = separationDirection * (overlap * 0.5f);

        transformA.Position -= separation;
        transformB.Position += separation;

        var damp = 0.25f;
        physicsA.Velocity -= separationDirection * damp;
        physicsB.Velocity += separationDirection * damp;
    }

    private static Vector3 BuildFallbackDirection(int entityA, int entityB)
    {
        var seed = (entityA * 92821) ^ (entityB * 68917);
        var x = ((seed & 0xFF) / 255f) - 0.5f;
        var z = (((seed >> 8) & 0xFF) / 255f) - 0.5f;
        var fallback = new Vector3(x, 0f, z);
        return fallback.LengthSquared() > 0.0001f ? Vector3.Normalize(fallback) : Vector3.Right;
    }
}