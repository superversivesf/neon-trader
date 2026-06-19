using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Shield types
/// </summary>
public enum ShieldType
{
    Standard,       // Basic energy shield
    Regenerative,   // Fast recharge, lower capacity
    Hardened,       // High resistance, slow recharge
    Adaptive,       // Adapts to damage type
    Reflective,     // Reflects energy weapons
    Ablative,       // Absorbs damage, degrades over time
    Phase,          // Chance to phase through damage
    Resonant        // Resonates with specific frequencies
}

/// <summary>
/// Shield equipment - provides defensive protection
/// </summary>
public sealed class Shield : Equipment
{
    /// <summary>
    /// Maximum shield capacity
    /// </summary>
    public int Capacity { get; set; } = 100;

    /// <summary>
    /// Shield recharge rate per second
    /// </summary>
    public double RechargeRate { get; set; } = 10;

    /// <summary>
    /// Delay before recharge starts after taking damage (seconds)
    /// </summary>
    public double RechargeDelay { get; set; } = 3;

    /// <summary>
    /// Damage resistance (0.0 - 1.0, reduces incoming damage)
    /// </summary>
    public double DamageResistance { get; set; } = 0;

    /// <summary>
    /// Kinetic damage resistance
    /// </summary>
    public double KineticResistance { get; set; } = 0;

    /// <summary>
    /// Energy damage resistance
    /// </summary>
    public double EnergyResistance { get; set; } = 0;

    /// <summary>
    /// Explosive damage resistance
    /// </summary>
    public double ExplosiveResistance { get; set; } = 0;

    /// <summary>
    /// Thermal damage resistance
    /// </summary>
    public double ThermalResistance { get; set; } = 0;

    /// <summary>
    /// EMP resistance (reduces system disable duration)
    /// </summary>
    public double EMPResistance { get; set; } = 0;

    /// <summary>
    /// Shield type
    /// </summary>
    public ShieldType ShieldType { get; set; } = ShieldType.Standard;

    /// <summary>
    /// Energy cost per second while active
    /// </summary>
    public double EnergyPerSecond { get; set; } = 5;

    /// <summary>
    /// Energy cost per damage point absorbed
    /// </summary>
    public double EnergyPerDamage { get; set; } = 0.1;

    /// <summary>
    /// Whether shield is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Current shield points
    /// </summary>
    public int CurrentCharge { get; set; } = 100;

    /// <summary>
    /// Time since last damage taken
    /// </summary>
    public double TimeSinceDamage { get; set; } = 0;

    /// <summary>
    /// Whether shield is broken (fully depleted)
    /// </summary>
    public bool IsBroken => CurrentCharge <= 0;

    /// <summary>
    /// Shield efficiency (0.0 - 1.0, affects all stats)
    /// </summary>
    public double Efficiency { get; set; } = 1.0;

    /// <summary>
    /// Minimum reactor output required (MW)
    /// </summary>
    public double RequiredReactorOutput { get; set; } = 10;

    /// <summary>
    /// Heat generated per second when active
    /// </summary>
    public double HeatPerSecond { get; set; } = 2;

    /// <summary>
    /// Heat generated per damage absorbed
    /// </summary>
    public double HeatPerDamage { get; set; } = 0.5;

    /// <summary>
    /// Recharge boost when not taking damage (multiplier)
    /// </summary>
    public double RechargeBoost { get; set; } = 1.0;

    /// <summary>
    /// Shield harmonics - bonus vs specific damage type
    /// </summary>
    public Dictionary<DamageType, double> HarmonicBonuses { get; } = new();

    // ISaveable implementation
    public override string SaveId => $"shield_{Id}";
    public override int SaveVersion => 1;

    /// <summary>
    /// Gets effective capacity considering condition and efficiency
    /// </summary>
    public int EffectiveCapacity => (int)(Capacity * Condition * Efficiency);

    /// <summary>
    /// Gets effective recharge rate considering condition and efficiency
    /// </summary>
    public double EffectiveRechargeRate => RechargeRate * Condition * Efficiency;

    /// <summary>
    /// Gets effective resistance for a damage type
    /// </summary>
    public double GetResistance(DamageType damageType)
    {
        var baseResistance = damageType switch
        {
            DamageType.Kinetic => KineticResistance,
            DamageType.Energy => EnergyResistance,
            DamageType.Explosive => ExplosiveResistance,
            DamageType.Thermal => ThermalResistance,
            DamageType.EMP => EMPResistance,
            _ => DamageResistance
        };

        var harmonicBonus = HarmonicBonuses.TryGetValue(damageType, out var bonus) ? bonus : 0;
        return Math.Min(1.0, (baseResistance + harmonicBonus) * Condition * Efficiency);
    }

