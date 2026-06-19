using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Weapon damage types
/// </summary>
public enum DamageType
{
    Kinetic,        // Physical projectiles - effective vs hull
    Energy,         // Lasers, plasma - effective vs shields
    Explosive,      // Missiles, torpedoes - area damage
    Thermal,        // Heat-based - bypasses some shields
    EMP,            // Disables systems temporarily
    Corrosive,      // Damage over time to hull
    Piercing,       // Ignores armor/resistance
    Quantum         // Bypasses normal defenses
}

/// <summary>
/// Weapon firing modes
/// </summary>
public enum FireMode
{
    Single,         // One shot per trigger pull
    Burst,          // 2-5 round burst
    Automatic,      // Continuous fire
    Charge,         // Hold to charge, release to fire
    Beam,           // Continuous beam
    Missile,        // Guided projectile
    Mine,           // Deployed stationary
    Drone           // Launches autonomous drone
}

/// <summary>
/// Projectile types for visual and physics effects
/// </summary>
public enum ProjectileType
{
    Bullet,
    Laser,
    Plasma,
    Missile,
    Torpedo,
    Railgun,
    Particle,
    Ion,
    Graviton,
    Drone
}

/// <summary>
/// Mount sizes for weapon hardpoints
/// </summary>
public enum WeaponMountSize
{
    Small,      // Size 1-2
    Medium,     // Size 3-4
    Large,      // Size 5-6
    Capital,    // Size 7-8
    Spinal      // Size 9+ (spinal mount)
}

/// <summary>
/// Weapon equipment - mounted on weapon hardpoints
/// </summary>
public sealed class Weapon : Equipment
{
    /// <summary>
    /// Damage per shot
    /// </summary>
    public int Damage { get; set; } = 10;

    /// <summary>
    /// Damage type
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Kinetic;

    /// <summary>
    /// Maximum effective range in meters
    /// </summary>
    public double Range { get; set; } = 1000;

    /// <summary>
    /// Optimal range (full damage)
    /// </summary>
    public double OptimalRange { get; set; } = 500;

    /// <summary>
    /// Falloff range (damage drops to 0)
    /// </summary>
    public double FalloffRange { get; set; } = 1500;

    /// <summary>
    /// Shots per second
    /// </summary>
    public double FireRate { get; set; } = 1.0;

    /// <summary>
    /// Firing mode
    /// </summary>
    public FireMode FireMode { get; set; } = FireMode.Single;

    /// <summary>
    /// Burst count (for burst fire)
    /// </summary>
    public int BurstCount { get; set; } = 3;

    /// <summary>
    /// Burst delay between shots (seconds)
    /// </summary>
    public double BurstDelay { get; set; } = 0.1;

    /// <summary>
    /// Energy cost per shot
    /// </summary>
    public double EnergyPerShot { get; set; } = 10;

    /// <summary>
    /// Projectile type
    /// </summary>
    public ProjectileType ProjectileType { get; set; } = ProjectileType.Bullet;

    /// <summary>
    /// Projectile speed (m/s)
    /// </summary>
    public double ProjectileSpeed { get; set; } = 500;

    /// <summary>
    /// Projectile lifetime (seconds)
    /// </summary>
    public double ProjectileLifetime { get; set; } = 5;

    /// <summary>
    /// Weapon mount size
    /// </summary>
    public WeaponMountSize MountSize { get; set; } = WeaponMountSize.Small;

    /// <summary>
    /// Accuracy (0.0 - 1.0, affects spread)
    /// </summary>
    public double Accuracy { get; set; } = 0.9;

    /// <summary>
    /// Tracking speed (degrees/second for turrets)
    /// </summary>
    public double TrackingSpeed { get; set; } = 60;

    /// <summary>
    /// Ammo capacity (0 = infinite/energy weapon)
    /// </summary>
    public int AmmoCapacity { get; set; } = 0;

    /// <summary>
    /// Current ammo
    /// </summary>
    public int CurrentAmmo { get; set; } = 0;

    /// <summary>
    /// Reload time (seconds)
    /// </summary>
    public double ReloadTime { get; set; } = 5;

    /// <summary>
    /// Whether weapon is currently reloading
    /// </summary>
    public bool IsReloading { get; set; } = false;

    /// <summary>
    /// Reload progress (0.0 - 1.0)
    /// </summary>
    public double ReloadProgress { get; set; } = 0;

