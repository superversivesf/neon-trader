using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Ship instance representing a player's or NPC's ship with runtime state
/// </summary>
public sealed class Ship : ISaveable
{
    /// <summary>
    /// Unique identifier for this ship instance
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name (can be customized by player)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the ship class definition
    /// </summary>
    public string ShipClassId { get; set; } = string.Empty;

    /// <summary>
    /// Current hull integrity (0 = destroyed)
    /// </summary>
    public int CurrentHull { get; set; } = 100;

    /// <summary>
    /// Maximum hull integrity (base + upgrades)
    /// </summary>
    public int MaxHull { get; set; } = 100;

    /// <summary>
    /// Current shield points
    /// </summary>
    public int CurrentShield { get; set; } = 0;

    /// <summary>
    /// Maximum shield capacity (base + upgrades)
    /// </summary>
    public int MaxShield { get; set; } = 0;

    /// <summary>
    /// Shield recharge rate per second
    /// </summary>
    public double ShieldRechargeRate { get; set; } = 0;

    /// <summary>
    /// Current cargo capacity in tons
    /// </summary>
    public int CargoCapacity { get; set; } = 50;

    /// <summary>
    /// Current fuel capacity
    /// </summary>
    public int FuelCapacity { get; set; } = 100;

    /// <summary>
    /// Current fuel level
    /// </summary>
    public int CurrentFuel { get; set; } = 100;

    /// <summary>
    /// Fuel consumption per light-year
    /// </summary>
    public double FuelConsumption { get; set; } = 1.0;

    /// <summary>
    /// Maximum speed (units per second)
    /// </summary>
    public double MaxSpeed { get; set; } = 100;

    /// <summary>
    /// Acceleration (units per second^2)
    /// </summary>
    public double Acceleration { get; set; } = 50;

    /// <summary>
    /// Turn rate (degrees per second)
    /// </summary>
    public double TurnRate { get; set; } = 90;

    /// <summary>
    /// Number of weapon hardpoints available
    /// </summary>
    public int WeaponHardpoints { get; set; } = 2;

    /// <summary>
    /// Number of utility hardpoints available
    /// </summary>
    public int UtilityHardpoints { get; set; } = 1;

    /// <summary>
    /// Number of upgrade slots available
    /// </summary>
    public int UpgradeSlots { get; set; } = 3;

    /// <summary>
    /// Currently installed equipment by slot ID
    /// </summary>
    public Dictionary<string, string> InstalledEquipment { get; } = new();

    /// <summary>
    /// Installed upgrades (upgrade IDs)
    /// </summary>
    public HashSet<string> InstalledUpgrades { get; } = new();

    /// <summary>
    /// Cargo contents (commodityId -> quantity)
    /// </summary>
    public Dictionary<string, int> Cargo { get; } = new();

    /// <summary>
    /// Ship condition (0.0 = destroyed, 1.0 = pristine)
    /// </summary>
    public double Condition { get; set; } = 1.0;

    /// <summary>
    /// Total distance traveled in light-years
    /// </summary>
    public double TotalDistanceTraveled { get; set; } = 0;

    /// <summary>
    /// Total credits earned with this ship
    /// </summary>
    public long TotalCreditsEarned { get; set; } = 0;

    /// <summary>
    /// Ship's home port / registration
    /// </summary>
    public string HomePort { get; set; } = string.Empty;

