using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Ship classifications with base stat templates
/// </summary>
public enum ShipClassType
{
    Fighter,
    Freighter,
    Explorer,
    Carrier,
    Interceptor,
    Corvette,
    Cruiser,
    Dreadnought,
    Shuttle,
    MiningVessel,
    Smuggler,
    Patrol
}

/// <summary>
/// Size categories for ships affecting hardpoints, cargo, and upgrade slots
/// </summary>
public enum ShipSize
{
    Tiny,       // 1 hardpoint, 2 upgrade slots, minimal cargo
    Small,      // 2 hardpoints, 3 upgrade slots, small cargo
    Medium,     // 3 hardpoints, 4 upgrade slots, medium cargo
    Large,      // 4 hardpoints, 6 upgrade slots, large cargo
    Capital,    // 6 hardpoints, 8 upgrade slots, massive cargo
    SuperCapital // 8 hardpoints, 10 upgrade slots, enormous cargo
}

/// <summary>
/// Ship class definition containing base stat templates for ship instances
/// </summary>
public sealed class ShipClass : ISaveable
{
    /// <summary>
    /// Unique identifier for this ship class
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the ship class
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type classification
    /// </summary>
    public ShipClassType Type { get; set; } = ShipClassType.Freighter;

    /// <summary>
    /// Physical size category
    /// </summary>
    public ShipSize Size { get; set; } = ShipSize.Medium;

    /// <summary>
    /// Description for UI display
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer/faction that produces this class
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Base hull integrity (health)
    /// </summary>
    public int BaseHullIntegrity { get; set; } = 100;

    /// <summary>
    /// Base shield capacity
    /// </summary>
    public int BaseShieldCapacity { get; set; } = 0;

    /// <summary>
    /// Base shield recharge rate per second
    /// </summary>
    public double BaseShieldRecharge { get; set; } = 0;

    /// <summary>
    /// Base cargo capacity in tons
    /// </summary>
    public int BaseCargoCapacity { get; set; } = 50;

    /// <summary>
    /// Base fuel capacity in units
    /// </summary>
    public int BaseFuelCapacity { get; set; } = 100;

    /// <summary>
    /// Base fuel consumption per light-year
    /// </summary>
    public double BaseFuelConsumption { get; set; } = 1.0;

    /// <summary>
    /// Base maximum speed (units per second)
    /// </summary>
    public double BaseMaxSpeed { get; set; } = 100;

    /// <summary>
    /// Base acceleration (units per second^2)
    /// </summary>
    public double BaseAcceleration { get; set; } = 50;

    /// <summary>
    /// Base turn rate (degrees per second)
    /// </summary>
    public double BaseTurnRate { get; set; } = 90;

    /// <summary>
    /// Number of weapon hardpoints
    /// </summary>
    public int HardpointCount { get; set; } = 2;

    /// <summary>
    /// Number of utility hardpoints (for non-weapon equipment)
    /// </summary>
    public int UtilityHardpointCount { get; set; } = 1;

    /// <summary>
    /// Number of upgrade slots available
    /// </summary>
    public int UpgradeSlotCount { get; set; } = 3;

    /// <summary>
    /// Base price in credits
    /// </summary>
    public long BasePrice { get; set; } = 50000;

    /// <summary>
    /// Minimum reputation tier required to purchase (0-4)
    /// </summary>
    public int RequiredReputationTier { get; set; } = 0;

    /// <summary>
    /// Faction reputation required (empty = any)
    /// </summary>
    public string RequiredFaction { get; set; } = string.Empty;

    /// <summary>
    /// Tags for special properties (e.g., "stealth", "mining", "carrier")
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    /// <summary>
    /// Stat multipliers for this class (e.g., "cargo" = 1.5 for freighters)
    /// </summary>
    public Dictionary<string, double> StatMultipliers { get; } = new();

    /// <summary>
    /// Default equipment loadout (equipment IDs)
    /// </summary>
    public List<string> DefaultEquipment { get; } = new();

    /// <summary>
    /// Whether this ship class can be purchased by players (vs NPC-only)
    /// </summary>
    public bool IsPlayerPurchasable { get; set; } = true;

