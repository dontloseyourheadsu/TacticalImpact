using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class ProjectileComponent
{
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public float RemainingLifeSeconds { get; set; } = 1f;
}