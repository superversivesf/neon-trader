using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Upgrade slot types
/// </summary>
public enum UpgradeSlotType
{
    Weapon,         // Weapon hardpoints
    Utility,        // Utility hardpoints (scanners, countermeasures, etc.)
    Internal,       // Internal modules (engine, shield, reactor, cargo)
    Armor,          // Armor plates
    Structural,     // Structural reinforcement
    Electronic,     // Electronic warfare, hacking
    Propulsion,     // Engine/maneuvering
    Power,          // Reactor, capacitor, power distribution
    Cooling,        // Heat management
    Sensor,         // Scanners, targeting
    Cargo,          // Cargo expansion
    Crew,           // Crew quarters, life support
    Hangar,         // Fighter/drone hangar
    Special         // Unique/special slots
}

/// <summary>
/// Upgrade slot sizes
/// </summary>
public enum UpgradeSlotSize
{
    Tiny,       // Size 1 - very small
    Small,      // Size 2 - small
    Medium,     // Size 3 - standard
    Large,      // Size 4 - large
    Huge,       // Size 5 - very large
    Capital,    // Size 6 - capital ship
    Universal   // Accepts any size (with adapter)
}

/// <summary>
/// Represents a single upgrade slot on a ship
/// </summary>
public sealed class UpgradeSlot : ISaveable
{
    /// <summary>
    /// Unique slot identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slot type
    /// </summary>
    public UpgradeSlotType Type { get; set; } = UpgradeSlotType.Internal;

    /// <summary>
    /// Slot size
    /// </summary>
    public UpgradeSlotSize Size { get; set; } = UpgradeSlotSize.Medium;

    /// <summary>
    /// Allowed equipment types for this slot
    /// </summary>
    public HashSet<EquipmentType> AllowedEquipmentTypes { get; } = new();

    /// <summary>
    /// Allowed equipment sizes (smaller can fit in larger with adapter)
    /// </summary>
    public HashSet<EquipmentSize> AllowedEquipmentSizes { get; } = new();

    /// <summary>
    /// Disallowed equipment types (blacklist)
    /// </summary>
    public HashSet<EquipmentType> DisallowedEquipmentTypes { get; } = new();

    /// <summary>
    /// Required tags on equipment (all must match)
    /// </summary>
    public HashSet<string> RequiredTags { get; } = new();

    /// <summary>
    /// Forbidden tags on equipment
    /// </summary>
    public HashSet<string> ForbiddenTags { get; } = new();

    /// <summary>
    /// Currently installed equipment ID
    /// </summary>
    public string? InstalledEquipmentId { get; set; }

    /// <summary>
    /// Whether this slot is unlocked/available
    /// </summary>
    public bool IsUnlocked { get; set; } = true;

    /// <summary>
    /// Unlock requirement (reputation tier, faction, etc.)
    /// </summary>
    public string UnlockRequirement { get; set; } = string.Empty;

    /// <summary>
    /// Unlock cost in credits
    /// </summary>
    public long UnlockCost { get; set; } = 0;

    /// <summary>
    /// Slot position for visual representation
    /// </summary>
    public (double X, double Y, double Z) Position { get; set; } = (0, 0, 0);

    /// <summary>
    /// Slot orientation (for hardpoints)
    /// </summary>
    public (double Pitch, double Yaw, double Roll) Orientation { get; set; } = (0, 0, 0);

    /// <summary>
    /// Arc limits for gimballed/turret mounts (degrees)
    /// </summary>
    public (double MinPitch, double MaxPitch, double MinYaw, double MaxYaw)? ArcLimits { get; set; }

    /// <summary>
    /// Whether slot is damaged
    /// </summary>
    public bool IsDamaged { get; set; } = false;

    /// <summary>
    /// Damage severity (0.0 - 1.0)
    /// </summary>
    public double DamageSeverity { get; set; } = 0;

    /// <summary>
    /// Power allocation priority (higher = gets power first)
    /// </summary>
    public int PowerPriority { get; set; } = 0;

    /// <summary>
    /// Cooling allocation priority
    /// </summary>
    public int CoolingPriority { get; set; } = 0;

