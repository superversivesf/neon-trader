using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Equipment types for categorization and slot restrictions
/// </summary>
public enum EquipmentType
{
    Engine,
    Weapon,
    Shield,
    Scanner,
    CargoExpansion,
    Utility,
    Armor,
    Reactor,
    Cooling,
    Targeting,
    Countermeasures,
    Mining,
    Salvage,
    Hacking,
    Communications
}

/// <summary>
/// Equipment size categories for slot compatibility
/// </summary>
public enum EquipmentSize
{
    Tiny,       // Size 1 - smallest modules
    Small,      // Size 2 - standard small modules
    Medium,     // Size 3 - standard modules
    Large,      // Size 4 - large modules
    Huge,       // Size 5 - capital ship modules
    Modular     // Variable size - adapts to slot
}

/// <summary>
/// Equipment mount types for hardpoint compatibility
/// </summary>
public enum MountType
{
    Fixed,      // Fixed forward-facing
    Gimballed,  // Limited arc tracking
    Turret,     // 360-degree tracking
    Internal,   // Internal module slot
    Hardpoint,  // External hardpoint
    Utility     // Utility slot
}

/// <summary>
/// Equipment rarity affecting price and availability
/// </summary>
public enum EquipmentRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Prototype
}

/// <summary>
/// Base equipment class - all equipment inherits from this
/// </summary>
public abstract partial class Equipment : ISaveable
{
    /// <summary>
    /// Unique identifier for this equipment
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Equipment type category
    /// </summary>
    public EquipmentType Type { get; set; } = EquipmentType.Utility;

    /// <summary>
    /// Physical size for slot compatibility
    /// </summary>
    public EquipmentSize Size { get; set; } = EquipmentSize.Small;

    /// <summary>
    /// Mount type for hardpoint compatibility
    /// </summary>
    public MountType MountType { get; set; } = MountType.Internal;

    /// <summary>
    /// Rarity affecting price and availability
    /// </summary>
    public EquipmentRarity Rarity { get; set; } = EquipmentRarity.Common;

    /// <summary>
    /// Manufacturer
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Description for UI
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Base price in credits
    /// </summary>
    public long BasePrice { get; set; } = 1000;

    /// <summary>
    /// Mass in tons (affects ship performance)
    /// </summary>
    public double Mass { get; set; } = 1.0;

    /// <summary>
    /// Power draw in MW (requires reactor capacity)
    /// </summary>
    public double PowerDraw { get; set; } = 0;

    /// <summary>
    /// Heat generation per second
    /// </summary>
    public double HeatGeneration { get; set; } = 0;

    /// <summary>
    /// Minimum ship size required
    /// </summary>
    public ShipSize MinimumShipSize { get; set; } = ShipSize.Tiny;

    /// <summary>
    /// Required reputation tier to purchase
    /// </summary>
    public int RequiredReputationTier { get; set; } = 0;

    /// <summary>
    /// Required faction reputation
    /// </summary>
    public string RequiredFaction { get; set; } = string.Empty;

    /// <summary>
    /// Tags for special properties
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    /// <summary>
    /// Stat modifiers provided by this equipment
    /// </summary>
    public Dictionary<string, double> StatModifiers { get; } = new();

    /// <summary>
    /// Whether this equipment is currently installed
    /// </summary>
    public bool IsInstalled { get; set; } = false;

    /// <summary>
    /// Ship ID this equipment is installed on
    /// </summary>
    public string? InstalledOnShipId { get; set; }

    /// <summary>
    /// Slot ID where installed
    /// </summary>
    public string? InstalledSlotId { get; set; }

    /// <summary>
    /// Condition (0.0 = broken, 1.0 = pristine)
    /// </summary>
    public double Condition { get; set; } = 1.0;

    // ISaveable implementation
    public virtual string SaveId => $"equipment_{Id}";
    public virtual int SaveVersion => 1;

