using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneSelectionSystem : ISystem
{
    public bool HasSelection { get; private set; }

    public void Select(EcsWorld world, int selectedEntity)
    {
        var found = false;

        foreach (var entity in world.Query<DroneSelectionComponent>())
        {
            var selection = world.GetComponent<DroneSelectionComponent>(entity);
            var isSelected = entity == selectedEntity;
            selection.IsSelected = isSelected;
            found |= isSelected;
        }

        HasSelection = found;
    }

    public void ClearSelection(EcsWorld world)
    {
        foreach (var entity in world.Query<DroneSelectionComponent>())
        {
            var selection = world.GetComponent<DroneSelectionComponent>(entity);
            selection.IsSelected = false;
        }

        HasSelection = false;
    }

    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var hasAny = false;
        foreach (var entity in world.Query<DroneSelectionComponent>())
        {
            if (world.GetComponent<DroneSelectionComponent>(entity).IsSelected)
            {
                hasAny = true;
                break;
            }
        }

        HasSelection = hasAny;
    }
}