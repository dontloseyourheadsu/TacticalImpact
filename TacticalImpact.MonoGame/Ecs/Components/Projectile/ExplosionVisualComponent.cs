using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class ExplosionVisualComponent
{
    public float MaxRadius { get; set; } = 3.0f;
    public float CurrentRadius { get; set; } = 0.1f;
    public float Age { get; set; } = 0f;
    public float MaxAge { get; set; } = 0.5f; // Short burst
    public Color Color { get; set; } = new Color(255, 160, 60); // Orange fire
}
