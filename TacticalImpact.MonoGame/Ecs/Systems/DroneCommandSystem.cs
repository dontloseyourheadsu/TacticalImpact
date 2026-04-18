using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneCommandSystem : ISystem
{
    private readonly float _hoverHeight;
    private bool _hasPendingCommand;
    private bool _selectedOnly;
    private Vector3 _pendingBasePosition;

    public DroneCommandSystem(float hoverHeight)
    {
        _hoverHeight = hoverHeight;
    }

    public void QueueMoveCommand(Vector3 groundPoint, bool selectedOnly)
    {
        _pendingBasePosition = new Vector3(groundPoint.X, _hoverHeight, groundPoint.Z);
        _selectedOnly = selectedOnly;
        _hasPendingCommand = true;
    }

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        if (!_hasPendingCommand)
        {
            return;
        }

        if (_selectedOnly)
        {
            foreach (var entity in world.Query<TransformComponent, DroneSelectionComponent>())
            {
                var selection = world.GetComponent<DroneSelectionComponent>(entity);
                if (!selection.IsSelected)
                {
                    continue;
                }

                var transform = world.GetComponent<TransformComponent>(entity);
                transform.BasePosition = _pendingBasePosition;
            }
        }
        else
        {
            var entities = new List<int>();
            foreach (var entity in world.Query<TransformComponent>())
            {
                entities.Add(entity);
            }

            entities.Sort();

            var spacing = GetFormationSpacing(world, entities);
            var offsets = BuildFormationOffsets(entities.Count, spacing);
            for (var i = 0; i < entities.Count; i++)
            {
                var transform = world.GetComponent<TransformComponent>(entities[i]);
                transform.BasePosition = _pendingBasePosition + offsets[i];
            }
        }

        _hasPendingCommand = false;
    }

    private static float GetFormationSpacing(EcsWorld world, IReadOnlyList<int> entities)
    {
        var maxRadius = 0.6f;

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!world.HasComponent<CollisionComponent>(entity))
            {
                continue;
            }

            var collision = world.GetComponent<CollisionComponent>(entity);
            maxRadius = MathF.Max(maxRadius, collision.Radius);
        }

        return maxRadius * 2.35f;
    }

    private static List<Vector3> BuildFormationOffsets(int count, float spacing)
    {
        var offsets = new List<Vector3>(count);
        if (count <= 0)
        {
            return offsets;
        }

        offsets.Add(Vector3.Zero);
        if (count == 1)
        {
            return offsets;
        }

        var placed = 1;
        var ringIndex = 1;

        while (placed < count)
        {
            var ringRadius = ringIndex * spacing;
            var ringSlots = Math.Max(6, ringIndex * 6);

            for (var i = 0; i < ringSlots && placed < count; i++)
            {
                var angle = MathHelper.TwoPi * i / ringSlots;
                offsets.Add(new Vector3(MathF.Cos(angle) * ringRadius, 0f, MathF.Sin(angle) * ringRadius));
                placed++;
            }

            ringIndex++;
        }

        return offsets;
    }
}