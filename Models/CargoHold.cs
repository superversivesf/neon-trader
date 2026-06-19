using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Cargo hold upgrade equipment - expands cargo capacity
/// </summary>
public sealed class CargoHold : Equipment
{
    /// <summary>
    /// Additional cargo capacity in tons
    /// </summary>
    public int CapacityBonus { get; set; } = 10;

    /// <summary>
    /// Cargo specialization (affects certain commodity types)
    /// </summary>
    public CargoSpecialization Specialization { get; set; } = CargoSpecialization.General;

    /// <summary>
    /// Temperature control (for perishables)
    /// </summary>
    public bool HasTemperatureControl { get; set; } = false;

    /// <summary>
    /// Temperature range (min, max) in Celsius
    /// </summary>
    public (int Min, int Max) TemperatureRange { get; set; } = (-20, 40);

    /// <summary>
    /// Hazardous material containment
    /// </summary>
    public bool HasHazmatContainment { get; set; } = false;

    /// <summary>
    /// Radiation shielding (for radioactive cargo)
    /// </summary>
    public bool HasRadiationShielding { get; set; } = false;

    /// <summary>
    /// Secure/encrypted storage (for valuable/data cargo)
    /// </summary>
    public bool HasSecureStorage { get; set; } = false;

    /// <summary>
    /// Automated loading/unloading (faster transfer)
    /// </summary>
    public bool HasAutomatedLoading { get; set; } = false;

    /// <summary>
    /// Loading speed multiplier
    /// </summary>
    public double LoadingSpeedMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Cargo compression (reduces effective mass)
    /// </summary>
    public double CompressionRatio { get; set; } = 1.0;

    /// <summary>
    /// Mass penalty for this cargo hold
    /// </summary>
    public double MassPenalty { get; set; } = 0;

    /// <summary>
    /// Power draw when active
    /// </summary>
    public double PowerDrawActive { get; set; } = 0;

    /// <summary>
    /// Minimum ship size
    /// </summary>
    public new ShipSize MinimumShipSize { get; set; } = ShipSize.Tiny;

    // ISaveable implementation
    public override string SaveId => $"cargohold_{Id}";
    public override int SaveVersion => 1;

    /// <summary>
    /// Gets effective capacity bonus considering condition
    /// </summary>
    public int EffectiveCapacityBonus => (int)(CapacityBonus * Condition);

    /// <summary>
    /// Checks if can store commodity type
    /// </summary>
    public bool CanStoreCommodity(CommodityCategory category, CommodityLegality legality, HashSet<string> tags)
    {
        // Check legality
        if (legality == CommodityLegality.Illegal && !HasSecureStorage)
            return false;

        // Check hazardous
        if (tags.Contains("hazardous") || tags.Contains("radioactive"))
        {
            if (tags.Contains("radioactive") && !HasRadiationShielding)
                return false;
            if (tags.Contains("hazardous") && !HasHazmatContainment)
                return false;
        }

        // Check perishable
        if (tags.Contains("perishable") && !HasTemperatureControl)
            return false;

        // Check specialization bonus/penalty
        return true;
    }

    /// <summary>
    /// Gets storage efficiency for a commodity (0.0 - 1.0+)
    /// </summary>
    public double GetStorageEfficiency(CommodityCategory category, HashSet<string> tags)
    {
        var efficiency = 1.0;

        // Specialization bonus
        if (Specialization == CargoSpecialization.General)
            return efficiency;

        var matchesSpecialization = (Specialization, category) switch
        {
            (CargoSpecialization.Ore, CommodityCategory.Ore) => true,
            (CargoSpecialization.Organics, CommodityCategory.Organics) => true,
            (CargoSpecialization.Tech, CommodityCategory.Tech) => true,
            (CargoSpecialization.Luxury, CommodityCategory.Luxury) => true,
            (CargoSpecialization.Weapons, CommodityCategory.Weapons) => true,
            (CargoSpecialization.Medical, CommodityCategory.Medical) => true,
            _ => false
        };

        if (matchesSpecialization)
            efficiency *= 1.2; // 20% bonus

        // Compression bonus
        if (tags.Contains("compressible"))
            efficiency *= CompressionRatio;

        return efficiency;
    }

