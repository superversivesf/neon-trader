using System;
using System.Collections.Generic;
using NeonTrader.Core;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Complete save game structure aggregating all saveable data
/// </summary>
public sealed class SaveData : ISaveable
{
    /// <summary>
    /// Save metadata
    /// </summary>
    public SaveMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Player state
    /// </summary>
    public Player Player { get; set; } = new();

    /// <summary>
    /// Core game state
    /// </summary>
    public Core.GameState GameState { get; set; } = new();

    /// <summary>
    /// All known planets/locations (discovered + current state)
    /// </summary>
    public Dictionary<string, Planet> Planets { get; } = new();

    /// <summary>
    /// All known factions (with current relations)
    /// </summary>
    public Dictionary<string, Faction> Factions { get; } = new();

    /// <summary>
    /// Market states for all locations
    /// </summary>
    public Dictionary<string, Market> Markets { get; } = new();

    /// <summary>
    /// Active market events
    /// </summary>
    public List<MarketEvent> ActiveMarketEvents { get; } = new();

    /// <summary>
    /// Available missions across all locations
    /// </summary>
    public List<Core.MissionInfo> AvailableMissions { get; } = new();

    /// <summary>
    /// Global economy state
    /// </summary>
    public EconomyState EconomyState { get; set; } = new();

    /// <summary>
    /// Ship registry states (owned ships, shipyard availability)
    /// </summary>
    public Dictionary<string, ShipState> ShipStates { get; } = new();

    /// <summary>
    /// Equipment states (owned, installed, available)
    /// </summary>
    public EquipmentState EquipmentState { get; set; } = new();

    /// <summary>
    /// Game settings
    /// </summary>
    public Core.GameSettings Settings { get; set; } = new();

    /// <summary>
    /// Global game statistics
    /// </summary>
    public Core.GameStatistics GlobalStatistics { get; set; } = new();

    /// <summary>
    /// Save version for migration
    /// </summary>
    public int SaveVersion => 1;

    // ISaveable implementation
    public string SaveId => "save_data";

    /// <summary>
    /// Serialize complete save data to JSON
    /// </summary>
    public JObject Serialize()
    {
        var planetsDict = new Dictionary<string, JObject>();
        foreach (var kvp in Planets)
        {
            planetsDict[kvp.Key] = kvp.Value.Serialize();
        }

        var factionsDict = new Dictionary<string, JObject>();
        foreach (var kvp in Factions)
        {
            factionsDict[kvp.Key] = kvp.Value.Serialize();
        }

        var marketsDict = new Dictionary<string, JObject>();
        foreach (var kvp in Markets)
        {
            marketsDict[kvp.Key] = kvp.Value.Serialize();
        }

        var shipStatesDict = new Dictionary<string, JObject>();
        foreach (var kvp in ShipStates)
        {
            shipStatesDict[kvp.Key] = kvp.Value.Serialize();
        }

        return new JObject
        {
            ["metadata"] = Metadata.Serialize(),
            ["player"] = Player.Serialize(),
            ["gameState"] = GameState.Serialize(),
            ["planets"] = JObject.FromObject(planetsDict),
            ["factions"] = JObject.FromObject(factionsDict),
            ["markets"] = JObject.FromObject(marketsDict),
            ["activeMarketEvents"] = JArray.FromObject(ActiveMarketEvents),
            ["availableMissions"] = JArray.FromObject(AvailableMissions),
            ["economyState"] = EconomyState.Serialize(),
            ["shipStates"] = JObject.FromObject(shipStatesDict),
            ["equipmentState"] = EquipmentState.Serialize(),
            ["settings"] = Settings.Serialize(),
            ["globalStatistics"] = GlobalStatistics.Serialize(),
            ["saveVersion"] = SaveVersion
        };
    }

    /// <summary>
    /// Deserialize complete save data from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        if (data["metadata"] is JObject metaObj)
        {
            Metadata = new SaveMetadata();
            Metadata.Deserialize(metaObj);
        }

