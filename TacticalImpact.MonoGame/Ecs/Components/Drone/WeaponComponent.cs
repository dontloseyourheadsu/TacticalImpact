namespace TacticalImpact.MonoGame.Ecs.Components;

public enum WeaponType
{
    PlasmaCannon,      // Deals raw damage and causes explosive knockback
    BlindingLaser,     // Emits intense light to blind enemy optical cameras
    MachineGun,        // Rapid-fire bullet shooter (high speed, lower damage)
    MissileLauncher,   // Fires homing missiles that lock onto enemy targets
    GrenadeLauncher    // Lobs bouncing grenades that explode after a fuse delay
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

    // Ammo / Reload mechanics
    public int MaxAmmo { get; set; } = 10;
    public int CurrentAmmo { get; set; } = 10;
    public float ReloadTimeSeconds { get; set; } = 2.0f;
    public float ReloadRemainingSeconds { get; set; } = 0f;
    public bool IsReloading { get; set; } = false;

    // Upgrades
    public int DamageUpgradeLevel { get; set; } = 1;
    public int FireRateUpgradeLevel { get; set; } = 1;
    public int AmmoUpgradeLevel { get; set; } = 1;

    public void UpgradeDamage()
    {
        DamageUpgradeLevel++;
        Damage = MathF.Round(Damage * 1.25f, 1);
        if (Type == WeaponType.BlindingLaser)
        {
            BlindDuration *= 1.15f;
        }
    }

    public void UpgradeFireRate()
    {
        FireRateUpgradeLevel++;
        CooldownSeconds = MathF.Max(0.05f, CooldownSeconds * 0.82f); // 18% cooldown reduction (faster firing)
    }

    public void UpgradeAmmo()
    {
        AmmoUpgradeLevel++;
        var prevMax = MaxAmmo;
        MaxAmmo = (int)MathF.Ceiling(MaxAmmo * 1.3f);
        CurrentAmmo += (MaxAmmo - prevMax); // refill the new ammo capacity slot
        ReloadTimeSeconds = MathF.Max(0.5f, ReloadTimeSeconds * 0.88f); // 12% faster reload speed
    }
}