using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class DroneTargetComponent
{
    public required Vector3 TargetPosition { get; set; }
}