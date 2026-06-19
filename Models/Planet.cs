using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Planet/location types
/// </summary>
public enum PlanetType
{
    /// <summary>Earth-like habitable world</summary>
    Terrestrial,
    
    /// <summary>Rocky, mineral-rich world</summary>
    Mining,
    
    /// <summary>Gas giant with orbital stations</summary>
    GasGiant,
    
    /// <summary>Ice world</summary>
    Ice,
    
    /// <summary>Desert world</summary>
    Desert,
    
    /// <summary>Ocean world</summary>
    Ocean,
    
    /// <summary>Space station</summary>
    Station,
    
    /// <summary>Asteroid base</summary>
    AsteroidBase,
    
    /// <summary>Derelict/abandoned location</summary>
    Derelict,
    
    /// <summary>Capital world</summary>
    Capital
}

/// <summary>
/// Planet/location definition with market
/// </summary>
public sealed class Planet : ISaveable
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Planet type
    /// </summary>
    public PlanetType Type { get; set; } = PlanetType.Terrestrial;

    /// <summary>
    /// System name this planet belongs to
    /// </summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Position in system (for distance calculations)
    /// </summary>
    public int OrbitIndex { get; set; } = 0;

    /// <summary>
    /// Controlling faction ID
    /// </summary>
    public string FactionId { get; set; } = string.Empty;

    /// <summary>
    /// Faction alignment with player
    /// </summary>
    public FactionAlignment PlayerAlignment { get; set; } = FactionAlignment.Neutral;

    /// <summary>
    /// Economy type
    /// </summary>
    public EconomyType EconomyType { get; set; } = EconomyType.Balanced;

    /// <summary>
    /// Tech level (0-10)
    /// </summary>
    public int TechLevel { get; set; } = 5;

    /// <summary>
    /// Population (in millions)
    /// </summary>
    public long Population { get; set; } = 1;

    /// <summary>
    /// Security level (0-10, affects pirate activity, police presence)
    /// </summary>
    public int SecurityLevel { get; set; } = 5;

    /// <summary>
    /// Market at this location
    /// </summary>
    public Market Market { get; set; } = new();

    /// <summary>
    /// Connected locations (jump destinations)
    /// </summary>
    public List<string> ConnectedLocations { get; } = new();

    /// <summary>
    /// Distance to connected locations (light years)
    /// </summary>
    public Dictionary<string, double> Distances { get; } = new();

    /// <summary>
    /// Fuel cost to travel to connected locations
    /// </summary>
    public Dictionary<string, int> FuelCosts { get; } = new();

    /// <summary>
    /// Danger level for travel (0-100)
    /// </summary>
    public int TravelDanger { get; set; } = 10;

    /// <summary>
    /// Whether this location has a shipyard
    /// </summary>
    public bool HasShipyard { get; set; } = false;

    /// <summary>
    /// Whether this location has an outfitter
    /// </summary>
    public bool HasOutfitter { get; set; } = false;

    /// <summary>
    /// Whether this location has a mission board
    /// </summary>
    public bool HasMissionBoard { get; set; } = true;

    /// <summary>
    /// Whether this location has a commodity exchange
    /// </summary>
    public bool HasCommodityExchange { get; set; } = true;

    /// <summary>
    /// Whether this location has a black market
    /// </summary>
    public bool HasBlackMarket { get; set; } = false;

    /// <summary>
    /// Special features/tags
    /// </summary>
    public HashSet<string> Features { get; } = new();

    /// <summary>
    /// Description for UI
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Image/icon resource name
    /// </summary>
    public string ImageResource { get; set; } = string.Empty;

    /// <summary>
    /// Whether this location is discovered/known to player
    /// </summary>
    public bool IsDiscovered { get; set; } = false;

    /// <summary>
    /// Whether this location is the player's home base
    /// </summary>
    public bool IsHomeBase { get; set; } = false;

    // ISaveable implementation
    public string SaveId => $"planet_{Id}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the planet to JSON
    /// </summary>
    public JObject Serialize()
    {
        var marketData = Market?.Serialize() ?? new JObject();

        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["type"] = Type.ToString(),
            ["systemName"] = SystemName,
            ["orbitIndex"] = OrbitIndex,
            ["factionId"] = FactionId,
            ["playerAlignment"] = PlayerAlignment.ToString(),
            ["economyType"] = EconomyType.ToString(),
            ["techLevel"] = TechLevel,
            ["population"] = Population,
            ["securityLevel"] = SecurityLevel,
            ["market"] = marketData,
            ["connectedLocations"] = JArray.FromObject(ConnectedLocations),
            ["distances"] = JObject.FromObject(Distances),
            ["fuelCosts"] = JObject.FromObject(FuelCosts),
            ["travelDanger"] = TravelDanger,
            ["hasShipyard"] = HasShipyard,
            ["hasOutfitter"] = HasOutfitter,
            ["hasMissionBoard"] = HasMissionBoard,
            ["hasCommodityExchange"] = HasCommodityExchange,
            ["hasBlackMarket"] = HasBlackMarket,
            ["features"] = JArray.FromObject(Features),
            ["description"] = Description,
            ["imageResource"] = ImageResource,
            ["isDiscovered"] = IsDiscovered,
            ["isHomeBase"] = IsHomeBase
        };
    }

    /// <summary>
    /// Deserialize the planet from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<PlanetType>(data["type"]?.ToString(), out var type))
            Type = type;

        SystemName = data["systemName"]?.ToString() ?? string.Empty;
        OrbitIndex = data["orbitIndex"]?.ToObject<int>() ?? 0;
        FactionId = data["factionId"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<FactionAlignment>(data["playerAlignment"]?.ToString(), out var align))
            PlayerAlignment = align;

        if (Enum.TryParse<EconomyType>(data["economyType"]?.ToString(), out var econ))
            EconomyType = econ;

        TechLevel = data["techLevel"]?.ToObject<int>() ?? 5;
        Population = data["population"]?.ToObject<long>() ?? 1;
        SecurityLevel = data["securityLevel"]?.ToObject<int>() ?? 5;

        if (data["market"] is JObject marketObj)
        {
            Market = new Market();
            Market.Deserialize(marketObj);
        }

        ConnectedLocations.Clear();
        if (data["connectedLocations"] is JArray connArray)
        {
            foreach (var loc in connArray)
            {
                var locStr = loc?.ToString();
                if (!string.IsNullOrEmpty(locStr))
                    ConnectedLocations.Add(locStr);
            }
        }

        Distances.Clear();
        if (data["distances"] is JObject distObj)
        {
            foreach (var kvp in distObj)
            {
                var value = kvp.Value?.ToObject<double>();
                if (value.HasValue)
                    Distances[kvp.Key] = value.Value;
            }
        }

        FuelCosts.Clear();
        if (data["fuelCosts"] is JObject fuelObj)
        {
            foreach (var kvp in fuelObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    FuelCosts[kvp.Key] = value.Value;
            }
        }

        TravelDanger = data["travelDanger"]?.ToObject<int>() ?? 10;
        HasShipyard = data["hasShipyard"]?.ToObject<bool>() ?? false;
        HasOutfitter = data["hasOutfitter"]?.ToObject<bool>() ?? false;
        HasMissionBoard = data["hasMissionBoard"]?.ToObject<bool>() ?? true;
        HasCommodityExchange = data["hasCommodityExchange"]?.ToObject<bool>() ?? true;
        HasBlackMarket = data["hasBlackMarket"]?.ToObject<bool>() ?? false;

        Features.Clear();
        if (data["features"] is JArray featArray)
        {
            foreach (var feat in featArray)
            {
                var featStr = feat?.ToString();
                if (!string.IsNullOrEmpty(featStr))
                    Features.Add(featStr);
            }
        }

        Description = data["description"]?.ToString() ?? string.Empty;
        ImageResource = data["imageResource"]?.ToString() ?? string.Empty;
        IsDiscovered = data["isDiscovered"]?.ToObject<bool>() ?? false;
        IsHomeBase = data["isHomeBase"]?.ToObject<bool>() ?? false;
    }

    /// <summary>
    /// Gets the distance to another location
    /// </summary>
    public double GetDistanceTo(string locationId)
    {
        return Distances.TryGetValue(locationId, out var dist) ? dist : double.MaxValue;
    }

    /// <summary>
    /// Gets the fuel cost to travel to another location
    /// </summary>
    public int GetFuelCostTo(string locationId)
    {
        return FuelCosts.TryGetValue(locationId, out var cost) ? cost : int.MaxValue;
    }

    /// <summary>
    /// Checks if location is connected to another
    /// </summary>
    public bool IsConnectedTo(string locationId)
    {
        return ConnectedLocations.Contains(locationId);
    }

    /// <summary>
    /// Adds a connection to another location
    /// </summary>
    public void AddConnection(string locationId, double distance, int fuelCost)
    {
        if (!ConnectedLocations.Contains(locationId))
            ConnectedLocations.Add(locationId);
        
        Distances[locationId] = distance;
        FuelCosts[locationId] = fuelCost;
    }

    /// <summary>
    /// Removes a connection
    /// </summary>
    public void RemoveConnection(string locationId)
    {
        ConnectedLocations.Remove(locationId);
        Distances.Remove(locationId);
        FuelCosts.Remove(locationId);
    }

    /// <summary>
    /// Gets all services available at this location
    /// </summary>
    public List<string> GetAvailableServices()
    {
        var services = new List<string>();
        
        if (HasCommodityExchange) services.Add("Commodity Exchange");
        if (HasShipyard) services.Add("Shipyard");
        if (HasOutfitter) services.Add("Outfitter");
        if (HasMissionBoard) services.Add("Mission Board");
        if (HasBlackMarket) services.Add("Black Market");
        
        return services;
    }

    /// <summary>
    /// Creates a clone of this planet
    /// </summary>
    public Planet Clone()
    {
        var clone = new Planet
        {
            Id = Id,
            Name = Name,
            Type = Type,
            SystemName = SystemName,
            OrbitIndex = OrbitIndex,
            FactionId = FactionId,
            PlayerAlignment = PlayerAlignment,
            EconomyType = EconomyType,
            TechLevel = TechLevel,
            Population = Population,
            SecurityLevel = SecurityLevel,
            TravelDanger = TravelDanger,
            HasShipyard = HasShipyard,
            HasOutfitter = HasOutfitter,
            HasMissionBoard = HasMissionBoard,
            HasCommodityExchange = HasCommodityExchange,
            HasBlackMarket = HasBlackMarket,
            Description = Description,
            ImageResource = ImageResource,
            IsDiscovered = IsDiscovered,
            IsHomeBase = IsHomeBase
        };

        foreach (var loc in ConnectedLocations)
            clone.ConnectedLocations.Add(loc);
        
        foreach (var kvp in Distances)
            clone.Distances[kvp.Key] = kvp.Value;
        
        foreach (var kvp in FuelCosts)
            clone.FuelCosts[kvp.Key] = kvp.Value;
        
        foreach (var feat in Features)
            clone.Features.Add(feat);

        if (Market != null)
            clone.Market = CloneMarket(Market);

        return clone;
    }

    private Market CloneMarket(Market source)
    {
        var clone = new Market
        {
            MarketId = source.MarketId,
            Name = source.Name,
            EconomyType = source.EconomyType,
            LastRefresh = source.LastRefresh,
            Seed = source.Seed,
            FactionId = source.FactionId,
            TechLevel = source.TechLevel,
            HasBlackMarket = source.HasBlackMarket,
            PlayerReputation = source.PlayerReputation,
            PriceChangeThreshold = source.PriceChangeThreshold
        };

        foreach (var kvp in source.Prices)
            clone.Prices[kvp.Key] = kvp.Value;
        
        foreach (var kvp in source.Supply)
            clone.Supply[kvp.Key] = kvp.Value;
        
        foreach (var kvp in source.Demand)
            clone.Demand[kvp.Key] = kvp.Value;
        
        foreach (var evt in source.ActiveEvents)
            clone.ActiveEvents.Add(evt);

        return clone;
    }
}

