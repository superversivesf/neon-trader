using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Represents the market state at a specific location
/// </summary>
public sealed class Market : ISaveable
{
    /// <summary>
    /// Unique identifier for this market (usually location ID)
    /// </summary>
    public string MarketId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the market
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Economy type of this market's location
    /// </summary>
    public EconomyType EconomyType { get; set; } = EconomyType.Balanced;

    /// <summary>
    /// Current prices for commodities (commodityId -> price per unit)
    /// </summary>
    public ConcurrentDictionary<string, decimal> Prices { get; } = new();

    /// <summary>
    /// Current supply levels (commodityId -> available units)
    /// </summary>
    public ConcurrentDictionary<string, int> Supply { get; } = new();

    /// <summary>
    /// Current demand levels (commodityId -> demanded units)
    /// </summary>
    public ConcurrentDictionary<string, int> Demand { get; } = new();

    /// <summary>
    /// Last time this market was refreshed
    /// </summary>
    public DateTime LastRefresh { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Random seed for deterministic price generation
    /// </summary>
    public int Seed { get; set; } = 0;

    /// <summary>
    /// Active market events affecting prices
    /// </summary>
    public List<MarketEvent> ActiveEvents { get; } = new();

    /// <summary>
    /// Faction controlling this market (affects prices/legality)
    /// </summary>
    public string FactionId { get; set; } = string.Empty;

    /// <summary>
    /// Tech level of this market (0-10)
    /// </summary>
    public int TechLevel { get; set; } = 5;

    /// <summary>
    /// Whether this market has a black market
    /// </summary>
    public bool HasBlackMarket { get; set; } = false;

    /// <summary>
    /// Market reputation with player (-100 to 100)
    /// </summary>
    public int PlayerReputation { get; set; } = 0;

    /// <summary>
    /// Minimum price change threshold to trigger refresh
    /// </summary>
    public decimal PriceChangeThreshold { get; set; } = 0.05m; // 5%

    // ISaveable implementation
    public string SaveId => $"market_{MarketId}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the market to JSON
    /// </summary>
    public JObject Serialize()
    {
        var pricesDict = new Dictionary<string, decimal>();
        foreach (var kvp in Prices)
            pricesDict[kvp.Key] = kvp.Value;

        var supplyDict = new Dictionary<string, int>();
        foreach (var kvp in Supply)
            supplyDict[kvp.Key] = kvp.Value;

        var demandDict = new Dictionary<string, int>();
        foreach (var kvp in Demand)
            demandDict[kvp.Key] = kvp.Value;

        return new JObject
        {
            ["marketId"] = MarketId,
            ["name"] = Name,
            ["economyType"] = EconomyType.ToString(),
            ["prices"] = JObject.FromObject(pricesDict),
            ["supply"] = JObject.FromObject(supplyDict),
            ["demand"] = JObject.FromObject(demandDict),
            ["lastRefresh"] = LastRefresh.ToString("o"),
            ["seed"] = Seed,
            ["activeEvents"] = JArray.FromObject(ActiveEvents),
            ["factionId"] = FactionId,
            ["techLevel"] = TechLevel,
            ["hasBlackMarket"] = HasBlackMarket,
            ["playerReputation"] = PlayerReputation,
            ["priceChangeThreshold"] = PriceChangeThreshold
        };
    }

    /// <summary>
    /// Deserialize the market from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        MarketId = data["marketId"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<EconomyType>(data["economyType"]?.ToString(), out var econType))
            EconomyType = econType;

        Prices.Clear();
        if (data["prices"] is JObject pricesObj)
        {
            foreach (var kvp in pricesObj)
            {
                var value = kvp.Value?.ToObject<decimal>();
                if (value.HasValue)
                    Prices[kvp.Key] = value.Value;
            }
        }

        Supply.Clear();
        if (data["supply"] is JObject supplyObj)
        {
            foreach (var kvp in supplyObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    Supply[kvp.Key] = value.Value;
            }
        }

        Demand.Clear();
        if (data["demand"] is JObject demandObj)
        {
            foreach (var kvp in demandObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    Demand[kvp.Key] = value.Value;
            }
        }

        if (data["lastRefresh"] != null)
            LastRefresh = DateTime.Parse(data["lastRefresh"]!.ToString());

        Seed = data["seed"]?.ToObject<int>() ?? 0;

        ActiveEvents.Clear();
        if (data["activeEvents"] is JArray eventsArray)
        {
            ActiveEvents.AddRange(eventsArray.ToObject<List<MarketEvent>>() ?? new());
        }

        FactionId = data["factionId"]?.ToString() ?? string.Empty;
        TechLevel = data["techLevel"]?.ToObject<int>() ?? 5;
        HasBlackMarket = data["hasBlackMarket"]?.ToObject<bool>() ?? false;
        PlayerReputation = data["playerReputation"]?.ToObject<int>() ?? 0;
        PriceChangeThreshold = data["priceChangeThreshold"]?.ToObject<decimal>() ?? 0.05m;
    }

