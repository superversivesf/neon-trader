using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Price generation algorithms with supply/demand curves, random events, and faction modifiers
/// </summary>
public static class PriceGenerator
{
    private static readonly Random _random = new();
    private static readonly object _randomLock = new();

    /// <summary>
    /// Generates initial market prices for a location based on its economy type
    /// </summary>
    public static void GenerateInitialMarket(Market market, IEnumerable<Commodity> commodities)
    {
        var profile = EconomyRegistry.GetProfile(market.EconomyType);
        var rng = CreateSeededRandom(market.Seed);

        foreach (var commodity in commodities)
        {
            var basePrice = commodity.BasePrice;
            var category = commodity.Category;

            // Apply economy production/consumption modifiers
            var productionMod = profile.ProductionModifiers.GetValueOrDefault(category, 1.0);
            var consumptionMod = profile.ConsumptionModifiers.GetValueOrDefault(category, 1.0);

            // Base supply/demand from economy profile
            var baseSupply = profile.BaseSupply.GetValueOrDefault(category, 50);
            var baseDemand = profile.BaseDemand.GetValueOrDefault(category, 50);

            // Apply market size modifier
            baseSupply = (int)(baseSupply * profile.MarketSizeModifier);
            baseDemand = (int)(baseDemand * profile.MarketSizeModifier);

            // Apply special production/consumption for specific commodities
            if (profile.SpecialProduction.TryGetValue(commodity.Id, out var specialProd))
                baseSupply = (int)(baseSupply * specialProd);

            if (profile.SpecialConsumption.TryGetValue(commodity.Id, out var specialCons))
                baseDemand = (int)(baseDemand * specialCons);

            // Tech level affects tech/medical availability
            if (category == CommodityCategory.Tech || category == CommodityCategory.Medical)
            {
                var techFactor = 0.5 + (market.TechLevel / 20.0); // 0.5 to 1.0
                baseSupply = (int)(baseSupply * techFactor);
            }

            // Security level affects illegal goods
            if (category == CommodityCategory.Illegal)
            {
                var securityFactor = 1.0 - (market.TechLevel / 20.0) * (1.0 - profile.IllegalTolerance);
                baseSupply = (int)(baseSupply * securityFactor);
                baseDemand = (int)(baseDemand * (1.0 + (1.0 - securityFactor) * 0.5));
            }

            // Add randomness
            var supplyVariance = 0.8 + rng.NextDouble() * 0.4; // 0.8 to 1.2
            var demandVariance = 0.8 + rng.NextDouble() * 0.4;

            var finalSupply = Math.Max(0, (int)(baseSupply * supplyVariance));
            var finalDemand = Math.Max(0, (int)(baseDemand * demandVariance));

            // Calculate price based on supply/demand
            var price = CalculatePrice(basePrice, finalSupply, finalDemand, commodity.Volatility, rng);

            // Apply economy price multipliers
            var buyMult = profile.BuyPriceMultiplier.GetValueOrDefault(category, 1.0);
            var sellMult = profile.SellPriceMultiplier.GetValueOrDefault(category, 1.0);
            
            // Market buys from player at buy price, sells to player at sell price
            // We store the "sell to player" price (higher)
            price = price * (decimal)sellMult;

            // Clamp to commodity min/max
            price = Math.Clamp(price, commodity.MinPrice, commodity.MaxPrice);

            // Apply faction modifier if any
            price = ApplyFactionModifier(price, market.FactionId, commodity);

            market.Prices[commodity.Id] = price;
            market.Supply[commodity.Id] = finalSupply;
            market.Demand[commodity.Id] = finalDemand;
        }

        market.LastRefresh = DateTime.UtcNow;
    }

