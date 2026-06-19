using System.Collections.Concurrent;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Core;

/// <summary>
/// Central game state container holding all mutable game data.
/// This is the single source of truth for the game's current state.
/// </summary>
public sealed class GameState : ISaveable
{
    // Player state
    public string PlayerName { get; set; } = "Pilot";
    public long Credits { get; set; } = 10000;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    
    // Location state
    public string CurrentLocation { get; set; } = "Neon Station";
    public string PreviousLocation { get; set; } = "";
    
    // Time state
    public DateTime GameTime { get; set; } = new DateTime(2087, 1, 1, 8, 0, 0);
    public TimeSpan TimeScale { get; set; } = TimeSpan.FromMinutes(5); // 5 game minutes per real second
    
    // Ship state
    public string ShipId { get; set; } = "starter_ship";
    public int CargoCapacity { get; set; } = 50;
    public int FuelCapacity { get; set; } = 100;
    public int CurrentFuel { get; set; } = 100;
    
    // Cargo (commodityId -> quantity)
    public ConcurrentDictionary<string, int> Cargo { get; } = new();
    
    // Ship upgrades
    public HashSet<string> InstalledUpgrades { get; } = new();
    
    // Market prices (commodityId -> price)
    public ConcurrentDictionary<string, decimal> MarketPrices { get; } = new();
    
    // Available missions
    public List<MissionInfo> AvailableMissions { get; } = new();
    
    // Active mission
    public MissionInfo? ActiveMission { get; set; }
    
    // Game settings
    public GameSettings Settings { get; } = new();
    
    // Statistics
    public GameStatistics Statistics { get; } = new();
    
    // GameState properties
    public string SaveId => "gamestate";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the game state to JSON
    /// </summary>
    public JObject Serialize()
    {
        var cargoDict = new Dictionary<string, int>();
        foreach (var kvp in Cargo)
        {
            cargoDict[kvp.Key] = kvp.Value;
        }

        var marketDict = new Dictionary<string, decimal>();
        foreach (var kvp in MarketPrices)
        {
            marketDict[kvp.Key] = kvp.Value;
        }

        return new JObject
        {
            ["playerName"] = PlayerName,
            ["credits"] = Credits,
            ["health"] = Health,
            ["maxHealth"] = MaxHealth,
            ["currentLocation"] = CurrentLocation,
            ["previousLocation"] = PreviousLocation,
            ["gameTime"] = GameTime.ToString("o"),
            ["timeScale"] = TimeScale.ToString(),
            ["shipId"] = ShipId,
            ["cargoCapacity"] = CargoCapacity,
            ["fuelCapacity"] = FuelCapacity,
            ["currentFuel"] = CurrentFuel,
            ["cargo"] = JObject.FromObject(cargoDict),
            ["installedUpgrades"] = JArray.FromObject(InstalledUpgrades),
            ["marketPrices"] = JObject.FromObject(marketDict),
            ["availableMissions"] = JArray.FromObject(AvailableMissions),
            ["activeMission"] = ActiveMission != null ? JObject.FromObject(ActiveMission) : JValue.CreateNull(),
            ["settings"] = Settings.Serialize(),
            ["statistics"] = Statistics.Serialize(),
            ["saveVersion"] = SaveVersion
        };
    }

    /// <summary>
    /// Deserialize the game state from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        PlayerName = data["playerName"]?.ToString() ?? "Pilot";
        Credits = data["credits"]?.ToObject<long>() ?? 10000;
        Health = data["health"]?.ToObject<int>() ?? 100;
        MaxHealth = data["maxHealth"]?.ToObject<int>() ?? 100;
        CurrentLocation = data["currentLocation"]?.ToString() ?? "Neon Station";
        PreviousLocation = data["previousLocation"]?.ToString() ?? "";
        
        if (data["gameTime"] != null)
        {
            GameTime = DateTime.Parse(data["gameTime"]!.ToString());
        }
        
        if (data["timeScale"] != null)
        {
            TimeScale = TimeSpan.Parse(data["timeScale"]!.ToString());
        }
        
        ShipId = data["shipId"]?.ToString() ?? "starter_ship";
        CargoCapacity = data["cargoCapacity"]?.ToObject<int>() ?? 50;
        FuelCapacity = data["fuelCapacity"]?.ToObject<int>() ?? 100;
        CurrentFuel = data["currentFuel"]?.ToObject<int>() ?? 100;

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

        MarketPrices.Clear();
        if (data["marketPrices"] is JObject marketObj)
        {
            foreach (var kvp in marketObj)
            {
                var value = kvp.Value?.ToObject<decimal>();
                if (value.HasValue)
                {
                    MarketPrices[kvp.Key] = value.Value;
                }
            }
        }

        AvailableMissions.Clear();
        if (data["availableMissions"] is JArray missionsArray)
        {
            AvailableMissions.AddRange(missionsArray.ToObject<List<MissionInfo>>() ?? new());
        }

        if (data["activeMission"] is JObject missionObj && missionObj.Type != JTokenType.Null)
        {
            ActiveMission = missionObj.ToObject<MissionInfo>();
        }
        else
        {
            ActiveMission = null;
        }

        if (data["settings"] is JObject settingsObj)
        {
            Settings.Deserialize(settingsObj);
        }

