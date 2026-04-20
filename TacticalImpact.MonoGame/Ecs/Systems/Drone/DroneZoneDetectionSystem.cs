using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneZoneDetectionSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var zones = new List<int>();
        foreach (var zoneEntity in world.Query<ZoneComponent>())
        {
            zones.Add(zoneEntity);
        }

        foreach (var droneEntity in world.Query<TransformComponent, DroneZoneStatusComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(droneEntity);
            var status = world.GetComponent<DroneZoneStatusComponent>(droneEntity);
            EvaluateZoneForPosition(world, zones, transform.Position, enforceHoverHeight: true, out var zoneEntity);
            status.ZoneEntity = zoneEntity;
            status.IsInZone = zoneEntity != -1;
        }

        foreach (var packageEntity in world.Query<TransformComponent, PackageZoneStatusComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(packageEntity);
            var status = world.GetComponent<PackageZoneStatusComponent>(packageEntity);

            EvaluateZoneForPosition(world, zones, transform.Position, enforceHoverHeight: false, out var zoneEntity);
            status.ZoneEntity = zoneEntity;
            status.IsInZone = zoneEntity != -1;
        }
    }

    private static void EvaluateZoneForPosition(
        EcsWorld world,
        IReadOnlyList<int> zoneEntities,
        Vector3 position,
        bool enforceHoverHeight,
        out int matchedZoneEntity)
    {
        var positionXZ = new Vector2(position.X, position.Z);
        matchedZoneEntity = -1;
        var closestDistanceSq = float.MaxValue;

        for (var i = 0; i < zoneEntities.Count; i++)
        {
            var zoneEntity = zoneEntities[i];
            var zone = world.GetComponent<ZoneComponent>(zoneEntity);
            if (enforceHoverHeight && position.Y < zone.HoverMinHeight)
            {
                continue;
            }

            var zoneXZ = new Vector2(zone.Center.X, zone.Center.Z);
            var distanceSq = Vector2.DistanceSquared(positionXZ, zoneXZ);
            var radiusSq = zone.Radius * zone.Radius;

            if (distanceSq > radiusSq || distanceSq >= closestDistanceSq)
            {
                continue;
            }

            closestDistanceSq = distanceSq;
            matchedZoneEntity = zoneEntity;
        }
    }
}