/// <summary>
/// Static registry of all planets/locations
/// </summary>
public static class PlanetRegistry
{
    private static readonly Dictionary<string, Planet> _planets = new();
    private static readonly Dictionary<string, List<Planet>> _bySystem = new();
    private static readonly Dictionary<EconomyType, List<Planet>> _byEconomy = new();

    /// <summary>
    /// Gets a planet by ID
    /// </summary>
    public static Planet? Get(string id)
    {
        _planets.TryGetValue(id, out var planet);
        return planet;
    }

    /// <summary>
    /// Gets all planets
    /// </summary>
    public static IReadOnlyCollection<Planet> All => _planets.Values;

    /// <summary>
    /// Gets planets in a system
    /// </summary>
    public static IReadOnlyList<Planet> GetBySystem(string systemName)
    {
        _bySystem.TryGetValue(systemName, out var list);
        return list ?? new List<Planet>();
    }

    /// <summary>
    /// Gets planets by economy type
    /// </summary>
    public static IReadOnlyList<Planet> GetByEconomy(EconomyType economyType)
    {
        _byEconomy.TryGetValue(economyType, out var list);
        return list ?? new List<Planet>();
    }

    /// <summary>
    /// Gets planets by faction
    /// </summary>
    public static IEnumerable<Planet> GetByFaction(string factionId)
    {
        return _planets.Values.Where(p => p.FactionId == factionId);
    }

