using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class ZoneComponent
{
    public required Vector3 Center { get; set; }
    public float Radius { get; set; } = 2f;
    public float HoverMinHeight { get; set; } = 0.8f;
    public float HologramHeight { get; set; } = 1.2f;
    public Color HologramColor { get; set; } = new(80, 255, 255);
}