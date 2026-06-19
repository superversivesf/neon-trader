using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Engine types
/// </summary>
public enum EngineType
{
    Chemical,       // Standard chemical rockets
    Ion,            // Electric propulsion - efficient, low thrust
    Plasma,         // Plasma drive - balanced
    Fusion,         // Fusion torch - high thrust, high efficiency
    Antimatter,     // Antimatter - extreme performance
    Warp,           // FTL capability
    Quantum,        // Quantum drive - experimental
    Grappler        // Grappling/towing engine
}

/// <summary>
/// Engine equipment - provides thrust, maneuverability, and FTL
/// </summary>
public sealed class Engine : Equipment
{
    /// <summary>
    /// Engine type
    /// </summary>
    public EngineType EngineType { get; set; } = EngineType.Chemical;

    /// <summary>
    /// Maximum thrust (Newtons)
    /// </summary>
    public double MaxThrust { get; set; } = 100000;

    /// <summary>
    /// Thrust at cruise (percentage of max)
    /// </summary>
    public double CruiseThrustRatio { get; set; } = 0.3;

    /// <summary>
    /// Fuel efficiency (distance per fuel unit)
    /// </summary>
    public double FuelEfficiency { get; set; } = 1.0;

    /// <summary>
    /// Maximum speed multiplier (applied to ship base)
    /// </summary>
    public double MaxSpeedMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Acceleration multiplier
    /// </summary>
    public double AccelerationMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Turn rate multiplier
    /// </summary>
    public double TurnRateMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Warp capability (light-years per jump)
    /// </summary>
    public double WarpRange { get; set; } = 0;

    /// <summary>
    /// Warp cooldown (seconds)
    /// </summary>
    public double WarpCooldown { get; set; } = 60;

    /// <summary>
    /// Warp fuel cost per light-year
    /// </summary>
    public double WarpFuelCost { get; set; } = 10;

    /// <summary>
    /// Warp energy cost per light-year
    /// </summary>
    public double WarpEnergyCost { get; set; } = 100;

    /// <summary>
    /// Whether engine supports FTL
    /// </summary>
    public bool HasWarpDrive => WarpRange > 0;

    /// <summary>
    /// Afterburner thrust multiplier
    /// </summary>
    public double AfterburnerMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Afterburner fuel multiplier
    /// </summary>
    public double AfterburnerFuelMultiplier { get; set; } = 5.0;

    /// <summary>
    /// Afterburner heat per second
    /// </summary>
    public double AfterburnerHeat { get; set; } = 50;

    /// <summary>
    /// Maneuvering thruster strength
    /// </summary>
    public double ManeuveringThrust { get; set; } = 10000;

    /// <summary>
    /// Reverse thrust ratio
    /// </summary>
    public double ReverseThrustRatio { get; set; } = 0.5;

    /// <summary>
    /// Minimum reactor output required (MW)
    /// </summary>
    public double RequiredReactorOutput { get; set; } = 50;

    /// <summary>
    /// Heat generated at max thrust per second
    /// </summary>
    public double HeatAtMaxThrust { get; set; } = 100;

    /// <summary>
    /// Heat generated at cruise per second
    /// </summary>
    public double HeatAtCruise { get; set; } = 20;

    /// <summary>
    /// Engine spool-up time (seconds to reach max thrust)
    /// </summary>
    public double SpoolUpTime { get; set; } = 1;

    /// <summary>
    /// Engine spool-down time
    /// </summary>
    public double SpoolDownTime { get; set; } = 0.5;

    /// <summary>
    /// Current thrust level (0.0 - 1.0)
    /// </summary>
    public double CurrentThrustLevel { get; set; } = 0;

    /// <summary>
    /// Whether afterburner is active
    /// </summary>
    public bool IsAfterburnerActive { get; set; } = false;

    /// <summary>
    /// Whether warp drive is charging
    /// </summary>
    public bool IsWarping { get; set; } = false;

    /// <summary>
    /// Warp charge progress (0.0 - 1.0)
    /// </summary>
    public double WarpChargeProgress { get; set; } = 0;

    /// <summary>
    /// Warp charge time (seconds)
    /// </summary>
    public double WarpChargeTime { get; set; } = 5;

    /// <summary>
    /// Time since last warp
    /// </summary>
    public double TimeSinceWarp { get; set; } = 0;

    /// <summary>
    /// Fuel type required
    /// </summary>
    public string FuelType { get; set; } = "Hydrogen";

    /// <summary>
    /// Engine efficiency at different throttle levels
    /// </summary>
    public Dictionary<double, double> ThrottleEfficiencyCurve { get; } = new();