    /// <summary>
    /// Refreshes market prices (simulates time passing)
    /// </summary>
    public static void RefreshMarket(Market market, IEnumerable<Commodity> commodities, DateTime currentTime, double timeFactor = 1.0)
    {
        var profile = EconomyRegistry.GetProfile(market.EconomyType);
        var rng = CreateSeededRandom(market.Seed + (int)currentTime.Ticks);

        // Process active events first
        var activeEvents = market.ActiveEvents.Where(e => e.IsActive(currentTime)).ToList();

        foreach (var commodity in commodities)
        {
            var currentPrice = market.GetPrice(commodity.Id);
            var currentSupply = market.GetSupply(commodity.Id);
            var currentDemand = market.GetDemand(commodity.Id);

            // Natural supply/demand drift toward economy baseline
            var baseSupply = profile.BaseSupply.GetValueOrDefault(commodity.Category, 50);
            var baseDemand = profile.BaseDemand.GetValueOrDefault(commodity.Category, 50);
            
            baseSupply = (int)(baseSupply * profile.MarketSizeModifier);
            baseDemand = (int)(baseDemand * profile.MarketSizeModifier);

            // Drift factor based on time passed
            var hoursSinceRefresh = (currentTime - market.LastRefresh).TotalHours * timeFactor;
            var driftFactor = Math.Min(1.0, hoursSinceRefresh / 24.0); // Full drift over 24 hours

            // Supply drifts toward baseline
            var targetSupply = baseSupply;
            if (profile.SpecialProduction.TryGetValue(commodity.Id, out var specialProd))
                targetSupply = (int)(targetSupply * specialProd);

            var newSupply = (int)(currentSupply + (targetSupply - currentSupply) * driftFactor * 0.3);
            newSupply = Math.Max(0, newSupply);

            // Demand drifts toward baseline
            var targetDemand = baseDemand;
            if (profile.SpecialConsumption.TryGetValue(commodity.Id, out var specialCons))
                targetDemand = (int)(targetDemand * specialCons);

            var newDemand = (int)(currentDemand + (targetDemand - currentDemand) * driftFactor * 0.3);
            newDemand = Math.Max(0, newDemand);

            // Apply active events
            double eventPriceMult = 1.0;
            double eventSupplyMult = 1.0;
            double eventDemandMult = 1.0;

            foreach (var evt in activeEvents)
            {
                if (evt.AffectsCommodity(commodity.Id, commodity.Category))
                {
                    eventPriceMult *= evt.PriceMultiplier;
                    eventSupplyMult *= evt.SupplyMultiplier;
                    eventDemandMult *= evt.DemandMultiplier;
                }
            }

            newSupply = (int)(newSupply * eventSupplyMult);
            newDemand = (int)(newDemand * eventDemandMult);

            // Random fluctuation
            var supplyNoise = 0.95 + rng.NextDouble() * 0.1; // ±5%
            var demandNoise = 0.95 + rng.NextDouble() * 0.1;

            newSupply = Math.Max(0, (int)(newSupply * supplyNoise));
            newDemand = Math.Max(0, (int)(newDemand * demandNoise));

            // Calculate new price
            var newPrice = CalculatePrice(commodity.BasePrice, newSupply, newDemand, commodity.Volatility, rng);

            // Apply economy multipliers
            var sellMult = profile.SellPriceMultiplier.GetValueOrDefault(commodity.Category, 1.0);
            newPrice = newPrice * (decimal)sellMult;

            // Apply event price multiplier
            newPrice = newPrice * (decimal)eventPriceMult;

            // Apply faction modifier
            newPrice = ApplyFactionModifier(newPrice, market.FactionId, commodity);

            // Clamp
            newPrice = Math.Clamp(newPrice, commodity.MinPrice, commodity.MaxPrice);

            // Smooth price changes (prevent wild swings)
            var maxChange = currentPrice * (decimal)commodity.Volatility * 0.5m;
            var priceDiff = newPrice - currentPrice;
            
            if (Math.Abs(priceDiff) > maxChange)
            {
                newPrice = currentPrice + Math.Sign(priceDiff) * maxChange;
            }

            market.Prices[commodity.Id] = newPrice;
            market.Supply[commodity.Id] = newSupply;
            market.Demand[commodity.Id] = newDemand;
        }

        // Remove expired events
        market.ActiveEvents.RemoveAll(e => !e.IsActive(currentTime));

        market.LastRefresh = currentTime;
    }

    /// <summary>
    /// Calculates price based on supply/demand curve
    /// </summary>
    private static decimal CalculatePrice(decimal basePrice, int supply, int demand, double volatility, Random rng)
    {
        if (supply <= 0 && demand <= 0)
            return basePrice;

        // Supply/demand ratio
        double ratio;
        if (demand == 0)
        {
            ratio = supply > 0 ? 10.0 : 1.0; // High supply, no demand = low price
        }
        else
        {
            ratio = (double)supply / demand;
        }

        // Price curve: 
        // ratio > 2.0 (oversupply) -> price drops to ~50% of base
        // ratio = 1.0 (balanced) -> price = base
        // ratio < 0.5 (undersupply) -> price rises to ~200% of base
        
        double priceMultiplier;
        
        if (ratio >= 2.0)
        {
            // Oversupplied: price drops logarithmically
            priceMultiplier = 0.5 + 0.5 / Math.Log(ratio + 1);
        }
        else if (ratio >= 1.0)
        {
            // Slight oversupply: linear interpolation
            priceMultiplier = 1.0 - 0.3 * (ratio - 1.0);
        }
        else if (ratio >= 0.5)
        {
            // Slight undersupply: linear interpolation
            priceMultiplier = 1.0 + 0.5 * (1.0 - ratio);
        }
        else
        {
            // Severe undersupply: exponential increase
            priceMultiplier = 1.5 + 1.0 / (ratio + 0.1);
        }

        // Add volatility noise
        var noiseRange = volatility * 0.2; // Up to 20% of volatility
        var noise = 1.0 + (rng.NextDouble() - 0.5) * 2.0 * noiseRange;
        priceMultiplier *= noise;

        var price = basePrice * (decimal)priceMultiplier;
        return price;
    }