    /// <summary>
    /// Gets the current price for a commodity
    /// </summary>
    public decimal GetPrice(string commodityId)
    {
        return Prices.TryGetValue(commodityId, out var price) ? price : 0m;
    }

    /// <summary>
    /// Gets the current supply for a commodity
    /// </summary>
    public int GetSupply(string commodityId)
    {
        return Supply.TryGetValue(commodityId, out var supply) ? supply : 0;
    }

    /// <summary>
    /// Gets the current demand for a commodity
    /// </summary>
    public int GetDemand(string commodityId)
    {
        return Demand.TryGetValue(commodityId, out var demand) ? demand : 0;
    }

    /// <summary>
    /// Checks if a commodity is available for purchase
    /// </summary>
    public bool IsAvailable(string commodityId, int quantity = 1)
    {
        return GetSupply(commodityId) >= quantity && GetPrice(commodityId) > 0;
    }

    /// <summary>
    /// Attempts to buy commodity from market (player sells to market)
    /// Returns the actual quantity sold and total credits received
    /// </summary>
    public (int quantitySold, decimal creditsReceived) SellToMarket(string commodityId, int quantity)
    {
        var availableSupply = GetSupply(commodityId);
        var actualQuantity = Math.Min(quantity, availableSupply);
        
        if (actualQuantity <= 0)
            return (0, 0m);

        var price = GetPrice(commodityId);
        var totalCredits = price * actualQuantity;

        // Update supply
        Supply.AddOrUpdate(commodityId, 
            _ => Math.Max(0, availableSupply - actualQuantity),
            (_, existing) => Math.Max(0, existing - actualQuantity));

        // Increase demand slightly (market wants to restock)
        Demand.AddOrUpdate(commodityId,
            _ => actualQuantity,
            (_, existing) => existing + actualQuantity / 2);

        return (actualQuantity, totalCredits);
    }

    /// <summary>
    /// Attempts to sell commodity to market (player buys from market)
    /// Returns the actual quantity bought and total credits paid
    /// </summary>
    public (int quantityBought, decimal creditsPaid) BuyFromMarket(string commodityId, int quantity)
    {
        var availableSupply = GetSupply(commodityId);
        var actualQuantity = Math.Min(quantity, availableSupply);
        
        if (actualQuantity <= 0)
            return (0, 0m);

        var price = GetPrice(commodityId);
        var totalCredits = price * actualQuantity;

        // Update supply
        Supply.AddOrUpdate(commodityId,
            _ => Math.Max(0, availableSupply - actualQuantity),
            (_, existing) => Math.Max(0, existing - actualQuantity));

        // Increase demand (market wants more)
        Demand.AddOrUpdate(commodityId,
            _ => actualQuantity,
            (_, existing) => existing + actualQuantity);

        return (actualQuantity, totalCredits);
    }

    /// <summary>
    /// Checks if market needs refresh based on time and price changes
    /// </summary>
    public bool NeedsRefresh(DateTime currentTime, TimeSpan refreshInterval)
    {
        if (LastRefresh == DateTime.MinValue)
            return true;

        var timeSinceRefresh = currentTime - LastRefresh;
        if (timeSinceRefresh >= refreshInterval)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the supply/demand ratio for a commodity (higher = more supply relative to demand)
    /// </summary>
    public double GetSupplyDemandRatio(string commodityId)
    {
        var supply = GetSupply(commodityId);
        var demand = GetDemand(commodityId);
        
        if (demand == 0)
            return supply > 0 ? double.MaxValue : 1.0;
        
        return (double)supply / demand;
    }

    /// <summary>
    /// Gets price trend for a commodity (-1 to 1, negative = falling, positive = rising)
    /// </summary>
    public double GetPriceTrend(string commodityId)
    {
        var ratio = GetSupplyDemandRatio(commodityId);
        
        // If supply > demand, price tends to fall
        // If demand > supply, price tends to rise
        if (ratio > 1.5)
            return -0.5; // Oversupplied
        else if (ratio > 1.0)
            return -0.2;
        else if (ratio > 0.7)
            return 0.0;  // Balanced
        else if (ratio > 0.4)
            return 0.2;
        else
            return 0.5;  // Undersupplied
    }
}

/// <summary>
/// Represents a temporary market event affecting prices
/// </summary>
public sealed class MarketEvent : ISaveable
{
    /// <summary>
    /// Unique event ID
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event type
    /// </summary>
    public MarketEventType Type { get; set; }