    // ISaveable implementation
    public override string SaveId => $"engine_{Id}";
    public override int SaveVersion => 1;

    /// <summary>
    /// Gets effective thrust at current level
    /// </summary>
    public double GetEffectiveThrust()
    {
        var baseThrust = MaxThrust * CurrentThrustLevel;
        if (IsAfterburnerActive)
            baseThrust *= AfterburnerMultiplier;
        return baseThrust * Condition;
    }

    /// <summary>
    /// Gets effective fuel consumption at current level
    /// </summary>
    public double GetEffectiveFuelConsumption()
    {
        var baseConsumption = 1.0 / FuelEfficiency * CurrentThrustLevel;
        if (IsAfterburnerActive)
            baseConsumption *= AfterburnerFuelMultiplier;
        return baseConsumption / Condition;
    }

    /// <summary>
    /// Gets effective heat generation at current level
    /// </summary>
    public double GetEffectiveHeat()
    {
        if (IsAfterburnerActive)
            return HeatAtMaxThrust * AfterburnerHeat / 100.0 * Condition;
        
        var thrustRatio = CurrentThrustLevel;
        return (HeatAtCruise + (HeatAtMaxThrust - HeatAtCruise) * thrustRatio) * Condition;
    }

    /// <summary>
    /// Gets effective max speed
    /// </summary>
    public double GetEffectiveMaxSpeed(double shipBaseSpeed)
    {
        return shipBaseSpeed * MaxSpeedMultiplier * Condition;
    }

    /// <summary>
    /// Gets effective acceleration
    /// </summary>
    public double GetEffectiveAcceleration(double shipBaseAccel)
    {
        return shipBaseAccel * AccelerationMultiplier * Condition;
    }

    /// <summary>
    /// Gets effective turn rate
    /// </summary>
    public double GetEffectiveTurnRate(double shipBaseTurnRate)
    {
        return shipBaseTurnRate * TurnRateMultiplier * Condition;
    }

    /// <summary>
    /// Sets thrust level (0.0 - 1.0)
    /// </summary>
    public void SetThrustLevel(double level)
    {
        CurrentThrustLevel = Math.Clamp(level, 0, 1);
    }

    /// <summary>
    /// Activates afterburner
    /// </summary>
    public bool ActivateAfterburner()
    {
        if (CurrentThrustLevel < 0.5)
            return false; // Need minimum thrust

        IsAfterburnerActive = true;
        return true;
    }

    /// <summary>
    /// Deactivates afterburner
    /// </summary>
    public void DeactivateAfterburner()
    {
        IsAfterburnerActive = false;
    }

    /// <summary>
    /// Starts warp charge
    /// </summary>
    public bool StartWarp()
    {
        if (!HasWarpDrive || IsWarping || TimeSinceWarp < WarpCooldown)
            return false;

        IsWarping = true;
        WarpChargeProgress = 0;
        return true;
    }

    /// <summary>
    /// Updates warp charge
    /// </summary>
    public bool UpdateWarp(double deltaTime, double availableEnergy, double availableFuel)
    {
        if (!IsWarping)
            return false;

        var energyRequired = WarpEnergyCost * WarpRange;
        var fuelRequired = WarpFuelCost * WarpRange;

        if (availableEnergy < energyRequired || availableFuel < fuelRequired)
        {
            CancelWarp();
            return false;
        }

        WarpChargeProgress += deltaTime / WarpChargeTime;

        if (WarpChargeProgress >= 1.0)
        {
            IsWarping = false;
            WarpChargeProgress = 0;
            TimeSinceWarp = 0;
            return true; // Warp complete
        }

        return false; // Still charging
    }

    /// <summary>
    /// Cancels warp charge
    /// </summary>
    public void CancelWarp()
    {
        IsWarping = false;
        WarpChargeProgress = 0;
    }

    /// <summary>
    /// Updates engine state
    /// </summary>
    public void Update(double deltaTime)
    {
        TimeSinceWarp += deltaTime;

        // Spool up/down
        if (CurrentThrustLevel > 0)
        {
            // Engine running - heat generation handled by ship
        }
    }

    /// <summary>
    /// Gets fuel consumption for a warp jump
    /// </summary>
    public double GetWarpFuelCost(double distance)
    {
        return WarpFuelCost * distance;
    }

    /// <summary>
    /// Gets energy cost for a warp jump
    /// </summary>
    public double GetWarpEnergyCost(double distance)
    {
        return WarpEnergyCost * distance;
    }