    /// <summary>
    /// Serialize the equipment to JSON
    /// </summary>
    public virtual JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["type"] = Type.ToString(),
            ["size"] = Size.ToString(),
            ["mountType"] = MountType.ToString(),
            ["rarity"] = Rarity.ToString(),
            ["manufacturer"] = Manufacturer,
            ["description"] = Description,
            ["basePrice"] = BasePrice,
            ["mass"] = Mass,
            ["powerDraw"] = PowerDraw,
            ["heatGeneration"] = HeatGeneration,
            ["minimumShipSize"] = MinimumShipSize.ToString(),
            ["requiredReputationTier"] = RequiredReputationTier,
            ["requiredFaction"] = RequiredFaction,
            ["tags"] = JArray.FromObject(Tags),
            ["statModifiers"] = JObject.FromObject(StatModifiers),
            ["isInstalled"] = IsInstalled,
            ["installedOnShipId"] = InstalledOnShipId ?? string.Empty,
            ["installedSlotId"] = InstalledSlotId ?? string.Empty,
            ["condition"] = Condition
        };
    }

    /// <summary>
    /// Deserialize the equipment from JSON
    /// </summary>
    public virtual void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<EquipmentType>(data["type"]?.ToString(), out var type))
            Type = type;

        if (Enum.TryParse<EquipmentSize>(data["size"]?.ToString(), out var size))
            Size = size;

        if (Enum.TryParse<MountType>(data["mountType"]?.ToString(), out var mountType))
            MountType = mountType;

        if (Enum.TryParse<EquipmentRarity>(data["rarity"]?.ToString(), out var rarity))
            Rarity = rarity;

        Manufacturer = data["manufacturer"]?.ToString() ?? string.Empty;
        Description = data["description"]?.ToString() ?? string.Empty;
        BasePrice = data["basePrice"]?.ToObject<long>() ?? 1000;
        Mass = data["mass"]?.ToObject<double>() ?? 1.0;
        PowerDraw = data["powerDraw"]?.ToObject<double>() ?? 0;
        HeatGeneration = data["heatGeneration"]?.ToObject<double>() ?? 0;

        if (Enum.TryParse<ShipSize>(data["minimumShipSize"]?.ToString(), out var minSize))
            MinimumShipSize = minSize;

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

        StatModifiers.Clear();
        if (data["statModifiers"] is JObject modifiersObj)
        {
            foreach (var kvp in modifiersObj)
            {
                var value = kvp.Value?.ToObject<double>();
                if (value.HasValue)
                    StatModifiers[kvp.Key] = value.Value;
            }
        }

        IsInstalled = data["isInstalled"]?.ToObject<bool>() ?? false;
        InstalledOnShipId = data["installedOnShipId"]?.ToString();
        InstalledSlotId = data["installedSlotId"]?.ToString();
        Condition = data["condition"]?.ToObject<double>() ?? 1.0;
    }

    /// <summary>
    /// Creates a copy of this equipment
    /// </summary>
    public virtual Equipment Clone()
    {
        var clone = (Equipment)MemberwiseClone();
        clone.Tags.Clear();
        foreach (var tag in Tags)
            clone.Tags.Add(tag);
        clone.StatModifiers.Clear();
        foreach (var kvp in StatModifiers)
            clone.StatModifiers[kvp.Key] = kvp.Value;
        clone.IsInstalled = false;
        clone.InstalledOnShipId = null;
        clone.InstalledSlotId = null;
        return clone;
    }

    /// <summary>
    /// Validates the equipment data
    /// </summary>
    public virtual bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "Equipment ID cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Equipment name cannot be empty";
            return false;
        }

        if (BasePrice < 0)
        {
            error = "Base price cannot be negative";
            return false;
        }

        if (Mass < 0)
        {
            error = "Mass cannot be negative";
            return false;
        }

        if (PowerDraw < 0)
        {
            error = "Power draw cannot be negative";
            return false;
        }

        if (HeatGeneration < 0)
        {
            error = "Heat generation cannot be negative";
            return false;
        }

        if (RequiredReputationTier < 0 || RequiredReputationTier > 4)
        {
            error = "Required reputation tier must be between 0 and 4";
            return false;
        }

        if (Condition < 0 || Condition > 1)
        {
            error = "Condition must be between 0 and 1";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Called when equipment is installed on a ship
    /// </summary>
    public virtual void OnInstalled(Ship ship, string slotId)
    {
        IsInstalled = true;
        InstalledOnShipId = ship.Id;
        InstalledSlotId = slotId;
    }

    /// <summary>
    /// Called when equipment is removed from a ship
    /// </summary>
    public virtual void OnRemoved()
    {
        IsInstalled = false;
        InstalledOnShipId = null;
        InstalledSlotId = null;
    }

    /// <summary>
    /// Called each frame when installed
    /// </summary>
    public virtual void OnUpdate(Ship ship, double deltaTime)
    {
        // Override in derived classes for active effects
    }

    /// <summary>
    /// Gets the effective value of a stat modifier considering condition
    /// </summary>
    public double GetEffectiveModifier(string statName)
    {
        if (StatModifiers.TryGetValue(statName, out var value))
        {
            return value * Condition;
        }
        return 0;
    }
}