    /// <summary>
    /// Purchase date
    /// </summary>
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tags for special properties
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    // ISaveable implementation
    public string SaveId => $"ship_{Id}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the ship to JSON
    /// </summary>
    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["shipClassId"] = ShipClassId,
            ["currentHull"] = CurrentHull,
            ["maxHull"] = MaxHull,
            ["currentShield"] = CurrentShield,
            ["maxShield"] = MaxShield,
            ["shieldRechargeRate"] = ShieldRechargeRate,
            ["cargoCapacity"] = CargoCapacity,
            ["fuelCapacity"] = FuelCapacity,
            ["currentFuel"] = CurrentFuel,
            ["fuelConsumption"] = FuelConsumption,
            ["maxSpeed"] = MaxSpeed,
            ["acceleration"] = Acceleration,
            ["turnRate"] = TurnRate,
            ["weaponHardpoints"] = WeaponHardpoints,
            ["utilityHardpoints"] = UtilityHardpoints,
            ["upgradeSlots"] = UpgradeSlots,
            ["installedEquipment"] = JObject.FromObject(InstalledEquipment),
            ["installedUpgrades"] = JArray.FromObject(InstalledUpgrades),
            ["cargo"] = JObject.FromObject(Cargo),
            ["condition"] = Condition,
            ["totalDistanceTraveled"] = TotalDistanceTraveled,
            ["totalCreditsEarned"] = TotalCreditsEarned,
            ["homePort"] = HomePort,
            ["purchaseDate"] = PurchaseDate.ToString("o"),
            ["tags"] = JArray.FromObject(Tags)
        };
    }

    /// <summary>
    /// Deserialize the ship from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;
        ShipClassId = data["shipClassId"]?.ToString() ?? string.Empty;
        CurrentHull = data["currentHull"]?.ToObject<int>() ?? 100;
        MaxHull = data["maxHull"]?.ToObject<int>() ?? 100;
        CurrentShield = data["currentShield"]?.ToObject<int>() ?? 0;
        MaxShield = data["maxShield"]?.ToObject<int>() ?? 0;
        ShieldRechargeRate = data["shieldRechargeRate"]?.ToObject<double>() ?? 0;
        CargoCapacity = data["cargoCapacity"]?.ToObject<int>() ?? 50;
        FuelCapacity = data["fuelCapacity"]?.ToObject<int>() ?? 100;
        CurrentFuel = data["currentFuel"]?.ToObject<int>() ?? 100;
        FuelConsumption = data["fuelConsumption"]?.ToObject<double>() ?? 1.0;
        MaxSpeed = data["maxSpeed"]?.ToObject<double>() ?? 100;
        Acceleration = data["acceleration"]?.ToObject<double>() ?? 50;
        TurnRate = data["turnRate"]?.ToObject<double>() ?? 90;
        WeaponHardpoints = data["weaponHardpoints"]?.ToObject<int>() ?? 2;
        UtilityHardpoints = data["utilityHardpoints"]?.ToObject<int>() ?? 1;
        UpgradeSlots = data["upgradeSlots"]?.ToObject<int>() ?? 3;
        Condition = data["condition"]?.ToObject<double>() ?? 1.0;
        TotalDistanceTraveled = data["totalDistanceTraveled"]?.ToObject<double>() ?? 0;
        TotalCreditsEarned = data["totalCreditsEarned"]?.ToObject<long>() ?? 0;
        HomePort = data["homePort"]?.ToString() ?? string.Empty;

        if (data["purchaseDate"] != null)
        {
            PurchaseDate = DateTime.Parse(data["purchaseDate"]!.ToString());
        }

        InstalledEquipment.Clear();
        if (data["installedEquipment"] is JObject eqObj)
        {
            foreach (var kvp in eqObj)
            {
                var value = kvp.Value?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    InstalledEquipment[kvp.Key] = value;
                }
            }
        }

        InstalledUpgrades.Clear();
        if (data["installedUpgrades"] is JArray upgradesArray)
        {
            foreach (var upgrade in upgradesArray)
            {
                var upgradeStr = upgrade?.ToString();
                if (!string.IsNullOrEmpty(upgradeStr))
                {
                    InstalledUpgrades.Add(upgradeStr);
                }
            }
        }

        Cargo.Clear();
        if (data["cargo"] is JObject cargoObj)
        {
            foreach (var kvp in cargoObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                {
                    Cargo[kvp.Key] = value.Value;
                }
            }
        }

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
    /// Gets the ship class definition
    /// </summary>
    public ShipClass? GetShipClass()
    {
        return ShipClassRegistry.Get(ShipClassId);
    }

    /// <summary>
    /// Calculates current cargo used in tons
    /// </summary>
    public int GetCargoUsed()
    {
        return Cargo.Values.Sum();
    }

    /// <summary>
    /// Calculates available cargo space
    /// </summary>
    public int GetAvailableCargoSpace()
    {
        return CargoCapacity - GetCargoUsed();
    }

    /// <summary>
    /// Checks if ship has enough cargo space for quantity
    /// </summary>
    public bool HasCargoSpace(int quantity)
    {
        return GetAvailableCargoSpace() >= quantity;
    }

    /// <summary>
    /// Adds cargo to the ship
    /// </summary>
    public bool AddCargo(string commodityId, int quantity)
    {
        if (!HasCargoSpace(quantity))
            return false;

        if (Cargo.TryGetValue(commodityId, out var current))
        {
            Cargo[commodityId] = current + quantity;
        }
        else
        {
            Cargo[commodityId] = quantity;
        }
        return true;
    }

    /// <summary>
    /// Removes cargo from the ship
    /// </summary>
    public bool RemoveCargo(string commodityId, int quantity)
    {
        if (!Cargo.TryGetValue(commodityId, out var current) || current < quantity)
            return false;

        var newQuantity = current - quantity;
        if (newQuantity <= 0)
        {
            Cargo.Remove(commodityId);
        }
        else
        {
            Cargo[commodityId] = newQuantity;
        }
        return true;
    }

    /// <summary>
    /// Gets cargo quantity for a commodity
    /// </summary>
    public int GetCargoQuantity(string commodityId)
    {
        return Cargo.TryGetValue(commodityId, out var qty) ? qty : 0;
    }

    /// <summary>
    /// Consumes fuel for travel
    /// </summary>
    public bool ConsumeFuel(int amount)
    {
        if (CurrentFuel < amount)
            return false;

        CurrentFuel -= amount;
        return true;
    }

    /// <summary>
    /// Refuels the ship
    /// </summary>
    public int Refuel(int amount)
    {
        var space = FuelCapacity - CurrentFuel;
        var actual = Math.Min(amount, space);
        CurrentFuel += actual;
        return actual;
    }

    /// <summary>
    /// Applies damage to shields first, then hull
    /// </summary>
    public void TakeDamage(int damage)
    {
        var shieldDamage = Math.Min(damage, CurrentShield);
        CurrentShield -= shieldDamage;
        damage -= shieldDamage;

        if (damage > 0)
        {
            CurrentHull = Math.Max(0, CurrentHull - damage);
            // Condition degrades with hull damage
            Condition = Math.Max(0, Condition - (damage * 0.01));
        }
    }

    /// <summary>
    /// Repairs hull
    /// </summary>
    public int RepairHull(int amount)
    {
        var missing = MaxHull - CurrentHull;
        var actual = Math.Min(amount, missing);
        CurrentHull += actual;
        Condition = Math.Min(1.0, Condition + (actual * 0.005));
        return actual;
    }

    /// <summary>
    /// Recharges shields
    /// </summary>
    public void RechargeShields(double deltaTime)
    {
        if (MaxShield > 0 && CurrentShield < MaxShield)
        {
            CurrentShield = Math.Min(MaxShield, CurrentShield + (int)(ShieldRechargeRate * deltaTime));
        }
    }

    /// <summary>
    /// Checks if ship is destroyed
    /// </summary>
    public bool IsDestroyed => CurrentHull <= 0;

    /// <summary>
    /// Checks if ship has equipment installed in a slot
    /// </summary>
    public bool HasEquipmentInSlot(string slotId)
    {
        return InstalledEquipment.ContainsKey(slotId);
    }

    /// <summary>
    /// Gets equipment ID installed in a slot
    /// </summary>
    public string? GetEquipmentInSlot(string slotId)
    {
        return InstalledEquipment.TryGetValue(slotId, out var eqId) ? eqId : null;
    }

    /// <summary>
    /// Installs equipment in a slot
    /// </summary>
    public bool InstallEquipment(string slotId, string equipmentId)
    {
        if (InstalledEquipment.ContainsKey(slotId))
            return false; // Slot occupied

        InstalledEquipment[slotId] = equipmentId;
        return true;
    }

    /// <summary>
    /// Removes equipment from a slot
    /// </summary>
    public bool RemoveEquipment(string slotId, out string equipmentId)
    {
        if (InstalledEquipment.TryGetValue(slotId, out equipmentId))
        {
            InstalledEquipment.Remove(slotId);
            return true;
        }
        equipmentId = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets count of free weapon hardpoints
    /// </summary>
    public int GetFreeWeaponHardpoints()
    {
        var used = InstalledEquipment.Count(kvp => 
        {
            var eq = EquipmentRegistry.Get(kvp.Value);
            return eq?.Type == EquipmentType.Weapon;
        });
        return WeaponHardpoints - used;
    }

    /// <summary>
    /// Gets count of free utility hardpoints
    /// </summary>
    public int GetFreeUtilityHardpoints()
    {
        var used = InstalledEquipment.Count(kvp => 
        {
            var eq = EquipmentRegistry.Get(kvp.Value);
            return eq?.Type != EquipmentType.Weapon;
        });
        return UtilityHardpoints - used;
    }

    /// <summary>
    /// Creates a copy of this ship
    /// </summary>
    public Ship Clone()
    {
        var clone = new Ship
        {
            Id = Id,
            Name = Name,
            ShipClassId = ShipClassId,
            CurrentHull = CurrentHull,
            MaxHull = MaxHull,
            CurrentShield = CurrentShield,
            MaxShield = MaxShield,
            ShieldRechargeRate = ShieldRechargeRate,
            CargoCapacity = CargoCapacity,
            FuelCapacity = FuelCapacity,
            CurrentFuel = CurrentFuel,
            FuelConsumption = FuelConsumption,
            MaxSpeed = MaxSpeed,
            Acceleration = Acceleration,
            TurnRate = TurnRate,
            WeaponHardpoints = WeaponHardpoints,
            UtilityHardpoints = UtilityHardpoints,
            UpgradeSlots = UpgradeSlots,
            Condition = Condition,
            TotalDistanceTraveled = TotalDistanceTraveled,
            TotalCreditsEarned = TotalCreditsEarned,
            HomePort = HomePort,
            PurchaseDate = PurchaseDate
        };

        foreach (var kvp in InstalledEquipment)
            clone.InstalledEquipment[kvp.Key] = kvp.Value;

        foreach (var upgrade in InstalledUpgrades)
            clone.InstalledUpgrades.Add(upgrade);

        foreach (var kvp in Cargo)
            clone.Cargo[kvp.Key] = kvp.Value;

        foreach (var tag in Tags)
            clone.Tags.Add(tag);

        return clone;
    }

    /// <summary>
    /// Validates the ship data
    /// </summary>
    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "Ship ID cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ShipClassId))
        {
            error = "Ship class ID cannot be empty";
            return false;
        }

        if (CurrentHull < 0 || CurrentHull > MaxHull)
        {
            error = "Current hull must be between 0 and max hull";
            return false;
        }

        if (MaxHull <= 0)
        {
            error = "Max hull must be positive";
            return false;
        }

        if (CurrentShield < 0 || CurrentShield > MaxShield)
        {
            error = "Current shield must be between 0 and max shield";
            return false;
        }

        if (MaxShield < 0)
        {
            error = "Max shield cannot be negative";
            return false;
        }

        if (CargoCapacity < 0)
        {
            error = "Cargo capacity cannot be negative";
            return false;
        }

        if (FuelCapacity <= 0)
        {
            error = "Fuel capacity must be positive";
            return false;
        }

        if (CurrentFuel < 0 || CurrentFuel > FuelCapacity)
        {
            error = "Current fuel must be between 0 and fuel capacity";
            return false;
        }

        if (FuelConsumption <= 0)
        {
            error = "Fuel consumption must be positive";
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
}

/// <summary>
/// Static registry for ship instances
/// </summary>
public static class ShipRegistry
{
    private static readonly Dictionary<string, Ship> _ships = new();

    /// <summary>
    /// Gets a ship by ID
    /// </summary>
    public static Ship? Get(string id)
    {
        _ships.TryGetValue(id, out var ship);
        return ship;
    }

    /// <summary>
    /// Gets all ships
    /// </summary>
    public static IReadOnlyCollection<Ship> All => _ships.Values;

    /// <summary>
    /// Registers a ship
    /// </summary>
    public static void Register(Ship ship)
    {
        if (ship.Validate(out var error))
        {
            _ships[ship.Id] = ship;
        }
        else
        {
            throw new ArgumentException($"Invalid ship: {error}");
        }
    }

    /// <summary>
    /// Unregisters a ship
    /// </summary>
    public static bool Unregister(string id)
    {
        return _ships.Remove(id);
    }

    /// <summary>
    /// Clears the registry
    /// </summary>
    public static void Clear()
    {
        _ships.Clear();
    }
}