    /// <summary>
    /// Absorbs damage, returns remaining damage that penetrates to hull
    /// </summary>
    public int AbsorbDamage(int damage, DamageType damageType)
    {
        if (!IsActive || IsBroken)
            return damage;

        TimeSinceDamage = 0;

        var resistance = GetResistance(damageType);
        var mitigatedDamage = (int)(damage * (1.0 - resistance));
        var actualDamage = Math.Min(mitigatedDamage, CurrentCharge);
        
        CurrentCharge -= actualDamage;
        
        // Generate heat from absorption
        var heatGenerated = HeatPerDamage * actualDamage;
        
        // Energy cost
        var energyCost = EnergyPerDamage * actualDamage;
        
        // If shield breaks, apply penalty
        if (IsBroken)
        {
            // Shield broken - longer recharge delay
            TimeSinceDamage = -RechargeDelay * 2;
        }

        return damage - actualDamage;
    }

    /// <summary>
    /// Recharges shield over time
    /// </summary>
    public void Recharge(double deltaTime, double availableEnergy)
    {
        if (!IsActive || IsBroken)
            return;

        TimeSinceDamage += deltaTime;

        if (TimeSinceDamage < RechargeDelay)
            return;

        var energyRequired = EffectiveRechargeRate * deltaTime * EnergyPerSecond;
        
        if (availableEnergy < energyRequired)
        {
            // Not enough energy - reduced recharge
            var energyRatio = availableEnergy / energyRequired;
            var rechargeAmount = EffectiveRechargeRate * deltaTime * energyRatio * RechargeBoost;
            CurrentCharge = Math.Min(EffectiveCapacity, CurrentCharge + (int)rechargeAmount);
        }
        else
        {
            var rechargeAmount = EffectiveRechargeRate * deltaTime * RechargeBoost;
            CurrentCharge = Math.Min(EffectiveCapacity, CurrentCharge + (int)rechargeAmount);
        }
    }