/// <summary>
/// Static registry for equipment definitions loaded from data files
/// </summary>
public static partial class EquipmentRegistry
{
    private static readonly Dictionary<string, Equipment> _equipment = new();
    private static readonly Dictionary<EquipmentType, List<Equipment>> _byType = new();

    /// <summary>
    /// Gets equipment by ID
    /// </summary>
    public static Equipment? Get(string id)
    {
        _equipment.TryGetValue(id, out var equipment);
        return equipment;
    }

    /// <summary>
    /// Gets all equipment
    /// </summary>
    public static IReadOnlyCollection<Equipment> All => _equipment.Values;

    /// <summary>
    /// Gets equipment by type
    /// </summary>
    public static IReadOnlyList<Equipment> GetByType(EquipmentType type)
    {
        _byType.TryGetValue(type, out var list);
        return list ?? new List<Equipment>();
    }

    /// <summary>
    /// Gets equipment by size
    /// </summary>
    public static IReadOnlyList<Equipment> GetBySize(EquipmentSize size)
    {
        return _equipment.Values.Where(e => e.Size == size).ToList();
    }

    /// <summary>
    /// Gets equipment by mount type
    /// </summary>
    public static IReadOnlyList<Equipment> GetByMountType(MountType mountType)
    {
        return _equipment.Values.Where(e => e.MountType == mountType).ToList();
    }

    /// <summary>
    /// Gets equipment compatible with a ship size
    /// </summary>
    public static IReadOnlyList<Equipment> GetCompatibleWith(ShipSize shipSize)
    {
        var shipSizeOrder = new[] { ShipSize.Tiny, ShipSize.Small, ShipSize.Medium, ShipSize.Large, ShipSize.Capital, ShipSize.SuperCapital };
        var shipIndex = Array.IndexOf(shipSizeOrder, shipSize);
        return _equipment.Values.Where(e => 
        {
            var eqIndex = Array.IndexOf(shipSizeOrder, e.MinimumShipSize);
            return eqIndex <= shipIndex;
        }).ToList();
    }

    /// <summary>
    /// Registers equipment
    /// </summary>
    public static void Register(Equipment equipment)
    {
        if (equipment.Validate(out var error))
        {
            _equipment[equipment.Id] = equipment;

            if (!_byType.ContainsKey(equipment.Type))
                _byType[equipment.Type] = new List<Equipment>();

            _byType[equipment.Type].Add(equipment);
        }
        else
        {
            throw new ArgumentException($"Invalid equipment: {error}");
        }
    }

    /// <summary>
    /// Unregisters equipment
    /// </summary>
    public static bool Unregister(string id)
    {
        if (_equipment.TryGetValue(id, out var equipment))
        {
            _byType[equipment.Type].Remove(equipment);
            return _equipment.Remove(id);
        }
        return false;
    }

    /// <summary>
    /// Clears the registry
    /// </summary>
    public static void Clear()
    {
        _equipment.Clear();
        _byType.Clear();
    }

    /// <summary>
    /// Loads equipment from JSON data
    /// </summary>
    public static void LoadFromJson(string json)
    {
        Clear();
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var obj = (JObject)item;
            var typeStr = obj["type"]?.ToString();
            
            if (Enum.TryParse<EquipmentType>(typeStr, out var type))
            {
                Equipment equipment = type switch
                {
                    EquipmentType.Weapon => new Weapon(),
                    EquipmentType.Shield => new Shield(),
                    EquipmentType.Engine => new Engine(),
                    EquipmentType.CargoExpansion => new CargoHold(),
                    _ => new EquipmentBase()
                };
                
                equipment.Deserialize(obj);
                Register(equipment);
            }
        }
    }

    /// <summary>
    /// Base equipment class for types without specific implementation
    /// </summary>
    private sealed class EquipmentBase : Equipment
    {
        // Uses base implementation
    }
}