        if (data["player"] is JObject playerObj)
        {
            Player = new Player();
            Player.Deserialize(playerObj);
        }

        if (data["gameState"] is JObject gsObj)
        {
            GameState = new Core.GameState();
            GameState.Deserialize(gsObj);
        }

        Planets.Clear();
        if (data["planets"] is JObject planetsObj)
        {
            foreach (var kvp in planetsObj)
            {
                if (kvp.Value is JObject planetObj)
                {
                    var planet = new Planet();
                    planet.Deserialize(planetObj);
                    Planets[kvp.Key] = planet;
                }
            }
        }

        Factions.Clear();
        if (data["factions"] is JObject factionsObj)
        {
            foreach (var kvp in factionsObj)
            {
                if (kvp.Value is JObject factionObj)
                {
                    var faction = new Faction();
                    faction.Deserialize(factionObj);
                    Factions[kvp.Key] = faction;
                }
            }
        }

        Markets.Clear();
        if (data["markets"] is JObject marketsObj)
        {
            foreach (var kvp in marketsObj)
            {
                if (kvp.Value is JObject marketObj)
                {
                    var market = new Market();
                    market.Deserialize(marketObj);
                    Markets[kvp.Key] = market;
                }
            }
        }

        ActiveMarketEvents.Clear();
        if (data["activeMarketEvents"] is JArray eventsArray)
        {
            ActiveMarketEvents.AddRange(eventsArray.ToObject<List<MarketEvent>>() ?? new());
        }

        AvailableMissions.Clear();
        if (data["availableMissions"] is JArray missionsArray)
        {
            AvailableMissions.AddRange(missionsArray.ToObject<List<MissionInfo>>() ?? new());
        }

        if (data["economyState"] is JObject econObj)
        {
            EconomyState = new EconomyState();
            EconomyState.Deserialize(econObj);
        }

        ShipStates.Clear();
        if (data["shipStates"] is JObject shipsObj)
        {
            foreach (var kvp in shipsObj)
            {
                if (kvp.Value is JObject shipObj)
                {
                    var shipState = new ShipState();
                    shipState.Deserialize(shipObj);
                    ShipStates[kvp.Key] = shipState;
                }
            }
        }

        if (data["equipmentState"] is JObject equipObj)
        {
            EquipmentState = new EquipmentState();
            EquipmentState.Deserialize(equipObj);
        }

        if (data["settings"] is JObject settingsObj)
        {
            Settings = new GameSettings();
            Settings.Deserialize(settingsObj);
        }

