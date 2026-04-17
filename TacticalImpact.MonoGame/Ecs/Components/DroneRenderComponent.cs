using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class DroneRenderComponent
{
    public float Size { get; set; } = 1f;
    public Color BaseColor { get; set; } = new Color(34, 153, 255);
}