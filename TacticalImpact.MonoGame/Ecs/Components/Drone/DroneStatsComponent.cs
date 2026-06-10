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

    // Upgrade levels (starting at 1)
    public int HealthUpgradeLevel { get; set; } = 1;
    public int ShieldUpgradeLevel { get; set; } = 1;
    public int ArmorUpgradeLevel { get; set; } = 1;
    public int EngineUpgradeLevel { get; set; } = 1;

    public void UpgradeHealth()
    {
        HealthUpgradeLevel++;
        var prevMax = MaxHealth;
        MaxHealth = MathF.Round(MaxHealth * 1.25f);
        CurrentHealth += (MaxHealth - prevMax); // Heal for the difference
    }

    public void UpgradeShield()
    {
        ShieldUpgradeLevel++;
        var prevMax = MaxShield;
        MaxShield = MathF.Round(MaxShield * 1.25f);
        CurrentShield += (MaxShield - prevMax); // Shield increase difference
        ShieldRechargeRate *= 1.2f;
    }

    public void UpgradeArmor()
    {
        ArmorUpgradeLevel++;
        // Armor increases weight/density slightly to resist pushbacks
        Weight *= 1.1f;
    }

    public void UpgradeEngine(EcsWorld world, int entity)
    {
        EngineUpgradeLevel++;
        
        if (world.HasComponent<DroneMotionComponent>(entity))
        {
            var motion = world.GetComponent<DroneMotionComponent>(entity);
            motion.MoveSpeed *= 1.22f;
        }
        
        if (world.HasComponent<DronePhysicsComponent>(entity))
        {
            var physics = world.GetComponent<DronePhysicsComponent>(entity);
            physics.PositionGain *= 1.15f;
            physics.VelocityGain *= 1.15f;
        }
    }
}