    /// <summary>
    /// Registers a planet
    /// </summary>
    public static void Register(Planet planet)
    {
        _planets[planet.Id] = planet;

        if (!_bySystem.ContainsKey(planet.SystemName))
            _bySystem[planet.SystemName] = new List<Planet>();
        _bySystem[planet.SystemName].Add(planet);

        if (!_byEconomy.ContainsKey(planet.EconomyType))
            _byEconomy[planet.EconomyType] = new List<Planet>();
        _byEconomy[planet.EconomyType].Add(planet);
    }

    /// <summary>
    /// Clears the registry
    /// </summary>
    public static void Clear()
    {
        _planets.Clear();
        _bySystem.Clear();
        _byEconomy.Clear();
    }

    /// <summary>
    /// Loads planets from JSON data
    /// </summary>
    public static void LoadFromJson(string json)
    {
        Clear();
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var planet = new Planet();
            planet.Deserialize((JObject)item);
            Register(planet);
        }
    }

    /// <summary>
    /// Gets all discovered planets
    /// </summary>
    public static IEnumerable<Planet> GetDiscovered()
    {
        return _planets.Values.Where(p => p.IsDiscovered);
    }

    /// <summary>
    /// Gets the home base planet
    /// </summary>
    public static Planet? GetHomeBase()
    {
        return _planets.Values.FirstOrDefault(p => p.IsHomeBase);
    }
}