        if (data["globalStatistics"] is JObject statsObj)
        {
            GlobalStatistics = new GameStatistics();
            GlobalStatistics.Deserialize(statsObj);
        }
    }

    /// <summary>
    /// Creates a new save data for a new game
    /// </summary>
    public static SaveData CreateNewGame(string playerName, string background = "merchant")
    {
        var save = new SaveData
        {
            Metadata = new SaveMetadata
            {
                SaveName = playerName,
                CreatedAt = DateTime.UtcNow,
                LastPlayedAt = DateTime.UtcNow,
                PlayerName = playerName,
                GameVersion = "1.0.0",
                IsIronman = false
            },
            Player = Player.CreateNew(playerName, background),
            GameState = new Core.GameState(),
            EconomyState = new EconomyState(),
            EquipmentState = new EquipmentState(),
            Settings = new GameSettings(),
            GlobalStatistics = new GameStatistics()
        };

        // Initialize core game state with player data
        save.GameState.PlayerName = save.Player.Name;
        save.GameState.Credits = save.Player.Credits;
        save.GameState.Health = save.Player.Health;
        save.GameState.MaxHealth = save.Player.MaxHealth;
        save.GameState.CurrentLocation = save.Player.CurrentLocationId;
        save.GameState.ShipId = save.Player.ShipId;
        save.GameState.CargoCapacity = save.Player.CargoCapacity;
        save.GameState.FuelCapacity = save.Player.MaxFuel;
        save.GameState.CurrentFuel = save.Player.CurrentFuel;

        // Initialize planets from registry
        foreach (var planet in PlanetRegistry.All)
        {
            save.Planets[planet.Id] = planet.Clone();
        }

        // Initialize markets for each planet
        foreach (var planet in save.Planets.Values)
        {
            var market = planet.Market;
            market.MarketId = planet.Id;
            market.Name = planet.Name + " Market";
            market.FactionId = planet.FactionId;
            market.TechLevel = planet.TechLevel;
            market.EconomyType = planet.EconomyType;
            market.HasBlackMarket = planet.HasBlackMarket;
            save.Markets[planet.Id] = market;
        }

        // Initialize factions from registry
        foreach (var faction in FactionRegistry.All)
        {
            save.Factions[faction.Id] = faction;
        }

        // Initialize ship states
        save.InitializeShipStates();

        return save;
    }

    /// <summary>
    /// Initializes ship states from registry
    /// </summary>
    private void InitializeShipStates()
    {
        foreach (var shipClass in ShipClassRegistry.All)
        {
            var state = new ShipState
            {
                ShipId = shipClass.Id,
                IsOwned = (shipClass.Id == Player.ShipId),
                IsAvailableForPurchase = shipClass.IsPlayerPurchasable,
                HullCondition = 1.0f,
                ShieldCondition = 1.0f,
                EngineCondition = 1.0f
            };
            ShipStates[shipClass.Id] = state;
        }
    }

    /// <summary>
    /// Updates save metadata before saving
    /// </summary>
    public void UpdateMetadata()
    {
        Metadata.LastPlayedAt = DateTime.UtcNow;
        Metadata.TotalPlayTime = Player.TotalPlayTime;
        Metadata.PlayerLevel = Player.Level;
        Metadata.PlayerCredits = Player.Credits;
        Metadata.CurrentLocation = Player.CurrentLocationId;
        Metadata.ShipName = Player.ShipName;
    }

    /// <summary>
    /// Validates save data integrity
    /// </summary>
    public SaveValidationResult Validate()
    {
        var result = new SaveValidationResult { IsValid = true };

        // Check player
        if (Player == null)
        {
            result.Errors.Add("Player data is missing");
            result.IsValid = false;
        }
        else
        {
            if (string.IsNullOrEmpty(Player.Name))
                result.Warnings.Add("Player name is empty");
            
            if (Player.Credits < 0)
                result.Warnings.Add("Player has negative credits");
            
            if (Player.Health <= 0 && !Player.IronmanMode)
                result.Warnings.Add("Player health is zero or below");
        }

        // Check game state
        if (GameState == null)
        {
            result.Errors.Add("Game state is missing");
            result.IsValid = false;
        }

        // Check planets
        if (Planets.Count == 0)
        {
            result.Warnings.Add("No planets in save data");
        }

        // Check for data consistency
        if (Player.CurrentLocationId != GameState.CurrentLocation)
        {
            result.Warnings.Add("Player location differs from game state location");
        }

        if (Player.Credits != GameState.Credits)
        {
            result.Warnings.Add($"Credits mismatch: Player={Player.Credits}, GameState={GameState.Credits}");
        }

        // CheckCargo capacity mismatch: Player={Player.CargoCapacity}, GameState={GameState.CargoCapacity}");

        return result;
    }
}

/// <summary>
/// Save file metadata
/// </summary>
public sealed class SaveMetadata : ISaveable
{
    /// <summary>
    /// Display name for the save
    /// </summary>
    public string SaveName { get; set; } = "New Game";

    /// <summary>
    /// When the save was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the save was last played
    /// </summary>
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Player character name
    /// </summary>
    public string PlayerName { get; set; } = "Pilot";

    /// <summary>
    /// Game version when save was created
    /// </summary>
    public string GameVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Game version when save was last loaded
    /// </summary>
    public string LastLoadedVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Total play time at last save
    /// </summary>
    public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Player level at last save
    /// </summary>
    public int PlayerLevel { get; set; } = 1;

    /// <summary>
    /// Player credits at last save
    /// </summary>
    public long PlayerCredits { get; set; } = 0;