        if (data["statistics"] is JObject statsObj)
        {
            Statistics.Deserialize(statsObj);
        }
    }

    /// <summary>
    /// Add cargo to the player's hold
    /// </summary>
    public bool AddCargo(string commodityId, int quantity)
    {
        var currentTotal = Cargo.Values.Sum();
        if (currentTotal + quantity > CargoCapacity)
        {
            return false; // Not enough space
        }
        
        Cargo.AddOrUpdate(commodityId, quantity, (_, existing) => existing + quantity);
        return true;
    }

    /// <summary>
    /// Remove cargo from the player's hold
    /// </summary>
    public bool RemoveCargo(string commodityId, int quantity)
    {
        if (!Cargo.TryGetValue(commodityId, out var current) || current < quantity)
        {
            return false; // Not enough cargo
        }
        
        var newQuantity = current - quantity;
        if (newQuantity <= 0)
        {
            Cargo.TryRemove(commodityId, out _);
        }
        else
        {
            Cargo[commodityId] = newQuantity;
        }
        return true;
    }

    /// <summary>
    /// Get current cargo quantity for a commodity
    /// </summary>
    public int GetCargoQuantity(string commodityId)
    {
        return Cargo.TryGetValue(commodityId, out var qty) ? qty : 0;
    }

    /// <summary>
    /// Get total cargo used
    /// </summary>
    public int GetTotalCargoUsed()
    {
        return Cargo.Values.Sum();
    }

    /// <summary>
    /// Get available cargo space
    /// </summary>
    public int GetAvailableCargoSpace()
    {
        return CargoCapacity - GetTotalCargoUsed();
    }
}

/// <summary>
/// Mission information
/// </summary>
public sealed class MissionInfo
{
    public string MissionId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string SourceLocation { get; set; } = "";
    public string DestinationLocation { get; set; } = "";
    public string CommodityId { get; set; } = "";
    public int RequiredQuantity { get; set; }
    public long Reward { get; set; }
    public DateTime ExpiryTime { get; set; }
    public MissionType Type { get; set; }
    public MissionStatus Status { get; set; } = MissionStatus.Available;
}

/// <summary>
/// Mission types
/// </summary>
public enum MissionType
{
    Delivery,
    Procurement,
    Combat,
    Exploration
}

/// <summary>
/// Mission status
/// </summary>
public enum MissionStatus
{
    Available,
    Active,
    Completed,
    Failed,
    Expired
}

/// <summary>
/// Game settings
/// </summary>
public sealed class GameSettings : ISaveable
{
    public string SaveId => "settings";
    public int SaveVersion => 1;

    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool ShowFPS { get; set; } = true;
    public bool EnableSound { get; set; } = false;
    public int UIRefreshRate { get; set; } = 60;
    public int LogicTickRate { get; set; } = 10;

    public JObject Serialize()
    {
        return new JObject
        {
            ["autoSave"] = AutoSave,
            ["autoSaveIntervalMinutes"] = AutoSaveIntervalMinutes,
            ["showFPS"] = ShowFPS,
            ["enableSound"] = EnableSound,
            ["uiRefreshRate"] = UIRefreshRate,
            ["logicTickRate"] = LogicTickRate
        };
    }

    public void Deserialize(JObject data)
    {
        AutoSave = data["autoSave"]?.ToObject<bool>() ?? true;
        AutoSaveIntervalMinutes = data["autoSaveIntervalMinutes"]?.ToObject<int>() ?? 5;
        ShowFPS = data["showFPS"]?.ToObject<bool>() ?? true;
        EnableSound = data["enableSound"]?.ToObject<bool>() ?? false;
        UIRefreshRate = data["uiRefreshRate"]?.ToObject<int>() ?? 60;
        LogicTickRate = data["logicTickRate"]?.ToObject<int>() ?? 10;
    }
}

/// <summary>
/// Game statistics tracking
/// </summary>
public sealed class GameStatistics : ISaveable
{
    public string SaveId => "statistics";
    public int SaveVersion => 1;

    public long TotalCreditsEarned { get; set; }
    public long TotalCreditsSpent { get; set; }
    public int TradesCompleted { get; set; }
    public int DistanceTraveled { get; set; }
    public int MissionsCompleted { get; set; }
    public int MissionsFailed { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public DateTime GameStartTime { get; set; } = DateTime.UtcNow;

    public JObject Serialize()
    {
        return new JObject
        {
            ["totalCreditsEarned"] = TotalCreditsEarned,
            ["totalCreditsSpent"] = TotalCreditsSpent,
            ["tradesCompleted"] = TradesCompleted,
            ["distanceTraveled"] = DistanceTraveled,
            ["missionsCompleted"] = MissionsCompleted,
            ["missionsFailed"] = MissionsFailed,
            ["totalPlayTime"] = TotalPlayTime.ToString(),
            ["gameStartTime"] = GameStartTime.ToString("o")
        };
    }

    public void Deserialize(JObject data)
    {
        TotalCreditsEarned = data["totalCreditsEarned"]?.ToObject<long>() ?? 0;
        TotalCreditsSpent = data["totalCreditsSpent"]?.ToObject<long>() ?? 0;
        TradesCompleted = data["tradesCompleted"]?.ToObject<int>() ?? 0;
        DistanceTraveled = data["distanceTraveled"]?.ToObject<int>() ?? 0;
        MissionsCompleted = data["missionsCompleted"]?.ToObject<int>() ?? 0;
        MissionsFailed = data["missionsFailed"]?.ToObject<int>() ?? 0;
        
        if (data["totalPlayTime"] != null)
        {
            TotalPlayTime = TimeSpan.Parse(data["totalPlayTime"]!.ToString());
        }
        
        if (data["gameStartTime"] != null)
        {
            GameStartTime = DateTime.Parse(data["gameStartTime"]!.ToString());
        }
    }
}