    /// <summary>
    /// Spread angle (degrees) at optimal range
    /// </summary>
    public double Spread { get; set; } = 0.5;

    /// <summary>
    /// Damage falloff per meter beyond optimal range
    /// </summary>
    public double DamageFalloffPerMeter { get; set; } = 0.001;

    /// <summary>
    /// Shield damage multiplier
    /// </summary>
    public double ShieldDamageMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Hull damage multiplier
    /// </summary>
    public double HullDamageMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Critical hit chance (0.0 - 1.0)
    /// </summary>
    public double CritChance { get; set; } = 0.05;

    /// <summary>
    /// Critical hit damage multiplier
    /// </summary>
    public double CritMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Status effect chance (0.0 - 1.0)
    /// </summary>
    public double StatusEffectChance { get; set; } = 0;

    /// <summary>
    /// Status effect type
    /// </summary>
    public string StatusEffect { get; set; } = string.Empty;

    /// <summary>
    /// Status effect duration (seconds)
    /// </summary>
    public double StatusEffectDuration { get; set; } = 0;

    /// <summary>
    /// Minimum ship size for this weapon
    /// </summary>
    public new ShipSize MinimumShipSize { get; set; } = ShipSize.Tiny;

    /// <summary>
    /// Required reactor output (MW)
    /// </summary>
    public double RequiredReactorOutput { get; set; } = 0;

    /// <summary>
    /// Heat generated per shot
    /// </summary>
    public double HeatPerShot { get; set; } = 5;

    // ISaveable implementation
    public override string SaveId => $"weapon_{Id}";
    public override int SaveVersion => 2;

    /// <summary>
    /// Calculates DPS (damage per second)
    /// </summary>
    public double DPS => Damage * FireRate;

    /// <summary>
    /// Calculates effective DPS at range
    /// </summary>
    public double GetDPSAtRange(double distance)
    {
        if (distance <= OptimalRange)
            return DPS;

        if (distance >= FalloffRange)
            return 0;

        var falloffFactor = 1.0 - (distance - OptimalRange) / (FalloffRange - OptimalRange);
        return DPS * falloffFactor;
    }

    /// <summary>
    /// Calculates damage at range
    /// </summary>
    public double GetDamageAtRange(double distance)
    {
        if (distance <= OptimalRange)
            return Damage;

        if (distance >= FalloffRange)
            return 0;

        var falloffFactor = 1.0 - (distance - OptimalRange) / (FalloffRange - OptimalRange);
        return Damage * falloffFactor;
    }

    /// <summary>
    /// Checks if target is in range
    /// </summary>
    public bool IsInRange(double distance)
    {
        return distance <= Range;
    }

    /// <summary>
    /// Checks if target is in optimal range
    /// </summary>
    public bool IsInOptimalRange(double distance)
    {
        return distance <= OptimalRange;
    }

    /// <summary>
    /// Fires the weapon (consumes ammo/energy)
    /// </summary>
    public bool Fire()
    {
        if (IsReloading)
            return false;

        if (AmmoCapacity > 0 && CurrentAmmo <= 0)
        {
            StartReload();
            return false;
        }

        if (AmmoCapacity > 0)
            CurrentAmmo--;

        return true;
    }

    /// <summary>
    /// Starts reload
    /// </summary>
    public void StartReload()
    {
        if (AmmoCapacity > 0 && CurrentAmmo < AmmoCapacity)
        {
            IsReloading = true;
            ReloadProgress = 0;
        }
    }

    /// <summary>
    /// Updates reload progress
    /// </summary>
    public void UpdateReload(double deltaTime)
    {
        if (IsReloading)
        {
            ReloadProgress += deltaTime / ReloadTime;
            if (ReloadProgress >= 1.0)
            {
                IsReloading = false;
                ReloadProgress = 0;
                CurrentAmmo = AmmoCapacity;
            }
        }
    }

    /// <summary>
    /// Reloads instantly (for testing or special abilities)
    /// </summary>
    public void InstantReload()
    {
        CurrentAmmo = AmmoCapacity;
        IsReloading = false;
        ReloadProgress = 0;
    }