    /// <summary>
    /// Current location at last save
    /// </summary>
    public string CurrentLocation { get; set; } = string.Empty;

    /// <summary>
    /// Ship name at last save
    /// </summary>
    public string ShipName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an ironman save
    /// </summary>
    public bool IsIronman { get; set; } = false;

    /// <summary>
    /// Difficulty level
    /// </summary>
    public GameDifficulty Difficulty { get; set; } = GameDifficulty.Normal;

    /// <summary>
    /// Save slot index
    /// </summary>
    public int SlotIndex { get; set; } = 0;

    /// <summary>
    /// Thumbnail screenshot data (base64)
    /// </summary>
    public string ThumbnailBase64 { get; set; } = string.Empty;

    public string SaveId => "save_metadata";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["saveName"] = SaveName,
            ["createdAt"] = CreatedAt.ToString("o"),
            ["lastPlayedAt"] = LastPlayedAt.ToString("o"),
            ["playerName"] = PlayerName,
            ["gameVersion"] = GameVersion,
            ["lastLoadedVersion"] = LastLoadedVersion,
            ["totalPlayTime"] = TotalPlayTime.ToString(),
            ["playerLevel"] = PlayerLevel,
            ["playerCredits"] = PlayerCredits,
            ["currentLocation"] = CurrentLocation,
            ["shipName"] = ShipName,
            ["isIronman"] = IsIronman,
            ["difficulty"] = Difficulty.ToString(),
            ["slotIndex"] = SlotIndex,
            ["thumbnailBase64"] = ThumbnailBase64
        };
    }

    public void Deserialize(JObject data)
    {
        SaveName = data["saveName"]?.ToString() ?? "New Game";
        
        if (data["createdAt"] != null)
            CreatedAt = DateTime.Parse(data["createdAt"]!.ToString());
        
        if (data["lastPlayedAt"] != null)
            LastPlayedAt = DateTime.Parse(data["lastPlayedAt"]!.ToString());
        
        PlayerName = data["playerName"]?.ToString() ?? "Pilot";
        GameVersion = data["gameVersion"]?.ToString() ?? "1.0.0";
        LastLoadedVersion = data["lastLoadedVersion"]?.ToString() ?? "1.0.0";

        if (data["totalPlayTime"] != null)
            TotalPlayTime = TimeSpan.Parse(data["totalPlayTime"]!.ToString());

        PlayerLevel = data["playerLevel"]?.ToObject<int>() ?? 1;
        PlayerCredits = data["playerCredits"]?.ToObject<long>() ?? 0;
        CurrentLocation = data["currentLocation"]?.ToString() ?? string.Empty;
        ShipName = data["shipName"]?.ToString() ?? string.Empty;
        IsIronman = data["isIronman"]?.ToObject<bool>() ?? false;

        if (Enum.TryParse<GameDifficulty>(data["difficulty"]?.ToString(), out var diff))
            Difficulty = diff;

        SlotIndex = data["slotIndex"]?.ToObject<int>() ?? 0;
        ThumbnailBase64 = data["thumbnailBase64"]?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Economy state for save/load
/// </summary>
public sealed class EconomyState : ISaveable
{
    /// <summary>
    /// Global price modifiers by commodity
    /// </summary>
    public Dictionary<string, decimal> GlobalPriceModifiers { get; } = new();

    /// <summary>
    /// Active global economic events
    /// </summary>
    public List<GlobalEconomicEvent> ActiveEvents { get; } = new();

    /// <summary>
    /// Trade route states
    /// </summary>
    public List<TradeRouteState> TradeRoutes { get; } = new();

    /// <summary>
    /// Commodity production/consumption rates
    /// </summary>
    public Dictionary<string, CommodityFlow> CommodityFlows { get; } = new();

    public string SaveId => "economy_state";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["globalPriceModifiers"] = JObject.FromObject(GlobalPriceModifiers),
            ["activeEvents"] = JArray.FromObject(ActiveEvents),
            ["tradeRoutes"] = JArray.FromObject(TradeRoutes),
            ["commodityFlows"] = JObject.FromObject(CommodityFlows)
        };
    }

    public void Deserialize(JObject data)
    {
        GlobalPriceModifiers.Clear();
        if (data["globalPriceModifiers"] is JObject modsObj)
        {
            foreach (var kvp in modsObj)
            {
                var value = kvp.Value?.ToObject<decimal>();
                if (value.HasValue)
                    GlobalPriceModifiers[kvp.Key] = value.Value;
            }
        }

        ActiveEvents.Clear();
        if (data["activeEvents"] is JArray eventsArray)
        {
            ActiveEvents.AddRange(eventsArray.ToObject<List<GlobalEconomicEvent>>() ?? new());
        }

        TradeRoutes.Clear();
        if (data["tradeRoutes"] is JArray routesArray)
        {
            TradeRoutes.AddRange(routesArray.ToObject<List<TradeRouteState>>() ?? new());
        }

        CommodityFlows.Clear();
        if (data["commodityFlows"] is JObject flowsObj)
        {
            foreach (var kvp in flowsObj)
            {
                if (kvp.Value is JObject flowObj)
                {
                    var flow = new CommodityFlow();
                    flow.Deserialize(flowObj);
                    CommodityFlows[kvp.Key] = flow;
                }
            }
        }
    }
}