    /// <summary>
    /// Commodity affected (empty = all commodities)
    /// </summary>
    public string CommodityId { get; set; } = string.Empty;

    /// <summary>
    /// Category affected (if commodityId is empty)
    /// </summary>
    public CommodityCategory? Category { get; set; }

    /// <summary>
    /// Price multiplier during event (1.0 = normal)
    /// </summary>
    public double PriceMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Supply multiplier during event (1.0 = normal)
    /// </summary>
    public double SupplyMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Demand multiplier during event (1.0 = normal)
    /// </summary>
    public double DemandMultiplier { get; set; } = 1.0;

    /// <summary>
    /// When the event started
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the event ends
    /// </summary>
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(1);

    /// <summary>
    /// Event description for UI
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Event severity (0-100)
    /// </summary>
    public int Severity { get; set; } = 50;

    // ISaveable implementation
    public string SaveId => $"market_event_{EventId}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["eventId"] = EventId,
            ["type"] = Type.ToString(),
            ["commodityId"] = CommodityId,
            ["category"] = Category?.ToString() ?? "",
            ["priceMultiplier"] = PriceMultiplier,
            ["supplyMultiplier"] = SupplyMultiplier,
            ["demandMultiplier"] = DemandMultiplier,
            ["startTime"] = StartTime.ToString("o"),
            ["endTime"] = EndTime.ToString("o"),
            ["description"] = Description,
            ["severity"] = Severity
        };
    }

    public void Deserialize(JObject data)
    {
        EventId = data["eventId"]?.ToString() ?? Guid.NewGuid().ToString();
        
        if (Enum.TryParse<MarketEventType>(data["type"]?.ToString(), out var type))
            Type = type;

        CommodityId = data["commodityId"]?.ToString() ?? string.Empty;
        
        if (Enum.TryParse<CommodityCategory>(data["category"]?.ToString(), out var cat))
            Category = cat;

        PriceMultiplier = data["priceMultiplier"]?.ToObject<double>() ?? 1.0;
        SupplyMultiplier = data["supplyMultiplier"]?.ToObject<double>() ?? 1.0;
        DemandMultiplier = data["demandMultiplier"]?.ToObject<double>() ?? 1.0;

        if (data["startTime"] != null)
            StartTime = DateTime.Parse(data["startTime"]!.ToString());
        
        if (data["endTime"] != null)
            EndTime = DateTime.Parse(data["endTime"]!.ToString());

        Description = data["description"]?.ToString() ?? string.Empty;
        Severity = data["severity"]?.ToObject<int>() ?? 50;
    }

    /// <summary>
    /// Checks if event is currently active
    /// </summary>
    public bool IsActive(DateTime currentTime)
    {
        return currentTime >= StartTime && currentTime <= EndTime;
    }

    /// <summary>
    /// Checks if event affects a specific commodity
    /// </summary>
    public bool AffectsCommodity(string commodityId, CommodityCategory category)
    {
        if (!string.IsNullOrEmpty(CommodityId))
            return CommodityId == commodityId;
        
        if (Category.HasValue)
            return Category.Value == category;
        
        return true; // Affects all
    }
}

/// <summary>
/// Types of market events
/// </summary>
public enum MarketEventType
{
    /// <summary>Shortage - supply reduced, prices up</summary>
    Shortage,
    
    /// <summary>Surplus - supply increased, prices down</summary>
    Surplus,
    
    /// <summary>High demand - demand increased, prices up</summary>
    HighDemand,
    
    /// <summary>Low demand - demand decreased, prices down</summary>
    LowDemand,
    
    /// <summary>Trade route disruption - affects specific commodities</summary>
    TradeRouteDisruption,
    
    /// <summary>Economic boom - all prices up, demand up</summary>
    EconomicBoom,
    
    /// <summary>Recession - all prices down, demand down</summary>
    Recession,
    
    /// <summary>Pirate activity - illegal goods up, legal goods down</summary>
    PirateActivity,
    
    /// <summary>Police crackdown - illegal goods down, weapons up</summary>
    PoliceCrackdown,
    
    /// <summary>Technological breakthrough - tech prices down</summary>
    TechBreakthrough,
    
    /// <summary>Crop failure - organics prices up</summary>
    CropFailure,
    
    /// <summary>Mineral discovery - ore prices down</summary>
    MineralDiscovery
}