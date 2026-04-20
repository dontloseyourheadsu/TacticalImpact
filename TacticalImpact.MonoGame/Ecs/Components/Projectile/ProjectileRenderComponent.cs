using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class ProjectileRenderComponent
{
    public float Radius { get; set; } = 0.2f;
    public Color Color { get; set; } = new Color(255, 186, 72);
}