    public override JObject Serialize()
    {
        var baseData = base.Serialize();
        
        baseData["damage"] = Damage;
        baseData["damageType"] = DamageType.ToString();
        baseData["range"] = Range;
        baseData["optimalRange"] = OptimalRange;
        baseData["falloffRange"] = FalloffRange;
        baseData["fireRate"] = FireRate;
        baseData["fireMode"] = FireMode.ToString();
        baseData["burstCount"] = BurstCount;
        baseData["burstDelay"] = BurstDelay;
        baseData["energyPerShot"] = EnergyPerShot;
        baseData["projectileType"] = ProjectileType.ToString();
        baseData["projectileSpeed"] = ProjectileSpeed;
        baseData["projectileLifetime"] = ProjectileLifetime;
        baseData["mountSize"] = MountSize.ToString();
        baseData["accuracy"] = Accuracy;
        baseData["trackingSpeed"] = TrackingSpeed;
        baseData["ammoCapacity"] = AmmoCapacity;
        baseData["currentAmmo"] = CurrentAmmo;
        baseData["reloadTime"] = ReloadTime;
        baseData["isReloading"] = IsReloading;
        baseData["reloadProgress"] = ReloadProgress;
        baseData["spread"] = Spread;
        baseData["damageFalloffPerMeter"] = DamageFalloffPerMeter;
        baseData["shieldDamageMultiplier"] = ShieldDamageMultiplier;
        baseData["hullDamageMultiplier"] = HullDamageMultiplier;
        baseData["critChance"] = CritChance;
        baseData["critMultiplier"] = CritMultiplier;
        baseData["statusEffectChance"] = StatusEffectChance;
        baseData["statusEffect"] = StatusEffect;
        baseData["statusEffectDuration"] = StatusEffectDuration;
        baseData["minimumShipSize"] = MinimumShipSize.ToString();
        baseData["requiredReactorOutput"] = RequiredReactorOutput;
        baseData["heatPerShot"] = HeatPerShot;

        return baseData;
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        Damage = data["damage"]?.ToObject<int>() ?? 10;
        
        if (Enum.TryParse<DamageType>(data["damageType"]?.ToString(), out var dmgType))
            DamageType = dmgType;

        Range = data["range"]?.ToObject<double>() ?? 1000;
        OptimalRange = data["optimalRange"]?.ToObject<double>() ?? 500;
        FalloffRange = data["falloffRange"]?.ToObject<double>() ?? 1500;
        FireRate = data["fireRate"]?.ToObject<double>() ?? 1.0;

        if (Enum.TryParse<FireMode>(data["fireMode"]?.ToString(), out var fireMode))
            FireMode = fireMode;

        BurstCount = data["burstCount"]?.ToObject<int>() ?? 3;
        BurstDelay = data["burstDelay"]?.ToObject<double>() ?? 0.1;
        EnergyPerShot = data["energyPerShot"]?.ToObject<double>() ?? 10;

        if (Enum.TryParse<ProjectileType>(data["projectileType"]?.ToString(), out var projType))
            ProjectileType = projType;

        ProjectileSpeed = data["projectileSpeed"]?.ToObject<double>() ?? 500;
        ProjectileLifetime = data["projectileLifetime"]?.ToObject<double>() ?? 5;

        if (Enum.TryParse<WeaponMountSize>(data["mountSize"]?.ToString(), out var mountSize))
            MountSize = mountSize;

        Accuracy = data["accuracy"]?.ToObject<double>() ?? 0.9;
        TrackingSpeed = data["trackingSpeed"]?.ToObject<double>() ?? 60;
        AmmoCapacity = data["ammoCapacity"]?.ToObject<int>() ?? 0;
        CurrentAmmo = data["currentAmmo"]?.ToObject<int>() ?? 0;
        ReloadTime = data["reloadTime"]?.ToObject<double>() ?? 5;
        IsReloading = data["isReloading"]?.ToObject<bool>() ?? false;
        ReloadProgress = data["reloadProgress"]?.ToObject<double>() ?? 0;
        Spread = data["spread"]?.ToObject<double>() ?? 0.5;
        DamageFalloffPerMeter = data["damageFalloffPerMeter"]?.ToObject<double>() ?? 0.001;
        ShieldDamageMultiplier = data["shieldDamageMultiplier"]?.ToObject<double>() ?? 1.0;
        HullDamageMultiplier = data["hullDamageMultiplier"]?.ToObject<double>() ?? 1.0;
        CritChance = data["critChance"]?.ToObject<double>() ?? 0.05;
        CritMultiplier = data["critMultiplier"]?.ToObject<double>() ?? 2.0;
        StatusEffectChance = data["statusEffectChance"]?.ToObject<double>() ?? 0;
        StatusEffect = data["statusEffect"]?.ToString() ?? string.Empty;
        StatusEffectDuration = data["statusEffectDuration"]?.ToObject<double>() ?? 0;

        if (Enum.TryParse<ShipSize>(data["minimumShipSize"]?.ToString(), out var minSize))
            MinimumShipSize = minSize;

        RequiredReactorOutput = data["requiredReactorOutput"]?.ToObject<double>() ?? 0;
        HeatPerShot = data["heatPerShot"]?.ToObject<double>() ?? 5;

        // Set equipment type
        Type = EquipmentType.Weapon;
        MountType = MountType.Hardpoint;
    }