/// <summary>
/// Global economic event
/// </summary>
public sealed class GlobalEconomicEvent : ISaveable
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EconomicEventType Type { get; set; }
    public List<string> AffectedCommodities { get; } = new();
    public List<string> AffectedSystems { get; } = new();
    public decimal PriceMultiplier { get; set; } = 1.0m;
    public double SupplyMultiplier { get; set; } = 1.0;
    public double DemandMultiplier { get; set; } = 1.0;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddDays(7);
    public int Severity { get; set; } = 50;

    public string SaveId => $"global_event_{EventId}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["eventId"] = EventId,
            ["name"] = Name,
            ["description"] = Description,
            ["type"] = Type.ToString(),
            ["affectedCommodities"] = JArray.FromObject(AffectedCommodities),
            ["affectedSystems"] = JArray.FromObject(AffectedSystems),
            ["priceMultiplier"] = PriceMultiplier,
            ["supplyMultiplier"] = SupplyMultiplier,
            ["demandMultiplier"] = DemandMultiplier,
            ["startTime"] = StartTime.ToString("o"),
            ["endTime"] = EndTime.ToString("o"),
            ["severity"] = Severity
        };
    }

    public void Deserialize(JObject data)
    {
        EventId = data["eventId"]?.ToString() ?? Guid.NewGuid().ToString();
        Name = data["name"]?.ToString() ?? string.Empty;
        Description = data["description"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<EconomicEventType>(data["type"]?.ToString(), out var type))
            Type = type;

        AffectedCommodities.Clear();
        if (data["affectedCommodities"] is JArray commArray)
        {
            foreach (var comm in commArray)
            {
                var commStr = comm?.ToString();
                if (!string.IsNullOrEmpty(commStr))
                    AffectedCommodities.Add(commStr);
            }
        }

        AffectedSystems.Clear();
        if (data["affectedSystems"] is JArray sysArray)
        {
            foreach (var sys in sysArray)
            {
                var sysStr = sys?.ToString();
                if (!string.IsNullOrEmpty(sysStr))
                    AffectedSystems.Add(sysStr);
            }
        }

        PriceMultiplier = data["priceMultiplier"]?.ToObject<decimal>() ?? 1.0m;
        SupplyMultiplier = data["supplyMultiplier"]?.ToObject<double>() ?? 1.0;
        DemandMultiplier = data["demandMultiplier"]?.ToObject<double>() ?? 1.0;

        if (data["startTime"] != null)
            StartTime = DateTime.Parse(data["startTime"]!.ToString());
        
        if (data["endTime"] != null)
            EndTime = DateTime.Parse(data["endTime"]!.ToString());

        Severity = data["severity"]?.ToObject<int>() ?? 50;
    }

    public bool IsActive(DateTime currentTime)
    {
        return currentTime >= StartTime && currentTime <= EndTime;
    }
}