    /// <summary>
    /// Gets effective mass of stored cargo
    /// </summary>
    public double GetEffectiveCargoMass(double baseMass, CommodityCategory category, HashSet<string> tags)
    {
        var efficiency = GetStorageEfficiency(category, tags);
        return baseMass / efficiency;
    }

    public override JObject Serialize()
    {
        var baseData = base.Serialize();
        
        baseData["capacityBonus"] = CapacityBonus;
        baseData["specialization"] = Specialization.ToString();
        baseData["hasTemperatureControl"] = HasTemperatureControl;
        baseData["temperatureRangeMin"] = TemperatureRange.Min;
        baseData["temperatureRangeMax"] = TemperatureRange.Max;
        baseData["hasHazmatContainment"] = HasHazmatContainment;
        baseData["hasRadiationShielding"] = HasRadiationShielding;
        baseData["hasSecureStorage"] = HasSecureStorage;
        baseData["hasAutomatedLoading"] = HasAutomatedLoading;
        baseData["loadingSpeedMultiplier"] = LoadingSpeedMultiplier;
        baseData["compressionRatio"] = CompressionRatio;
        baseData["massPenalty"] = MassPenalty;
        baseData["powerDrawActive"] = PowerDrawActive;
        baseData["minimumShipSize"] = MinimumShipSize.ToString();

        return baseData;
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        CapacityBonus = data["capacityBonus"]?.ToObject<int>() ?? 10;

        if (Enum.TryParse<CargoSpecialization>(data["specialization"]?.ToString(), out var spec))
            Specialization = spec;

        HasTemperatureControl = data["hasTemperatureControl"]?.ToObject<bool>() ?? false;
        
        var minTemp = data["temperatureRangeMin"]?.ToObject<int>() ?? -20;
        var maxTemp = data["temperatureRangeMax"]?.ToObject<int>() ?? 40;
        TemperatureRange = (minTemp, maxTemp);

        HasHazmatContainment = data["hasHazmatContainment"]?.ToObject<bool>() ?? false;
        HasRadiationShielding = data["hasRadiationShielding"]?.ToObject<bool>() ?? false;
        HasSecureStorage = data["hasSecureStorage"]?.ToObject<bool>() ?? false;
        HasAutomatedLoading = data["hasAutomatedLoading"]?.ToObject<bool>() ?? false;
        LoadingSpeedMultiplier = data["loadingSpeedMultiplier"]?.ToObject<double>() ?? 1.0;
        CompressionRatio = data["compressionRatio"]?.ToObject<double>() ?? 1.0;
        MassPenalty = data["massPenalty"]?.ToObject<double>() ?? 0;
        PowerDrawActive = data["powerDrawActive"]?.ToObject<double>() ?? 0;

        if (Enum.TryParse<ShipSize>(data["minimumShipSize"]?.ToString(), out var minSize))
            MinimumShipSize = minSize;

        // Set equipment type
        Type = EquipmentType.CargoExpansion;
        MountType = MountType.Internal;
        Size = EquipmentSize.Medium;
    }

    public override Equipment Clone()
    {
        var clone = new CargoHold
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
            CapacityBonus = CapacityBonus,
            Specialization = Specialization,
            HasTemperatureControl = HasTemperatureControl,
            TemperatureRange = TemperatureRange,
            HasHazmatContainment = HasHazmatContainment,
            HasRadiationShielding = HasRadiationShielding,
            HasSecureStorage = HasSecureStorage,
            HasAutomatedLoading = HasAutomatedLoading,
            LoadingSpeedMultiplier = LoadingSpeedMultiplier,
            CompressionRatio = CompressionRatio,
            MassPenalty = MassPenalty,
            PowerDrawActive = PowerDrawActive
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

        if (CapacityBonus < 0)
        {
            error = "Capacity bonus cannot be negative";
            return false;
        }

        if (LoadingSpeedMultiplier <= 0)
        {
            error = "Loading speed multiplier must be positive";
            return false;
        }

        if (CompressionRatio < 1.0)
        {
            error = "Compression ratio must be >= 1.0";
            return false;
        }

        if (MassPenalty < 0)
        {
            error = "Mass penalty cannot be negative";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

/// <summary>
/// Cargo specialization types
/// </summary>
public enum CargoSpecialization
{
    General,
    Ore,
    Organics,
    Tech,
    Luxury,
    Weapons,
    Medical
}