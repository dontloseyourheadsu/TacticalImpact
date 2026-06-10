using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class ProjectileComponent
{
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public float RemainingLifeSeconds { get; set; } = 1f;
    
    // Projectile stats inherited from weapon
    public int ShooterEntity { get; set; } = -1;
    public float Damage { get; set; } = 0f;
    public bool IsLightProjectile { get; set; } = false;
    public float BlindDuration { get; set; } = 0f;
}