    /// <summary>
    /// Applies faction-specific price modifiers
    /// </summary>
    private static decimal ApplyFactionModifier(decimal price, string factionId, Commodity commodity)
    {
        if (string.IsNullOrEmpty(factionId))
            return price;

        // Faction modifiers would be loaded from data
        // For now, return base price
        // TODO: Implement faction-specific price modifiers from data files
        return price;
    }

    /// <summary>
    /// Calculates the price a market pays when player sells to market (buy price)
    /// </summary>
    public static decimal CalculateBuyPrice(Market market, Commodity commodity, int quantity = 1)
    {
        var sellPrice = market.GetPrice(commodity.Id);
        var profile = EconomyRegistry.GetProfile(market.EconomyType);
        
        // Market buys at a discount from sell price
        var buyMult = profile.BuyPriceMultiplier.GetValueOrDefault(commodity.Category, 1.0);
        var baseBuyPrice = sellPrice * (decimal)buyMult;

        // Quantity discount/penalty
        var supply = market.GetSupply(commodity.Id);
        var demand = market.GetDemand(commodity.Id);
        
        double quantityFactor;
        if (supply == 0)
        {
            quantityFactor = 1.2; // Desperate for goods
        }
        else
        {
            var ratio = (double)supply / Math.Max(1, demand);
            if (ratio > 2.0)
                quantityFactor = 0.8; // Oversupplied, lower buy price
            else if (ratio > 1.0)
                quantityFactor = 0.9;
            else
                quantityFactor = 1.0; // Balanced or undersupplied
        }

        // Large quantities get worse prices (market can't absorb)
        var volumeImpact = Math.Min(1.0, (double)quantity / Math.Max(1, supply + demand));
        quantityFactor *= (1.0 - volumeImpact * 0.2);

        var finalPrice = baseBuyPrice * (decimal)quantityFactor;
        return Math.Clamp(finalPrice, commodity.MinPrice, commodity.MaxPrice);
    }

    /// <summary>
    /// Calculates the price a market charges when player buys from market (sell price)
    /// </summary>
    public static decimal CalculateSellPrice(Market market, Commodity commodity, int quantity = 1)
    {
        var sellPrice = market.GetPrice(commodity.Id);
        
        // Quantity premium for large purchases
        var supply = market.GetSupply(commodity.Id);
        var volumeImpact = Math.Min(1.0, (double)quantity / Math.Max(1, supply));
        
        var quantityFactor = 1.0 + volumeImpact * 0.15; // Up to 15% premium
        
        var finalPrice = sellPrice * (decimal)quantityFactor;
        return Math.Clamp(finalPrice, commodity.MinPrice, commodity.MaxPrice);
    }

    /// <summary>
    /// Generates a random market event
    /// </summary>
    public static MarketEvent? GenerateRandomEvent(Market market, IEnumerable<Commodity> commodities, DateTime currentTime, double chance = 0.1)
    {
        lock (_randomLock)
        {
            if (_random.NextDouble() > chance)
                return null;
        }

        var eventTypes = Enum.GetValues<MarketEventType>();
        var evtType = eventTypes[_random.Next(eventTypes.Length)];

        // Pick a random commodity or category
        var commodityList = commodities.ToList();
        var commodity = commodityList[_random.Next(commodityList.Count)];

        var evt = new MarketEvent
        {
            Type = evtType,
            StartTime = currentTime,
            EndTime = currentTime.AddHours(6 + _random.Next(48)), // 6-54 hours
            Severity = 20 + _random.Next(80)
        };

        ConfigureEvent(evt, commodity, market.EconomyType);
        return evt;
    }