    // ISaveable implementation
    public string SaveId => $"shipclass_{Id}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the ship class to JSON
    /// </summary>
    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["type"] = Type.ToString(),
            ["size"] = Size.ToString(),
            ["description"] = Description,
            ["manufacturer"] = Manufacturer,
            ["baseHullIntegrity"] = BaseHullIntegrity,
            ["baseShieldCapacity"] = BaseShieldCapacity,
            ["baseShieldRecharge"] = BaseShieldRecharge,
            ["baseCargoCapacity"] = BaseCargoCapacity,
            ["baseFuelCapacity"] = BaseFuelCapacity,
            ["baseFuelConsumption"] = BaseFuelConsumption,
            ["baseMaxSpeed"] = BaseMaxSpeed,
            ["baseAcceleration"] = BaseAcceleration,
            ["baseTurnRate"] = BaseTurnRate,
            ["hardpointCount"] = HardpointCount,
            ["utilityHardpointCount"] = UtilityHardpointCount,
            ["upgradeSlotCount"] = UpgradeSlotCount,
            ["basePrice"] = BasePrice,
            ["requiredReputationTier"] = RequiredReputationTier,
            ["requiredFaction"] = RequiredFaction,
            ["tags"] = JArray.FromObject(Tags),
            ["statMultipliers"] = JObject.FromObject(StatMultipliers),
            ["defaultEquipment"] = JArray.FromObject(DefaultEquipment)
        };
    }

    /// <summary>
    /// Deserialize the ship class from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<ShipClassType>(data["type"]?.ToString(), out var type))
            Type = type;

        if (Enum.TryParse<ShipSize>(data["size"]?.ToString(), out var size))
            Size = size;

        Description = data["description"]?.ToString() ?? string.Empty;
        Manufacturer = data["manufacturer"]?.ToString() ?? string.Empty;
        BaseHullIntegrity = data["baseHullIntegrity"]?.ToObject<int>() ?? 100;
        BaseShieldCapacity = data["baseShieldCapacity"]?.ToObject<int>() ?? 0;
        BaseShieldRecharge = data["baseShieldRecharge"]?.ToObject<double>() ?? 0;
        BaseCargoCapacity = data["baseCargoCapacity"]?.ToObject<int>() ?? 50;
        BaseFuelCapacity = data["baseFuelCapacity"]?.ToObject<int>() ?? 100;
        BaseFuelConsumption = data["baseFuelConsumption"]?.ToObject<double>() ?? 1.0;
        BaseMaxSpeed = data["baseMaxSpeed"]?.ToObject<double>() ?? 100;
        BaseAcceleration = data["baseAcceleration"]?.ToObject<double>() ?? 50;
        BaseTurnRate = data["baseTurnRate"]?.ToObject<double>() ?? 90;
        HardpointCount = data["hardpointCount"]?.ToObject<int>() ?? 2;
        UtilityHardpointCount = data["utilityHardpointCount"]?.ToObject<int>() ?? 1;
        UpgradeSlotCount = data["upgradeSlotCount"]?.ToObject<int>() ?? 3;
        BasePrice = data["basePrice"]?.ToObject<long>() ?? 50000;
        RequiredReputationTier = data["requiredReputationTier"]?.ToObject<int>() ?? 0;
        RequiredFaction = data["requiredFaction"]?.ToString() ?? string.Empty;

        Tags.Clear();
        if (data["tags"] is JArray tagsArray)
        {
            foreach (var tag in tagsArray)
            {
                var tagStr = tag?.ToString();
                if (!string.IsNullOrEmpty(tagStr))
                    Tags.Add(tagStr);
            }
        }

        StatMultipliers.Clear();
        if (data["statMultipliers"] is JObject multipliersObj)
        {
            foreach (var kvp in multipliersObj)
            {
                var value = kvp.Value?.ToObject<double>();
                if (value.HasValue)
                    StatMultipliers[kvp.Key] = value.Value;
            }
        }

        DefaultEquipment.Clear();
        if (data["defaultEquipment"] is JArray equipmentArray)
        {
            DefaultEquipment.AddRange(equipmentArray.ToObject<List<string>>() ?? new());
        }
    }

    /// <summary>
    /// Creates a copy of this ship class
    /// </summary>
    public ShipClass Clone()
    {
        var clone = new ShipClass
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Size = Size,
            Description = Description,
            Manufacturer = Manufacturer,
            BaseHullIntegrity = BaseHullIntegrity,
            BaseShieldCapacity = BaseShieldCapacity,
            BaseShieldRecharge = BaseShieldRecharge,
            BaseCargoCapacity = BaseCargoCapacity,
            BaseFuelCapacity = BaseFuelCapacity,
            BaseFuelConsumption = BaseFuelConsumption,
            BaseMaxSpeed = BaseMaxSpeed,
            BaseAcceleration = BaseAcceleration,
            BaseTurnRate = BaseTurnRate,
            HardpointCount = HardpointCount,
            UtilityHardpointCount = UtilityHardpointCount,
            UpgradeSlotCount = UpgradeSlotCount,
            BasePrice = BasePrice,
            RequiredReputationTier = RequiredReputationTier,
            RequiredFaction = RequiredFaction
        };

        foreach (var tag in Tags)
            clone.Tags.Add(tag);

        foreach (var kvp in StatMultipliers)
            clone.StatMultipliers[kvp.Key] = kvp.Value;

        foreach (var eq in DefaultEquipment)
            clone.DefaultEquipment.Add(eq);

        return clone;
    }

    /// <summary>
    /// Validates the ship class data
    /// </summary>
    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "Ship class ID cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Ship class name cannot be empty";
            return false;
        }

        if (BaseHullIntegrity <= 0)
        {
            error = "Base hull integrity must be positive";
            return false;
        }

        if (BaseCargoCapacity < 0)
        {
            error = "Base cargo capacity cannot be negative";
            return false;
        }

        if (BaseFuelCapacity <= 0)
        {
            error = "Base fuel capacity must be positive";
            return false;
        }

        if (BaseFuelConsumption <= 0)
        {
            error = "Base fuel consumption must be positive";
            return false;
        }

        if (BaseMaxSpeed <= 0)
        {
            error = "Base max speed must be positive";
            return false;
        }

        if (HardpointCount < 0)
        {
            error = "Hardpoint count cannot be negative";
            return false;
        }

        if (UtilityHardpointCount < 0)
        {
            error = "Utility hardpoint count cannot be negative";
            return false;
        }

        if (UpgradeSlotCount < 0)
        {
            error = "Upgrade slot count cannot be negative";
            return false;
        }

        if (BasePrice < 0)
        {
            error = "Base price cannot be negative";
            return false;
        }

        if (RequiredReputationTier < 0 || RequiredReputationTier > 4)
        {
            error = "Required reputation tier must be between 0 and 4";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

/// <summary>
/// Static registry for ship class definitions loaded from data files
/// </summary>
public static class ShipClassRegistry
{
    private static readonly Dictionary<string, ShipClass> _classes = new();

    /// <summary>
    /// Gets a ship class by ID
    /// </summary>
    public static ShipClass? Get(string id)
    {
        _classes.TryGetValue(id, out var shipClass);
        return shipClass;
    }

    /// <summary>
    /// Gets all ship classes
    /// </summary>
    public static IReadOnlyCollection<ShipClass> All => _classes.Values;

    /// <summary>
    /// Gets ship classes by type
    /// </summary>
    public static IReadOnlyList<ShipClass> GetByType(ShipClassType type)
    {
        return _classes.Values.Where(c => c.Type == type).ToList();
    }

    /// <summary>
    /// Gets ship classes by size
    /// </summary>
    public static IReadOnlyList<ShipClass> GetBySize(ShipSize size)
    {
        return _classes.Values.Where(c => c.Size == size).ToList();
    }

    /// <summary>
    /// Registers a ship class
    /// </summary>
    public static void Register(ShipClass shipClass)
    {
        if (shipClass.Validate(out var error))
        {
            _classes[shipClass.Id] = shipClass;
        }
        else
        {
            throw new ArgumentException($"Invalid ship class: {error}");
        }
    }

    /// <summary>
    /// Clears the registry (for testing or reload)
    /// </summary>
    public static void Clear()
    {
        _classes.Clear();
    }

    /// <summary>
    /// Loads ship classes from JSON data
    /// </summary>
    public static void LoadFromJson(string json)
    {
        Clear();
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var shipClass = new ShipClass();
            shipClass.Deserialize((JObject)item);
            Register(shipClass);
        }
    }
}