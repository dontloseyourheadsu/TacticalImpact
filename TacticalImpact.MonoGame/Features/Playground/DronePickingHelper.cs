using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Features.Playground;

public static class DronePickingHelper
{
    public static bool TryPickDrone(EcsWorld world, Ray ray, out int pickedDrone)
    {
        pickedDrone = -1;
        var closestDistance = float.MaxValue;

        foreach (var entity in world.Query<TransformComponent, DroneRenderComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            var render = world.GetComponent<DroneRenderComponent>(entity);
            var halfExtent = new Vector3(render.Size * 0.5f);
            var bounds = new BoundingBox(transform.Position - halfExtent, transform.Position + halfExtent);
            var hitDistance = ray.Intersects(bounds);

            if (!hitDistance.HasValue || hitDistance.Value >= closestDistance)
            {
                continue;
            }

            closestDistance = hitDistance.Value;
            pickedDrone = entity;
        }

        return pickedDrone != -1;
    }

    public static bool TryIntersectGround(Ray ray, out Vector3 point)
    {
        point = Vector3.Zero;
        var denominator = Vector3.Dot(ray.Direction, Vector3.Up);
        if (MathF.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        var distance = -ray.Position.Y / denominator;
        if (distance <= 0f)
        {
            return false;
        }

        point = ray.Position + ray.Direction * distance;
        return true;
    }
}