    /// <summary>
    /// Tags for special properties
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    // ISaveable implementation
    public string SaveId => $"upgradeslot_{Id}";
    public int SaveVersion => 1;

    /// <summary>
    /// Checks if equipment can be installed in this slot
    /// </summary>
    public bool CanInstall(Equipment equipment)
    {
        if (!IsUnlocked)
            return false;

        if (IsDamaged && DamageSeverity > 0.5)
            return false;

        if (InstalledEquipmentId != null)
            return false;

        // Check allowed types
        if (AllowedEquipmentTypes.Count > 0 && !AllowedEquipmentTypes.Contains(equipment.Type))
            return false;

        // Check disallowed types
        if (DisallowedEquipmentTypes.Contains(equipment.Type))
            return false;

        // Check allowed sizes (smaller can fit in larger)
        if (AllowedEquipmentSizes.Count > 0)
        {
            var equipmentSizeOrder = new[] { EquipmentSize.Tiny, EquipmentSize.Small, EquipmentSize.Medium, EquipmentSize.Large, EquipmentSize.Huge, EquipmentSize.Modular };
            var slotSizeOrder = new[] { UpgradeSlotSize.Tiny, UpgradeSlotSize.Small, UpgradeSlotSize.Medium, UpgradeSlotSize.Large, UpgradeSlotSize.Huge, UpgradeSlotSize.Capital, UpgradeSlotSize.Universal };
            
            var eqIndex = Array.IndexOf(equipmentSizeOrder, equipment.Size);
            var slotIndex = Array.IndexOf(slotSizeOrder, Size);

            if (eqIndex > slotIndex && Size != UpgradeSlotSize.Universal)
                return false; // Equipment too large for slot
        }

        // Check required tags
        foreach (var tag in RequiredTags)
        {
            if (!equipment.Tags.Contains(tag))
                return false;
        }

        // Check forbidden tags
        foreach (var tag in ForbiddenTags)
        {
            if (equipment.Tags.Contains(tag))
                return false;
        }

        // Check minimum ship size
        // This would need ship context, but we can check basic compatibility

        return true;
    }

    /// <summary>
    /// Installs equipment in this slot
    /// </summary>
    public bool InstallEquipment(Equipment equipment)
    {
        if (!CanInstall(equipment))
            return false;

        InstalledEquipmentId = equipment.Id;
        // Ship reference would be passed in real usage; handle null gracefully for testing
        if (equipment.IsInstalled == false)
        {
            equipment.IsInstalled = true;
            equipment.InstalledSlotId = Id;
        }
        return true;
    }

    /// <summary>
    /// Removes equipment from this slot
    /// </summary>
    public string? RemoveEquipment()
    {
        var equipmentId = InstalledEquipmentId;
        if (equipmentId != null)
        {
            InstalledEquipmentId = null;
            // Equipment.OnRemoved() would be called by ship
        }
        return equipmentId;
    }

    /// <summary>
    /// Gets installed equipment
    /// </summary>
    public Equipment? GetInstalledEquipment()
    {
        if (InstalledEquipmentId != null)
            return EquipmentRegistry.Get(InstalledEquipmentId);
        return null;
    }

    /// <summary>
    /// Damages the slot
    /// </summary>
    public void Damage(double severity)
    {
        IsDamaged = true;
        DamageSeverity = Math.Clamp(severity, 0, 1);
        
        if (DamageSeverity >= 1.0)
        {
            // Slot destroyed - remove equipment
            RemoveEquipment();
        }
    }

    /// <summary>
    /// Repairs the slot
    /// </summary>
    public void Repair(double amount)
    {
        DamageSeverity = Math.Max(0, DamageSeverity - amount);
        if (DamageSeverity <= 0)
        {
            IsDamaged = false;
            DamageSeverity = 0;
        }
    }

    /// <summary>
    /// Fully repairs the slot
    /// </summary>
    public void FullRepair()
    {
        IsDamaged = false;
        DamageSeverity = 0;
    }