    /// <summary>
    /// Configures event parameters based on type
    /// </summary>
    private static void ConfigureEvent(MarketEvent evt, Commodity commodity, EconomyType economyType)
    {
        var severityFactor = evt.Severity / 100.0;

        switch (evt.Type)
        {
            case MarketEventType.Shortage:
                evt.CommodityId = commodity.Id;
                evt.PriceMultiplier = 1.5 + severityFactor;
                evt.SupplyMultiplier = 0.3 + severityFactor * 0.4;
                evt.DemandMultiplier = 1.2 + severityFactor * 0.5;
                evt.Description = $"Shortage of {commodity.Name} due to supply chain disruption";
                break;

            case MarketEventType.Surplus:
                evt.CommodityId = commodity.Id;
                evt.PriceMultiplier = 0.5 + severityFactor * 0.3;
                evt.SupplyMultiplier = 2.0 + severityFactor;
                evt.DemandMultiplier = 0.7;
                evt.Description = $"Surplus of {commodity.Name} floods the market";
                break;

            case MarketEventType.HighDemand:
                evt.CommodityId = commodity.Id;
                evt.PriceMultiplier = 1.3 + severityFactor * 0.5;
                evt.SupplyMultiplier = 0.8;
                evt.DemandMultiplier = 1.5 + severityFactor;
                evt.Description = $"High demand for {commodity.Name} drives up prices";
                break;

            case MarketEventType.LowDemand:
                evt.CommodityId = commodity.Id;
                evt.PriceMultiplier = 0.6 + severityFactor * 0.2;
                evt.SupplyMultiplier = 1.2;
                evt.DemandMultiplier = 0.4 + severityFactor * 0.3;
                evt.Description = $"Low demand for {commodity.Name} causes price crash";
                break;

            case MarketEventType.TradeRouteDisruption:
                evt.Category = commodity.Category;
                evt.PriceMultiplier = 1.4 + severityFactor * 0.4;
                evt.SupplyMultiplier = 0.5 + severityFactor * 0.3;
                evt.DemandMultiplier = 1.1;
                evt.Description = $"Trade route disruption affects {commodity.Category} commodities";
                break;

            case MarketEventType.EconomicBoom:
                evt.PriceMultiplier = 1.2 + severityFactor * 0.3;
                evt.SupplyMultiplier = 1.1;
                evt.DemandMultiplier = 1.3 + severityFactor * 0.4;
                evt.Description = "Economic boom increases all prices and demand";
                break;

            case MarketEventType.Recession:
                evt.PriceMultiplier = 0.7 + severityFactor * 0.2;
                evt.SupplyMultiplier = 1.2;
                evt.DemandMultiplier = 0.6 + severityFactor * 0.3;
                evt.Description = "Recession reduces demand and prices across the board";
                break;

            case MarketEventType.PirateActivity:
                evt.Category = CommodityCategory.Illegal;
                evt.PriceMultiplier = 1.5 + severityFactor;
                evt.SupplyMultiplier = 1.3 + severityFactor;
                evt.DemandMultiplier = 1.2;
                // Legal goods suffer
                evt.Description = "Pirate activity disrupts legal trade, boosts black market";
                break;

            case MarketEventType.PoliceCrackdown:
                evt.Category = CommodityCategory.Illegal;
                evt.PriceMultiplier = 0.4 + severityFactor * 0.3;
                evt.SupplyMultiplier = 0.3;
                evt.DemandMultiplier = 0.5;
                // Weapons demand up
                evt.Description = "Police crackdown suppresses illegal trade";
                break;

            case MarketEventType.TechBreakthrough:
                evt.Category = CommodityCategory.Tech;
                evt.PriceMultiplier = 0.6 + severityFactor * 0.2;
                evt.SupplyMultiplier = 1.5 + severityFactor;
                evt.DemandMultiplier = 0.8;
                evt.Description = "Technological breakthrough floods market with cheap tech";
                break;

            case MarketEventType.CropFailure:
                evt.Category = CommodityCategory.Organics;
                evt.PriceMultiplier = 2.0 + severityFactor;
                evt.SupplyMultiplier = 0.2 + severityFactor * 0.3;
                evt.DemandMultiplier = 1.5 + severityFactor * 0.5;
                evt.Description = "Widespread crop failure causes food shortage";
                break;

            case MarketEventType.MineralDiscovery:
                evt.Category = CommodityCategory.Ore;
                evt.PriceMultiplier = 0.5 + severityFactor * 0.3;
                evt.SupplyMultiplier = 2.0 + severityFactor;
                evt.DemandMultiplier = 0.9;
                evt.Description = "Major mineral discovery crashes ore prices";
                break;
        }

        // Clamp multipliers
        evt.PriceMultiplier = Math.Clamp(evt.PriceMultiplier, 0.1, 5.0);
        evt.SupplyMultiplier = Math.Clamp(evt.SupplyMultiplier, 0.05, 5.0);
        evt.DemandMultiplier = Math.Clamp(evt.DemandMultiplier, 0.05, 5.0);
    }