    /// <summary>
    /// Checks if can warp distance
    /// </summary>
    public bool CanWarpDistance(double distance)
    {
        return HasWarpDrive && distance <= WarpRange;
    }

    public override JObject Serialize()
    {
        var baseData = base.Serialize();
        
        baseData["engineType"] = EngineType.ToString();
        baseData["maxThrust"] = MaxThrust;
        baseData["cruiseThrustRatio"] = CruiseThrustRatio;
        baseData["fuelEfficiency"] = FuelEfficiency;
        baseData["maxSpeedMultiplier"] = MaxSpeedMultiplier;
        baseData["accelerationMultiplier"] = AccelerationMultiplier;
        baseData["turnRateMultiplier"] = TurnRateMultiplier;
        baseData["warpRange"] = WarpRange;
        baseData["warpCooldown"] = WarpCooldown;
        baseData["warpFuelCost"] = WarpFuelCost;
        baseData["warpEnergyCost"] = WarpEnergyCost;
        baseData["afterburnerMultiplier"] = AfterburnerMultiplier;
        baseData["afterburnerFuelMultiplier"] = AfterburnerFuelMultiplier;
        baseData["afterburnerHeat"] = AfterburnerHeat;
        baseData["maneuveringThrust"] = ManeuveringThrust;
        baseData["reverseThrustRatio"] = ReverseThrustRatio;
        baseData["requiredReactorOutput"] = RequiredReactorOutput;
        baseData["heatAtMaxThrust"] = HeatAtMaxThrust;
        baseData["heatAtCruise"] = HeatAtCruise;
        baseData["spoolUpTime"] = SpoolUpTime;
        baseData["spoolDownTime"] = SpoolDownTime;
        baseData["currentThrustLevel"] = CurrentThrustLevel;
        baseData["isAfterburnerActive"] = IsAfterburnerActive;
        baseData["isWarping"] = IsWarping;
        baseData["warpChargeProgress"] = WarpChargeProgress;
        baseData["warpChargeTime"] = WarpChargeTime;
        baseData["timeSinceWarp"] = TimeSinceWarp;
        baseData["fuelType"] = FuelType;
        baseData["throttleEfficiencyCurve"] = JObject.FromObject(ThrottleEfficiencyCurve);

        return baseData;
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        if (Enum.TryParse<EngineType>(data["engineType"]?.ToString(), out var engineType))
            EngineType = engineType;

        MaxThrust = data["maxThrust"]?.ToObject<double>() ?? 100000;
        CruiseThrustRatio = data["cruiseThrustRatio"]?.ToObject<double>() ?? 0.3;
        FuelEfficiency = data["fuelEfficiency"]?.ToObject<double>() ?? 1.0;
        MaxSpeedMultiplier = data["maxSpeedMultiplier"]?.ToObject<double>() ?? 1.0;
        AccelerationMultiplier = data["accelerationMultiplier"]?.ToObject<double>() ?? 1.0;
        TurnRateMultiplier = data["turnRateMultiplier"]?.ToObject<double>() ?? 1.0;
        WarpRange = data["warpRange"]?.ToObject<double>() ?? 0;
        WarpCooldown = data["warpCooldown"]?.ToObject<double>() ?? 60;
        WarpFuelCost = data["warpFuelCost"]?.ToObject<double>() ?? 10;
        WarpEnergyCost = data["warpEnergyCost"]?.ToObject<double>() ?? 100;
        AfterburnerMultiplier = data["afterburnerMultiplier"]?.ToObject<double>() ?? 2.0;
        AfterburnerFuelMultiplier = data["afterburnerFuelMultiplier"]?.ToObject<double>() ?? 5.0;
        AfterburnerHeat = data["afterburnerHeat"]?.ToObject<double>() ?? 50;
        ManeuveringThrust = data["maneuveringThrust"]?.ToObject<double>() ?? 10000;
        ReverseThrustRatio = data["reverseThrustRatio"]?.ToObject<double>() ?? 0.5;
        RequiredReactorOutput = data["requiredReactorOutput"]?.ToObject<double>() ?? 50;
        HeatAtMaxThrust = data["heatAtMaxThrust"]?.ToObject<double>() ?? 100;
        HeatAtCruise = data["heatAtCruise"]?.ToObject<double>() ?? 20;
        SpoolUpTime = data["spoolUpTime"]?.ToObject<double>() ?? 1;
        SpoolDownTime = data["spoolDownTime"]?.ToObject<double>() ?? 0.5;
        CurrentThrustLevel = data["currentThrustLevel"]?.ToObject<double>() ?? 0;
        IsAfterburnerActive = data["isAfterburnerActive"]?.ToObject<bool>() ?? false;
        IsWarping = data["isWarping"]?.ToObject<bool>() ?? false;
        WarpChargeProgress = data["warpChargeProgress"]?.ToObject<double>() ?? 0;
        WarpChargeTime = data["warpChargeTime"]?.ToObject<double>() ?? 5;
        TimeSinceWarp = data["timeSinceWarp"]?.ToObject<double>() ?? 0;
        FuelType = data["fuelType"]?.ToString() ?? "Hydrogen";

        ThrottleEfficiencyCurve.Clear();
        if (data["throttleEfficiencyCurve"] is JObject curveObj)
        {
            foreach (var kvp in curveObj)
            {
                if (double.TryParse(kvp.Key, out var key))
                {
                    var value = kvp.Value?.ToObject<double>();
                    if (value.HasValue)
                        ThrottleEfficiencyCurve[key] = value.Value;
                }
            }
        }

        // Set equipment type
        Type = EquipmentType.Engine;
        MountType = MountType.Internal;
        Size = EquipmentSize.Medium;
    }