    /// <summary>
    /// Unlocks the slot
    /// </summary>
    public void Unlock()
    {
        IsUnlocked = true;
        UnlockRequirement = string.Empty;
        UnlockCost = 0;
    }

    /// <summary>
    /// Locks the slot with requirement
    /// </summary>
    public void Lock(string requirement, long cost)
    {
        IsUnlocked = false;
        UnlockRequirement = requirement;
        UnlockCost = cost;
        
        // Remove any installed equipment
        RemoveEquipment();
    }

    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["type"] = Type.ToString(),
            ["size"] = Size.ToString(),
            ["allowedEquipmentTypes"] = JArray.FromObject(AllowedEquipmentTypes.Select(t => t.ToString())),
            ["allowedEquipmentSizes"] = JArray.FromObject(AllowedEquipmentSizes.Select(s => s.ToString())),
            ["disallowedEquipmentTypes"] = JArray.FromObject(DisallowedEquipmentTypes.Select(t => t.ToString())),
            ["requiredTags"] = JArray.FromObject(RequiredTags),
            ["forbiddenTags"] = JArray.FromObject(ForbiddenTags),
            ["installedEquipmentId"] = InstalledEquipmentId ?? string.Empty,
            ["isUnlocked"] = IsUnlocked,
            ["unlockRequirement"] = UnlockRequirement,
            ["unlockCost"] = UnlockCost,
            ["positionX"] = Position.X,
            ["positionY"] = Position.Y,
            ["positionZ"] = Position.Z,
            ["orientationPitch"] = Orientation.Pitch,
            ["orientationYaw"] = Orientation.Yaw,
            ["orientationRoll"] = Orientation.Roll,
            ["arcLimitsMinPitch"] = ArcLimits?.MinPitch ?? 0,
            ["arcLimitsMaxPitch"] = ArcLimits?.MaxPitch ?? 0,
            ["arcLimitsMinYaw"] = ArcLimits?.MinYaw ?? 0,
            ["arcLimitsMaxYaw"] = ArcLimits?.MaxYaw ?? 0,
            ["hasArcLimits"] = ArcLimits.HasValue,
            ["isDamaged"] = IsDamaged,
            ["damageSeverity"] = DamageSeverity,
            ["powerPriority"] = PowerPriority,
            ["coolingPriority"] = CoolingPriority,
            ["tags"] = JArray.FromObject(Tags)
        };
    }

    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<UpgradeSlotType>(data["type"]?.ToString(), out var type))
            Type = type;

        if (Enum.TryParse<UpgradeSlotSize>(data["size"]?.ToString(), out var size))
            Size = size;

        AllowedEquipmentTypes.Clear();
        if (data["allowedEquipmentTypes"] is JArray allowedTypesArray)
        {
            foreach (var item in allowedTypesArray)
            {
                if (Enum.TryParse<EquipmentType>(item?.ToString(), out var eqType))
                    AllowedEquipmentTypes.Add(eqType);
            }
        }

        AllowedEquipmentSizes.Clear();
        if (data["allowedEquipmentSizes"] is JArray allowedSizesArray)
        {
            foreach (var item in allowedSizesArray)
            {
                if (Enum.TryParse<EquipmentSize>(item?.ToString(), out var eqSize))
                    AllowedEquipmentSizes.Add(eqSize);
            }
        }

        DisallowedEquipmentTypes.Clear();
        if (data["disallowedEquipmentTypes"] is JArray disallowedTypesArray)
        {
            foreach (var item in disallowedTypesArray)
            {
                if (Enum.TryParse<EquipmentType>(item?.ToString(), out var eqType))
                    DisallowedEquipmentTypes.Add(eqType);
            }
        }

        RequiredTags.Clear();
        if (data["requiredTags"] is JArray requiredTagsArray)
        {
            foreach (var tag in requiredTagsArray)
            {
                var tagStr = tag?.ToString();
                if (!string.IsNullOrEmpty(tagStr))
                    RequiredTags.Add(tagStr);
            }
        }

        ForbiddenTags.Clear();
        if (data["forbiddenTags"] is JArray forbiddenTagsArray)
        {
            foreach (var tag in forbiddenTagsArray)
            {
                var tagStr = tag?.ToString();
                if (!string.IsNullOrEmpty(tagStr))
                    ForbiddenTags.Add(tagStr);
            }
        }

        InstalledEquipmentId = data["installedEquipmentId"]?.ToString();
        if (string.IsNullOrEmpty(InstalledEquipmentId))
            InstalledEquipmentId = null;

        IsUnlocked = data["isUnlocked"]?.ToObject<bool>() ?? true;
        UnlockRequirement = data["unlockRequirement"]?.ToString() ?? string.Empty;
        UnlockCost = data["unlockCost"]?.ToObject<long>() ?? 0;

        Position = (
            data["positionX"]?.ToObject<double>() ?? 0,
            data["positionY"]?.ToObject<double>() ?? 0,
            data["positionZ"]?.ToObject<double>() ?? 0
        );

        Orientation = (
            data["orientationPitch"]?.ToObject<double>() ?? 0,
            data["orientationYaw"]?.ToObject<double>() ?? 0,
            data["orientationRoll"]?.ToObject<double>() ?? 0
        );

        if (data["hasArcLimits"]?.ToObject<bool>() ?? false)
        {
            ArcLimits = (
                data["arcLimitsMinPitch"]?.ToObject<double>() ?? 0,
                data["arcLimitsMaxPitch"]?.ToObject<double>() ?? 0,
                data["arcLimitsMinYaw"]?.ToObject<double>() ?? 0,
                data["arcLimitsMaxYaw"]?.ToObject<double>() ?? 0
            );
        }
        else
        {
            ArcLimits = null;
        }

        IsDamaged = data["isDamaged"]?.ToObject<bool>() ?? false;
        DamageSeverity = data["damageSeverity"]?.ToObject<double>() ?? 0;
        PowerPriority = data["powerPriority"]?.ToObject<int>() ?? 0;
        CoolingPriority = data["coolingPriority"]?.ToObject<int>() ?? 0;

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
    }

    /// <summary>
    /// Creates a copy of this slot
    /// </summary>
    public UpgradeSlot Clone()
    {
        var clone = new UpgradeSlot
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Size = Size,
            InstalledEquipmentId = InstalledEquipmentId,
            IsUnlocked = IsUnlocked,
            UnlockRequirement = UnlockRequirement,
            UnlockCost = UnlockCost,
            Position = Position,
            Orientation = Orientation,
            ArcLimits = ArcLimits,
            IsDamaged = IsDamaged,
            DamageSeverity = DamageSeverity,
            PowerPriority = PowerPriority,
            CoolingPriority = CoolingPriority
        };

        foreach (var t in AllowedEquipmentTypes)
            clone.AllowedEquipmentTypes.Add(t);

        foreach (var s in AllowedEquipmentSizes)
            clone.AllowedEquipmentSizes.Add(s);

        foreach (var t in DisallowedEquipmentTypes)
            clone.DisallowedEquipmentTypes.Add(t);

        foreach (var tag in RequiredTags)
            clone.RequiredTags.Add(tag);

        foreach (var tag in ForbiddenTags)
            clone.ForbiddenTags.Add(tag);

        foreach (var tag in Tags)
            clone.Tags.Add(tag);

        return clone;
    }

    /// <summary>
    /// Validates the slot data
    /// </summary>
    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "Slot ID cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Slot name cannot be empty";
            return false;
        }

        if (DamageSeverity < 0 || DamageSeverity > 1)
        {
            error = "Damage severity must be between 0 and 1";
            return false;
        }

        if (UnlockCost < 0)
        {
            error = "Unlock cost cannot be negative";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

/// <summary>
/// Upgrade slot collection for a ship
/// </summary>
public sealed class UpgradeSlotCollection : ISaveable
{
    /// <summary>
    /// All slots by ID
    /// </summary>
    public Dictionary<string, UpgradeSlot> Slots { get; } = new();

    /// <summary>
    /// Ship this collection belongs to
    /// </summary>
    public string ShipId { get; set; } = string.Empty;

    // ISaveable implementation
    public string SaveId => $"upgradeslots_{ShipId}";
    public int SaveVersion => 1;

    /// <summary>
    /// Adds a slot
    /// </summary>
    public void AddSlot(UpgradeSlot slot)
    {
        Slots[slot.Id] = slot;
    }

    /// <summary>
    /// Removes a slot
    /// </summary>
    public bool RemoveSlot(string slotId)
    {
        return Slots.Remove(slotId);
    }

    /// <summary>
    /// Gets a slot by ID
    /// </summary>
    public UpgradeSlot? GetSlot(string slotId)
    {
        Slots.TryGetValue(slotId, out var slot);
        return slot;
    }

    /// <summary>
    /// Gets all slots of a type
    /// </summary>
    public IEnumerable<UpgradeSlot> GetSlotsByType(UpgradeSlotType type)
    {
        return Slots.Values.Where(s => s.Type == type);
    }

    /// <summary>
    /// Gets all unlocked slots of a type
    /// </summary>
    public IEnumerable<UpgradeSlot> GetUnlockedSlotsByType(UpgradeSlotType type)
    {
        return Slots.Values.Where(s => s.Type == type && s.IsUnlocked);
    }

    /// <summary>
    /// Gets all free (unoccupied) slots of a type
    /// </summary>
    public IEnumerable<UpgradeSlot> GetFreeSlotsByType(UpgradeSlotType type)
    {
        return Slots.Values.Where(s => s.Type == type && s.IsUnlocked && s.InstalledEquipmentId == null && !s.IsDamaged);
    }

    /// <summary>
    /// Finds first free slot that can accept equipment
    /// </summary>
    public UpgradeSlot? FindFreeSlotFor(Equipment equipment)
    {
        return Slots.Values
            .Where(s => s.IsUnlocked && s.InstalledEquipmentId == null && !s.IsDamaged)
            .FirstOrDefault(s => s.CanInstall(equipment));
    }

    /// <summary>
    /// Installs equipment in the best available slot
    /// </summary>
    public bool InstallEquipment(Equipment equipment, out string? slotId)
    {
        var slot = FindFreeSlotFor(equipment);
        if (slot != null)
        {
            slotId = slot.Id;
            return slot.InstallEquipment(equipment);
        }
        slotId = null;
        return false;
    }

    /// <summary>
    /// Gets all installed equipment
    /// </summary>
    public IEnumerable<Equipment> GetInstalledEquipment()
    {
        return Slots.Values
            .Where(s => s.InstalledEquipmentId != null)
            .Select(s => EquipmentRegistry.Get(s.InstalledEquipmentId!))
            .Where(e => e != null)!;
    }

    /// <summary>
    /// Gets installed equipment by type
    /// </summary>
    public IEnumerable<Equipment> GetInstalledEquipmentByType(EquipmentType type)
    {
        return GetInstalledEquipment().Where(e => e.Type == type);
    }

    /// <summary>
    /// Total power draw of all installed equipment
    /// </summary>
    public double TotalPowerDraw => GetInstalledEquipment().Sum(e => e.PowerDraw);

    /// <summary>
    /// Total heat generation of all installed equipment
    /// </summary>
    public double TotalHeatGeneration => GetInstalledEquipment().Sum(e => e.HeatGeneration);

    /// <summary>
    /// Total mass of all installed equipment
    /// </summary>
    public double TotalMass => GetInstalledEquipment().Sum(e => e.Mass);

    public JObject Serialize()
    {
        return new JObject
        {
            ["shipId"] = ShipId,
            ["slots"] = JObject.FromObject(Slots.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Serialize()))
        };
    }

    public void Deserialize(JObject data)
    {
        ShipId = data["shipId"]?.ToString() ?? string.Empty;

        Slots.Clear();
        if (data["slots"] is JObject slotsObj)
        {
            foreach (var kvp in slotsObj)
            {
                var slot = new UpgradeSlot();
                slot.Deserialize((JObject)kvp.Value);
                Slots[kvp.Key] = slot;
            }
        }
    }
}