    public override Equipment Clone()
    {
        var clone = new Weapon
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Size = Size,
            MountType = MountType,
            Rarity = Rarity,
            Manufacturer = Manufacturer,
            Description = Description,
            BasePrice = BasePrice,
            Mass = Mass,
            PowerDraw = PowerDraw,
            HeatGeneration = HeatGeneration,
            MinimumShipSize = MinimumShipSize,
            RequiredReputationTier = RequiredReputationTier,
            RequiredFaction = RequiredFaction,
            Condition = Condition,
            Damage = Damage,
            DamageType = DamageType,
            Range = Range,
            OptimalRange = OptimalRange,
            FalloffRange = FalloffRange,
            FireRate = FireRate,
            FireMode = FireMode,
            BurstCount = BurstCount,
            BurstDelay = BurstDelay,
            EnergyPerShot = EnergyPerShot,
            ProjectileType = ProjectileType,
            ProjectileSpeed = ProjectileSpeed,
            ProjectileLifetime = ProjectileLifetime,
            MountSize = MountSize,
            Accuracy = Accuracy,
            TrackingSpeed = TrackingSpeed,
            AmmoCapacity = AmmoCapacity,
            CurrentAmmo = CurrentAmmo,
            ReloadTime = ReloadTime,
            IsReloading = IsReloading,
            ReloadProgress = ReloadProgress,
            Spread = Spread,
            DamageFalloffPerMeter = DamageFalloffPerMeter,
            ShieldDamageMultiplier = ShieldDamageMultiplier,
            HullDamageMultiplier = HullDamageMultiplier,
            CritChance = CritChance,
            CritMultiplier = CritMultiplier,
            StatusEffectChance = StatusEffectChance,
            StatusEffect = StatusEffect,
            StatusEffectDuration = StatusEffectDuration,
            RequiredReactorOutput = RequiredReactorOutput,
            HeatPerShot = HeatPerShot
        };

        foreach (var tag in Tags)
            clone.Tags.Add(tag);

        foreach (var kvp in StatModifiers)
            clone.StatModifiers[kvp.Key] = kvp.Value;

        return clone;
    }

    public override bool Validate(out string error)
    {
        if (!base.Validate(out error))
            return false;

        if (Damage <= 0)
        {
            error = "Damage must be positive";
            return false;
        }

        if (Range <= 0)
        {
            error = "Range must be positive";
            return false;
        }

        if (OptimalRange <= 0 || OptimalRange > Range)
        {
            error = "Optimal range must be positive and <= range";
            return false;
        }

        if (FalloffRange < OptimalRange)
        {
            error = "Falloff range must be >= optimal range";
            return false;
        }

        if (FireRate <= 0)
        {
            error = "Fire rate must be positive";
            return false;
        }

        if (EnergyPerShot < 0)
        {
            error = "Energy per shot cannot be negative";
            return false;
        }

        if (ProjectileSpeed <= 0)
        {
            error = "Projectile speed must be positive";
            return false;
        }

        if (Accuracy < 0 || Accuracy > 1)
        {
            error = "Accuracy must be between 0 and 1";
            return false;
        }

        if (AmmoCapacity < 0)
        {
            error = "Ammo capacity cannot be negative";
            return false;
        }

        if (CurrentAmmo < 0 || CurrentAmmo > AmmoCapacity)
        {
            error = "Current ammo must be between 0 and capacity";
            return false;
        }

        if (ReloadTime < 0)
        {
            error = "Reload time cannot be negative";
            return false;
        }

        if (CritChance < 0 || CritChance > 1)
        {
            error = "Crit chance must be between 0 and 1";
            return false;
        }

        if (CritMultiplier <= 1)
        {
            error = "Crit multiplier must be > 1";
            return false;
        }

        error = string.Empty;
        return true;
    }
}