    /// <summary>
    /// Activates the shield
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        if (CurrentCharge <= 0)
            CurrentCharge = 1; // Minimum charge to activate
    }

    /// <summary>
    /// Deactivates the shield
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Instantly restores shield to full (for repair docks, etc.)
    /// </summary>
    public void FullRestore()
    {
        CurrentCharge = EffectiveCapacity;
        TimeSinceDamage = RechargeDelay; // Ready to recharge immediately
    }

    /// <summary>
    /// Gets charge percentage (0.0 - 1.0)
    /// </summary>
    public double ChargePercentage => EffectiveCapacity > 0 ? (double)CurrentCharge / EffectiveCapacity : 0;

    public override JObject Serialize()
    {
        var baseData = base.Serialize();
        
        baseData["capacity"] = Capacity;
        baseData["rechargeRate"] = RechargeRate;
        baseData["rechargeDelay"] = RechargeDelay;
        baseData["damageResistance"] = DamageResistance;
        baseData["kineticResistance"] = KineticResistance;
        baseData["energyResistance"] = EnergyResistance;
        baseData["explosiveResistance"] = ExplosiveResistance;
        baseData["thermalResistance"] = ThermalResistance;
        baseData["empResistance"] = EMPResistance;
        baseData["shieldType"] = ShieldType.ToString();
        baseData["energyPerSecond"] = EnergyPerSecond;
        baseData["energyPerDamage"] = EnergyPerDamage;
        baseData["isActive"] = IsActive;
        baseData["currentCharge"] = CurrentCharge;
        baseData["timeSinceDamage"] = TimeSinceDamage;
        baseData["efficiency"] = Efficiency;
        baseData["requiredReactorOutput"] = RequiredReactorOutput;
        baseData["heatPerSecond"] = HeatPerSecond;
        baseData["heatPerDamage"] = HeatPerDamage;
        baseData["rechargeBoost"] = RechargeBoost;
        baseData["harmonicBonuses"] = JObject.FromObject(HarmonicBonuses);

        return baseData;
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        Capacity = data["capacity"]?.ToObject<int>() ?? 100;
        RechargeRate = data["rechargeRate"]?.ToObject<double>() ?? 10;
        RechargeDelay = data["rechargeDelay"]?.ToObject<double>() ?? 3;
        DamageResistance = data["damageResistance"]?.ToObject<double>() ?? 0;
        KineticResistance = data["kineticResistance"]?.ToObject<double>() ?? 0;
        EnergyResistance = data["energyResistance"]?.ToObject<double>() ?? 0;
        ExplosiveResistance = data["explosiveResistance"]?.ToObject<double>() ?? 0;
        ThermalResistance = data["thermalResistance"]?.ToObject<double>() ?? 0;
        EMPResistance = data["empResistance"]?.ToObject<double>() ?? 0;

        if (Enum.TryParse<ShieldType>(data["shieldType"]?.ToString(), out var shieldType))
            ShieldType = shieldType;

        EnergyPerSecond = data["energyPerSecond"]?.ToObject<double>() ?? 5;
        EnergyPerDamage = data["energyPerDamage"]?.ToObject<double>() ?? 0.1;
        IsActive = data["isActive"]?.ToObject<bool>() ?? true;
        CurrentCharge = data["currentCharge"]?.ToObject<int>() ?? 100;
        TimeSinceDamage = data["timeSinceDamage"]?.ToObject<double>() ?? 0;
        Efficiency = data["efficiency"]?.ToObject<double>() ?? 1.0;
        RequiredReactorOutput = data["requiredReactorOutput"]?.ToObject<double>() ?? 10;
        HeatPerSecond = data["heatPerSecond"]?.ToObject<double>() ?? 2;
        HeatPerDamage = data["heatPerDamage"]?.ToObject<double>() ?? 0.5;
        RechargeBoost = data["rechargeBoost"]?.ToObject<double>() ?? 1.0;

        HarmonicBonuses.Clear();
        if (data["harmonicBonuses"] is JObject bonusesObj)
        {
            foreach (var kvp in bonusesObj)
            {
                if (Enum.TryParse<DamageType>(kvp.Key, out var dmgType))
                {
                    var value = kvp.Value?.ToObject<double>();
                    if (value.HasValue)
                        HarmonicBonuses[dmgType] = value.Value;
                }
            }
        }

        // Set equipment type
        Type = EquipmentType.Shield;
        MountType = MountType.Internal;
        Size = EquipmentSize.Medium;
    }

    public override Equipment Clone()
    {
        var clone = new Shield
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
            Capacity = Capacity,
            RechargeRate = RechargeRate,
            RechargeDelay = RechargeDelay,
            DamageResistance = DamageResistance,
            KineticResistance = KineticResistance,
            EnergyResistance = EnergyResistance,
            ExplosiveResistance = ExplosiveResistance,
            ThermalResistance = ThermalResistance,
            EMPResistance = EMPResistance,
            ShieldType = ShieldType,
            EnergyPerSecond = EnergyPerSecond,
            EnergyPerDamage = EnergyPerDamage,
            IsActive = IsActive,
            CurrentCharge = CurrentCharge,
            TimeSinceDamage = TimeSinceDamage,
            Efficiency = Efficiency,
            RequiredReactorOutput = RequiredReactorOutput,
            HeatPerSecond = HeatPerSecond,
            HeatPerDamage = HeatPerDamage,
            RechargeBoost = RechargeBoost
        };

        foreach (var tag in Tags)
            clone.Tags.Add(tag);

        foreach (var kvp in StatModifiers)
            clone.StatModifiers[kvp.Key] = kvp.Value;

        foreach (var kvp in HarmonicBonuses)
            clone.HarmonicBonuses[kvp.Key] = kvp.Value;

        return clone;
    }

    public override bool Validate(out string error)
    {
        if (!base.Validate(out error))
            return false;

        if (Capacity <= 0)
        {
            error = "Shield capacity must be positive";
            return false;
        }

        if (RechargeRate < 0)
        {
            error = "Recharge rate cannot be negative";
            return false;
        }

        if (RechargeDelay < 0)
        {
            error = "Recharge delay cannot be negative";
            return false;
        }

        if (DamageResistance < 0 || DamageResistance > 1)
        {
            error = "Damage resistance must be between 0 and 1";
            return false;
        }

        if (KineticResistance < 0 || KineticResistance > 1)
        {
            error = "Kinetic resistance must be between 0 and 1";
            return false;
        }

        if (EnergyResistance < 0 || EnergyResistance > 1)
        {
            error = "Energy resistance must be between 0 and 1";
            return false;
        }

        if (ExplosiveResistance < 0 || ExplosiveResistance > 1)
        {
            error = "Explosive resistance must be between 0 and 1";
            return false;
        }

        if (ThermalResistance < 0 || ThermalResistance > 1)
        {
            error = "Thermal resistance must be between 0 and 1";
            return false;
        }

        if (EMPResistance < 0 || EMPResistance > 1)
        {
            error = "EMP resistance must be between 0 and 1";
            return false;
        }

        if (EnergyPerSecond < 0)
        {
            error = "Energy per second cannot be negative";
            return false;
        }

        if (EnergyPerDamage < 0)
        {
            error = "Energy per damage cannot be negative";
            return false;
        }

        if (CurrentCharge < 0 || CurrentCharge > EffectiveCapacity)
        {
            error = "Current charge must be between 0 and effective capacity";
            return false;
        }

        if (Efficiency < 0 || Efficiency > 1)
        {
            error = "Efficiency must be between 0 and 1";
            return false;
        }

        error = string.Empty;
        return true;
    }
}