/// <summary>
/// Types of global economic events
/// </summary>
public enum EconomicEventType
{
    Boom,
    Recession,
    TradeWar,
    ResourceShortage,
    TechnologicalBreakthrough,
    Pandemic,
    War,
    PirateActivity,
    PoliceCrackdown,
    Festival
}

/// <summary>
/// Trade route state
/// </summary>
public sealed class TradeRouteState : ISaveable
{
    public string RouteId { get; set; } = string.Empty;
    public string OriginId { get; set; } = string.Empty;
    public string DestinationId { get; set; } = string.Empty;
    public string PrimaryCommodity { get; set; } = string.Empty;
    public int Volume { get; set; } = 0;
    public int Capacity { get; set; } = 100;
    public double Profitability { get; set; } = 1.0;
    public int RiskLevel { get; set; } = 0;
    public RouteStatus Status { get; set; } = RouteStatus.Active;
    public DateTime EstablishedAt { get; set; } = DateTime.UtcNow;

    public string SaveId => $"trade_route_{RouteId}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["routeId"] = RouteId,
            ["originId"] = OriginId,
            ["destinationId"] = DestinationId,
            ["primaryCommodity"] = PrimaryCommodity,
            ["volume"] = Volume,
            ["capacity"] = Capacity,
            ["profitability"] = Profitability,
            ["riskLevel"] = RiskLevel,
            ["status"] = Status.ToString(),
            ["establishedAt"] = EstablishedAt.ToString("o")
        };
    }

    public void Deserialize(JObject data)
    {
        RouteId = data["routeId"]?.ToString() ?? string.Empty;
        OriginId = data["originId"]?.ToString() ?? string.Empty;
        DestinationId = data["destinationId"]?.ToString() ?? string.Empty;
        PrimaryCommodity = data["primaryCommodity"]?.ToString() ?? string.Empty;
        Volume = data["volume"]?.ToObject<int>() ?? 0;
        Capacity = data["capacity"]?.ToObject<int>() ?? 100;
        Profitability = data["profitability"]?.ToObject<double>() ?? 1.0;
        RiskLevel = data["riskLevel"]?.ToObject<int>() ?? 0;

        if (Enum.TryParse<RouteStatus>(data["status"]?.ToString(), out var status))
            Status = status;

        if (data["establishedAt"] != null)
            EstablishedAt = DateTime.Parse(data["establishedAt"]!.ToString());
    }
}

/// <summary>
/// Trade route status
/// </summary>
public enum RouteStatus
{
    Active,
    Disrupted,
    Blocked,
    Abandoned
}

/// <summary>
/// Commodity flow data
/// </summary>
public sealed class CommodityFlow : ISaveable
{
    public string CommodityId { get; set; } = string.Empty;
    public double GlobalProduction { get; set; } = 0;
    public double GlobalConsumption { get; set; } = 0;
    public decimal AveragePrice { get; set; } = 0;
    public decimal PriceTrend { get; set; } = 0;
    public Dictionary<string, double> ProductionBySystem { get; } = new();
    public Dictionary<string, double> ConsumptionBySystem { get; } = new();

