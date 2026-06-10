namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class DroneStatsComponent
{
    // Health and Shield stats
    public float MaxHealth { get; set; } = 100f;
    public float CurrentHealth { get; set; } = 100f;
    
    public float MaxShield { get; set; } = 50f;
    public float CurrentShield { get; set; } = 50f;
    public float ShieldRechargeRate { get; set; } = 8f; // Units per second
    public float ShieldRechargeDelay { get; set; } = 3f; // Seconds before recharging starts after taking damage
    public float ShieldCooldown { get; set; } = 0f; // Time remaining before recharge can begin

    // Physics related stats
    public float Weight { get; set; } = 1f; // Higher weight reduces wind drift and explosion knockback
    public float WindResistance { get; set; } = 0.5f; // Coefficient of wind resistance (lower means less affected by wind)

    // Energy stats (for special abilities or sensors)
    public float MaxEnergy { get; set; } = 100f;
    public float CurrentEnergy { get; set; } = 100f;
    public float EnergyRechargeRate { get; set; } = 5f; // Units per second
}