    /// <summary>
    /// Creates a seeded random for deterministic generation
    /// </summary>
    private static Random CreateSeededRandom(int seed)
    {
        return new Random(seed.GetHashCode());
    }

    /// <summary>
    /// Calculates profit margin for a trade route
    /// </summary>
    public static (decimal buyPrice, decimal sellPrice, decimal profitPerUnit, double profitMargin) 
        CalculateTradeProfit(Market buyMarket, Market sellMarket, Commodity commodity, int quantity)
    {
        var buyPrice = CalculateBuyPrice(buyMarket, commodity, quantity);
        var sellPrice = CalculateSellPrice(sellMarket, commodity, quantity);
        
        var profitPerUnit = sellPrice - buyPrice;
        var profitMargin = buyPrice > 0 ? (double)(profitPerUnit / buyPrice) : 0;

        return (buyPrice, sellPrice, profitPerUnit, profitMargin);
    }

    /// <summary>
    /// Finds best trade routes from a market
    /// </summary>
    public static List<TradeRoute> FindBestTradeRoutes(
        Market sourceMarket, 
        IEnumerable<Market> destinationMarkets, 
        IEnumerable<Commodity> commodities,
        int maxResults = 10)
    {
        var routes = new List<TradeRoute>();

        foreach (var destMarket in destinationMarkets)
        {
            if (destMarket.MarketId == sourceMarket.MarketId)
                continue;

            foreach (var commodity in commodities)
            {
                var sourceSupply = sourceMarket.GetSupply(commodity.Id);
                if (sourceSupply <= 0)
                    continue;

                var destDemand = destMarket.GetDemand(commodity.Id);
                if (destDemand <= 0)
                    continue;

                var maxQuantity = Math.Min(sourceSupply, destDemand);
                if (maxQuantity <= 0)
                    continue;

                var (buyPrice, sellPrice, profitPerUnit, profitMargin) = 
                    CalculateTradeProfit(sourceMarket, destMarket, commodity, maxQuantity);

                if (profitPerUnit <= 0)
                    continue;

                var route = new TradeRoute
                {
                    SourceMarketId = sourceMarket.MarketId,
                    SourceMarketName = sourceMarket.Name,
                    DestinationMarketId = destMarket.MarketId,
                    DestinationMarketName = destMarket.Name,
                    CommodityId = commodity.Id,
                    CommodityName = commodity.Name,
                    MaxQuantity = maxQuantity,
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    ProfitPerUnit = profitPerUnit,
                    ProfitMargin = profitMargin,
                    TotalProfit = profitPerUnit * maxQuantity
                };

                routes.Add(route);
            }
        }

        return routes
            .OrderByDescending(r => r.TotalProfit)
            .ThenByDescending(r => r.ProfitMargin)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Applies a trade to both markets (updates supply/demand)
    /// </summary>
    public static void ExecuteTrade(Market buyMarket, Market sellMarket, Commodity commodity, int quantity)
    {
        // Player buys from buyMarket (source)
        buyMarket.BuyFromMarket(commodity.Id, quantity);
        
        // Player sells to sellMarket (destination)
        sellMarket.SellToMarket(commodity.Id, quantity);
    }
}

/// <summary>
/// Represents a profitable trade route
/// </summary>
public sealed class TradeRoute
{
    public string SourceMarketId { get; set; } = string.Empty;
    public string SourceMarketName { get; set; } = string.Empty;
    public string DestinationMarketId { get; set; } = string.Empty;
    public string DestinationMarketName { get; set; } = string.Empty;
    public string CommodityId { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public int MaxQuantity { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal ProfitPerUnit { get; set; }
    public double ProfitMargin { get; set; }
    public decimal TotalProfit { get; set; }

    public override string ToString()
    {
        return $"{SourceMarketName} -> {DestinationMarketName}: {CommodityName} x{MaxQuantity} " +
               $"(@{BuyPrice:F0} -> @{SellPrice:F0}) = {ProfitPerUnit:F0}/unit " +
               $"{ProfitMargin:P1} margin, {TotalProfit:F0} total";
    }
}