    public string SaveId => $"commodity_flow_{CommodityId}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["commodityId"] = CommodityId,
            ["globalProduction"] = GlobalProduction,
            ["globalConsumption"] = GlobalConsumption,
            ["averagePrice"] = AveragePrice,
            ["priceTrend"] = PriceTrend,
            ["productionBySystem"] = JObject.FromObject(ProductionBySystem),
            ["consumptionBySystem"] = JObject.FromObject(ConsumptionBySystem)
        };
    }

    public void Deserialize(JObject data)
    {
        CommodityId = data["commodityId"]?.ToString() ?? string.Empty;
        GlobalProduction = data["globalProduction"]?.ToObject<double>() ?? 0;
        GlobalConsumption = data["globalConsumption"]?.ToObject<double>() ?? 0;
        AveragePrice = data["averagePrice"]?.ToObject<decimal>() ?? 0;
        PriceTrend = data["priceTrend"]?.ToObject<decimal>() ?? 0;

        ProductionBySystem.Clear();
        if (data["productionBySystem"] is JObject prodObj)
        {
            foreach (var kvp in prodObj)
            {
                var value = kvp.Value?.ToObject<double>();
                if (value.HasValue)
                    ProductionBySystem[kvp.Key] = value.Value;
            }
        }

        ConsumptionBySystem.Clear();
        if (data["consumptionBySystem"] is JObject consObj)
        {
            foreach (var kvp in consObj)
            {
                var value = kvp.Value?.ToObject<double>();
                if (value.HasValue)
                    ConsumptionBySystem[kvp.Key] = value.Value;
            }
        }
    }
}

/// <summary>
/// Ship state for save/load
/// </summary>
public sealed class ShipState : ISaveable
{
    public string ShipId { get; set; } = string.Empty;
    public bool IsOwned { get; set; } = false;
    public bool IsAvailableForPurchase { get; set; } = true;
    public float HullCondition { get; set; } = 1.0f;
    public float ShieldCondition { get; set; } = 1.0f;
    public float EngineCondition { get; set; } = 1.0f;
    public Dictionary<string, string> InstalledEquipment { get; } = new();
    public HashSet<string> InstalledUpgrades { get; } = new();
    public string CustomName { get; set; } = string.Empty;
    public long PurchasePrice { get; set; } = 0;
    public DateTime PurchaseDate { get; set; } = DateTime.MinValue;

    public string SaveId => $"ship_state_{ShipId}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["shipId"] = ShipId,
            ["isOwned"] = IsOwned,
            ["isAvailableForPurchase"] = IsAvailableForPurchase,
            ["hullCondition"] = HullCondition,
            ["shieldCondition"] = ShieldCondition,
            ["engineCondition"] = EngineCondition,
            ["installedEquipment"] = JObject.FromObject(InstalledEquipment),
            ["installedUpgrades"] = JArray.FromObject(InstalledUpgrades),
            ["customName"] = CustomName,
            ["purchasePrice"] = PurchasePrice,
            ["purchaseDate"] = PurchaseDate.ToString("o")
        };
    }

    public void Deserialize(JObject data)
    {
        ShipId = data["shipId"]?.ToString() ?? string.Empty;
        IsOwned = data["isOwned"]?.ToObject<bool>() ?? false;
        IsAvailableForPurchase = data["isAvailableForPurchase"]?.ToObject<bool>() ?? true;
        HullCondition = data["hullCondition"]?.ToObject<float>() ?? 1.0f;
        ShieldCondition = data["shieldCondition"]?.ToObject<float>() ?? 1.0f;
        EngineCondition = data["engineCondition"]?.ToObject<float>() ?? 1.0f;

        InstalledEquipment.Clear();
        if (data["installedEquipment"] is JObject equipObj)
        {
            foreach (var kvp in equipObj)
            {
                var value = kvp.Value?.ToString();
                if (!string.IsNullOrEmpty(value))
                    InstalledEquipment[kvp.Key] = value;
            }
        }

        InstalledUpgrades.Clear();
        if (data["installedUpgrades"] is JArray upgradesArray)
        {
            foreach (var upgrade in upgradesArray)
            {
                var upgradeStr = upgrade?.ToString();
                if (!string.IsNullOrEmpty(upgradeStr))
                    InstalledUpgrades.Add(upgradeStr);
            }
        }

        CustomName = data["customName"]?.ToString() ?? string.Empty;
        PurchasePrice = data["purchasePrice"]?.ToObject<long>() ?? 0;

        if (data["purchaseDate"] != null && data["purchaseDate"].Type != JTokenType.Null)
        {
            PurchaseDate = DateTime.Parse(data["purchaseDate"]!.ToString());
        }
    }
}