    public override Equipment Clone()
    {
        var clone = new Engine
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
            EngineType = EngineType,
            MaxThrust = MaxThrust,
            CruiseThrustRatio = CruiseThrustRatio,
            FuelEfficiency = FuelEfficiency,
            MaxSpeedMultiplier = MaxSpeedMultiplier,
            AccelerationMultiplier = AccelerationMultiplier,
            TurnRateMultiplier = TurnRateMultiplier,
            WarpRange = WarpRange,
            WarpCooldown = WarpCooldown,
            WarpFuelCost = WarpFuelCost,
            WarpEnergyCost = WarpEnergyCost,
            AfterburnerMultiplier = AfterburnerMultiplier,
            AfterburnerFuelMultiplier = AfterburnerFuelMultiplier,
            AfterburnerHeat = AfterburnerHeat,
            ManeuveringThrust = ManeuveringThrust,
            ReverseThrustRatio = ReverseThrustRatio,
            RequiredReactorOutput = RequiredReactorOutput,
            HeatAtMaxThrust = HeatAtMaxThrust,
            HeatAtCruise = HeatAtCruise,
            SpoolUpTime = SpoolUpTime,
            SpoolDownTime = SpoolDownTime,
            CurrentThrustLevel = CurrentThrustLevel,
            IsAfterburnerActive = IsAfterburnerActive,
            IsWarping = IsWarping,
            WarpChargeProgress = WarpChargeProgress,
            WarpChargeTime = WarpChargeTime,
            TimeSinceWarp = TimeSinceWarp,
            FuelType = FuelType
        };

        foreach (var tag in Tags)
            clone.Tags.Add(tag);

        foreach (var kvp in StatModifiers)
            clone.StatModifiers[kvp.Key] = kvp.Value;

        foreach (var kvp in ThrottleEfficiencyCurve)
            clone.ThrottleEfficiencyCurve[kvp.Key] = kvp.Value;

        return clone;
    }

    public override bool Validate(out string error)
    {
        if (!base.Validate(out error))
            return false;

        if (MaxThrust <= 0)
        {
            error = "Max thrust must be positive";
            return false;
        }

        if (CruiseThrustRatio < 0 || CruiseThrustRatio > 1)
        {
            error = "Cruise thrust ratio must be between 0 and 1";
            return false;
        }

        if (FuelEfficiency <= 0)
        {
            error = "Fuel efficiency must be positive";
            return false;
        }

        if (MaxSpeedMultiplier <= 0)
        {
            error = "Max speed multiplier must be positive";
            return false;
        }

        if (WarpRange < 0)
        {
            error = "Warp range cannot be negative";
            return false;
        }

        if (WarpCooldown < 0)
        {
            error = "Warp cooldown cannot be negative";
            return false;
        }

        if (WarpFuelCost < 0)
        {
            error = "Warp fuel cost cannot be negative";
            return false;
        }

        if (WarpEnergyCost < 0)
        {
            error = "Warp energy cost cannot be negative";
            return false;
        }

        if (AfterburnerMultiplier < 1)
        {
            error = "Afterburner multiplier must be >= 1";
            return false;
        }

        if (AfterburnerFuelMultiplier < 1)
        {
            error = "Afterburner fuel multiplier must be >= 1";
            return false;
        }

        if (RequiredReactorOutput < 0)
        {
            error = "Required reactor output cannot be negative";
            return false;
        }

        error = string.Empty;
        return true;
    }
}