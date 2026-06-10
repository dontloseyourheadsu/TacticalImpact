namespace TacticalImpact.MonoGame.Ecs.Components;

public enum WeaponType
{
    PlasmaCannon,  // Deals raw damage and causes explosive knockback
    BlindingLaser  // Emits intense light to blind enemy optical cameras
}

public sealed class WeaponComponent
{
    public WeaponType Type { get; set; } = WeaponType.PlasmaCannon;
    public float CooldownSeconds { get; set; } = 0.35f;
    public float ProjectileSpeed { get; set; } = 16f;
    public float ProjectileLifeSeconds { get; set; } = 2.25f;
    public float MuzzleHeight { get; set; } = 0.35f;
    public float CooldownRemaining { get; set; }
    
    // Weapon stats
    public float Damage { get; set; } = 15f;
    public float BlindDuration { get; set; } = 3.0f; // Applicable if BlindingLaser
}