/// <summary>
/// Equipment state for save/load
/// </summary>
public sealed class EquipmentState : ISaveable
{
    /// <summary>
    /// Owned equipment (equipmentId -> quantity)
    /// </summary>
    public Dictionary<string, int> OwnedEquipment { get; } = new();

    /// <summary>
    /// Equipment installed on ships (shipId -> equipmentId -> slot)
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> InstalledByShip { get; } = new();

    /// <summary>
    /// Equipment available at shops (locationId -> equipmentId -> stock)
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> ShopStock { get; } = new();

    public string SaveId => "equipment_state";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        var installedDict = new Dictionary<string, JObject>();
        foreach (var kvp in InstalledByShip)
        {
            installedDict[kvp.Key] = JObject.FromObject(kvp.Value);
        }

        var shopDict = new Dictionary<string, JObject>();
        foreach (var kvp in ShopStock)
        {
            shopDict[kvp.Key] = JObject.FromObject(kvp.Value);
        }

        return new JObject
        {
            ["ownedEquipment"] = JObject.FromObject(OwnedEquipment),
            ["installedByShip"] = JObject.FromObject(installedDict),
            ["shopStock"] = JObject.FromObject(shopDict)
        };
    }

    public void Deserialize(JObject data)
    {
        OwnedEquipment.Clear();
        if (data["ownedEquipment"] is JObject ownedObj)
        {
            foreach (var kvp in ownedObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    OwnedEquipment[kvp.Key] = value.Value;
            }
        }

        InstalledByShip.Clear();
        if (data["installedByShip"] is JObject installedObj)
        {
            foreach (var kvp in installedObj)
            {
                if (kvp.Value is JObject shipEquipObj)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var equipKvp in shipEquipObj)
                    {
                        var value = equipKvp.Value?.ToString();
                        if (!string.IsNullOrEmpty(value))
                            dict[equipKvp.Key] = value;
                    }
                    InstalledByShip[kvp.Key] = dict;
                }
            }
        }

        ShopStock.Clear();
        if (data["shopStock"] is JObject shopObj)
        {
            foreach (var kvp in shopObj)
            {
                if (kvp.Value is JObject stockObj)
                {
                    var dict = new Dictionary<string, int>();
                    foreach (var stockKvp in stockObj)
                    {
                        var value = stockKvp.Value?.ToObject<int>();
                        if (value.HasValue)
                            dict[stockKvp.Key] = value.Value;
                    }
                    ShopStock[kvp.Key] = dict;
                }
            }
        }
    }
}

/// <summary>
/// Save validation result
/// </summary>
public sealed class SaveValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public override string ToString()
    {
        var lines = new List<string>();
        if (IsValid)
            lines.Add("Save validation: PASSED");
        else
            lines.Add("Save validation: FAILED");

        foreach (var error in Errors)
            lines.Add($"  ERROR: {error}");

        foreach (var warning in Warnings)
            lines.Add($"  WARNING: {warning}");

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Save manager interface for save/load operations
/// </summary>
public interface ISaveManager
{
    /// <summary>
    /// Saves the game to a slot
    /// </summary>
    Task<bool> SaveGameAsync(int slotIndex, SaveData saveData, string saveName = "");

    /// <summary>
    /// Loads the game from a slot
    /// </summary>
    Task<SaveData?> LoadGameAsync(int slotIndex);

    /// <summary>
    /// Gets metadata for all save slots
    /// </summary>
    Task<List<SaveMetadata>> GetSaveSlotsAsync();

    /// <summary>
    /// Deletes a save slot
    /// </summary>
    Task<bool> DeleteSaveAsync(int slotIndex);

    /// <summary>
    /// Gets the next available save slot
    /// </summary>
    int GetNextAvailableSlot();

    /// <summary>
    /// Auto-saves the current game
    /// </summary>
    Task<bool> AutoSaveAsync(SaveData saveData);

    /// <summary>
    /// Creates a backup of a save
    /// </summary>
    Task<bool> BackupSaveAsync(int slotIndex);
}