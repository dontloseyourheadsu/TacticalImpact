using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class TransformComponent
{
    public required Vector3 BasePosition { get; set; }
    public required Vector3 Position { get; set; }
}