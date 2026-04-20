namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class WeaponComponent
{
    public float CooldownSeconds { get; set; } = 0.35f;
    public float ProjectileSpeed { get; set; } = 16f;
    public float ProjectileLifeSeconds { get; set; } = 2.25f;
    public float MuzzleHeight { get; set; } = 0.35f;
    public float CooldownRemaining { get; set; }
}