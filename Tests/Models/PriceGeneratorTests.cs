using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

/// <summary>
/// Tests for PriceGenerator: initial market generation, market refresh,
/// price calculation, buy/sell price calculation, trade routes, and random events.
/// </summary>
[Collection("Sequential")]
public class PriceGeneratorTests
{
    #region GenerateInitialMarket

    [Fact]
    public void GenerateInitialMarket_PopulatesPrices()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market
        {
            MarketId = "market_test",
            EconomyType = EconomyType.Balanced,
            Seed = 42
        };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        Assert.True(market.Prices.ContainsKey("water"));
        Assert.True(market.Supply.ContainsKey("water"));
        Assert.True(market.Demand.ContainsKey("water"));
    }

    [Fact]
    public void GenerateInitialMarket_PricesWithinBounds()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 50m, maxPrice: 500m);
        var market = new Market
        {
            MarketId = "market_test",
            EconomyType = EconomyType.Balanced,
            Seed = 42
        };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var price = market.GetPrice("water");
        Assert.True(price >= commodity.MinPrice, $"Price {price} below min {commodity.MinPrice}");
        Assert.True(price <= commodity.MaxPrice, $"Price {price} above max {commodity.MaxPrice}");
    }

    [Fact]
    public void GenerateInitialMarket_SupplyAndDemandNonNegative()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market
        {
            MarketId = "market_test",
            EconomyType = EconomyType.Balanced,
            Seed = 42
        };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        Assert.True(market.GetSupply("water") >= 0);
        Assert.True(market.GetDemand("water") >= 0);
    }

    [Fact]
    public void GenerateInitialMarket_SetsLastRefresh()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market
        {
            MarketId = "market_test",
            EconomyType = EconomyType.Balanced,
            Seed = 42
        };

        var before = DateTime.UtcNow;
        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        Assert.True(market.LastRefresh >= before);
        Assert.True(market.LastRefresh <= DateTime.UtcNow);
    }

    [Fact]
    public void GenerateInitialMarket_DeterministicWithSameSeed()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);

        var market1 = new Market { MarketId = "m1", EconomyType = EconomyType.Balanced, Seed = 123 };
        var market2 = new Market { MarketId = "m2", EconomyType = EconomyType.Balanced, Seed = 123 };

        PriceGenerator.GenerateInitialMarket(market1, new[] { commodity });
        PriceGenerator.GenerateInitialMarket(market2, new[] { commodity });

        Assert.Equal(market1.GetPrice("water"), market2.GetPrice("water"));
        Assert.Equal(market1.GetSupply("water"), market2.GetSupply("water"));
        Assert.Equal(market1.GetDemand("water"), market2.GetDemand("water"));
    }

    [Fact]
    public void GenerateInitialMarket_DifferentSeedsProduceDifferentResults()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);

        var market1 = new Market { MarketId = "m1", EconomyType = EconomyType.Balanced, Seed = 123 };
        var market2 = new Market { MarketId = "m2", EconomyType = EconomyType.Balanced, Seed = 456 };

        PriceGenerator.GenerateInitialMarket(market1, new[] { commodity });
        PriceGenerator.GenerateInitialMarket(market2, new[] { commodity });

        // With different seeds, at least one of price/supply/demand should differ
        bool anyDifferent = market1.GetPrice("water") != market2.GetPrice("water")
                         || market1.GetSupply("water") != market2.GetSupply("water")
                         || market1.GetDemand("water") != market2.GetDemand("water");

        Assert.True(anyDifferent, "Different seeds should produce different market data");
    }

    [Fact]
    public void GenerateInitialMarket_MultipleCommodities()
    {
        CommodityRegistry.Clear();
        var water = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var ore = CreateAndRegisterCommodity("iron_ore", "Iron Ore", CommodityCategory.Ore, 50m);
        var tech = CreateAndRegisterCommodity("electronics", "Electronics", CommodityCategory.Tech, 200m);

        var market = new Market
        {
            MarketId = "market_test",
            EconomyType = EconomyType.Balanced,
            Seed = 42
        };

        PriceGenerator.GenerateInitialMarket(market, new[] { water, ore, tech });

        Assert.Equal(3, market.Prices.Count);
        Assert.True(market.Prices.ContainsKey("water"));
        Assert.True(market.Prices.ContainsKey("iron_ore"));
        Assert.True(market.Prices.ContainsKey("electronics"));
    }

    [Fact]
    public void GenerateInitialMarket_IndustrialEconomy_ProducesMoreOre()
    {
        CommodityRegistry.Clear();
        var ore = CreateAndRegisterCommodity("iron_ore", "Iron Ore", CommodityCategory.Ore, 50m);
        var tech = CreateAndRegisterCommodity("electronics", "Electronics", CommodityCategory.Tech, 200m);

        var industrialMarket = new Market { MarketId = "ind", EconomyType = EconomyType.Industrial, Seed = 42 };
        var balancedMarket = new Market { MarketId = "bal", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(industrialMarket, new[] { ore, tech });
        PriceGenerator.GenerateInitialMarket(balancedMarket, new[] { ore, tech });

        // Industrial should have more ore supply
        Assert.True(industrialMarket.GetSupply("iron_ore") > balancedMarket.GetSupply("iron_ore"),
            $"Industrial ore supply {industrialMarket.GetSupply("iron_ore")} should exceed balanced {balancedMarket.GetSupply("iron_ore")}");
    }

    [Fact]
    public void GenerateInitialMarket_TechLevelAffectsTechSupply()
    {
        CommodityRegistry.Clear();
        var tech = CreateAndRegisterCommodity("electronics", "Electronics", CommodityCategory.Tech, 200m);

        var highTechMarket = new Market { MarketId = "ht", EconomyType = EconomyType.Balanced, Seed = 42, TechLevel = 10 };
        var lowTechMarket = new Market { MarketId = "lt", EconomyType = EconomyType.Balanced, Seed = 42, TechLevel = 0 };

        PriceGenerator.GenerateInitialMarket(highTechMarket, new[] { tech });
        PriceGenerator.GenerateInitialMarket(lowTechMarket, new[] { tech });

        // Higher tech level should produce more tech supply
        Assert.True(highTechMarket.GetSupply("electronics") > lowTechMarket.GetSupply("electronics"),
            $"High tech supply {highTechMarket.GetSupply("electronics")} should exceed low tech {lowTechMarket.GetSupply("electronics")}");
    }

    [Fact]
    public void GenerateInitialMarket_IllegalGoodsAffectedBySecurity()
    {
        CommodityRegistry.Clear();
        var illegal = CreateAndRegisterCommodity("contraband", "Contraband", CommodityCategory.Illegal, 500m);

        var criminalMarket = new Market { MarketId = "crim", EconomyType = EconomyType.Criminal, Seed = 42, TechLevel = 5 };
        var balancedMarket = new Market { MarketId = "bal", EconomyType = EconomyType.Balanced, Seed = 42, TechLevel = 5 };

        PriceGenerator.GenerateInitialMarket(criminalMarket, new[] { illegal });
        PriceGenerator.GenerateInitialMarket(balancedMarket, new[] { illegal });

        // Criminal economy should have more illegal supply
        Assert.True(criminalMarket.GetSupply("contraband") > balancedMarket.GetSupply("contraband"),
            $"Criminal illegal supply {criminalMarket.GetSupply("contraband")} should exceed balanced {balancedMarket.GetSupply("contraband")}");
    }

    #endregion

    #region RefreshMarket

    [Fact]
    public void RefreshMarket_UpdatesLastRefresh()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });
        var oldRefresh = market.LastRefresh;

        var newTime = oldRefresh.AddHours(24);
        PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

        Assert.Equal(newTime, market.LastRefresh);
    }

    [Fact]
    public void RefreshMarket_PricesStayWithinBounds()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 50m, maxPrice: 500m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        // Refresh multiple times
        for (int i = 0; i < 10; i++)
        {
            var newTime = market.LastRefresh.AddHours(24);
            PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

            var price = market.GetPrice("water");
            Assert.True(price >= commodity.MinPrice,
                $"Iteration {i}: Price {price} below min {commodity.MinPrice}");
            Assert.True(price <= commodity.MaxPrice,
                $"Iteration {i}: Price {price} above max {commodity.MaxPrice}");
        }
    }

    [Fact]
    public void RefreshMarket_SupplyAndDemandStayNonNegative()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        for (int i = 0; i < 10; i++)
        {
            var newTime = market.LastRefresh.AddHours(24);
            PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

            Assert.True(market.GetSupply("water") >= 0, $"Iteration {i}: supply negative");
            Assert.True(market.GetDemand("water") >= 0, $"Iteration {i}: demand negative");
        }
    }

    [Fact]
    public void RefreshMarket_RemovesExpiredEvents()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var expiredEvent = new MarketEvent
        {
            Type = MarketEventType.Shortage,
            CommodityId = "water",
            StartTime = market.LastRefresh.AddHours(-10),
            EndTime = market.LastRefresh.AddHours(-1),
            PriceMultiplier = 2.0
        };
        market.ActiveEvents.Add(expiredEvent);

        var newTime = market.LastRefresh.AddHours(24);
        PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

        Assert.Empty(market.ActiveEvents);
    }

    [Fact]
    public void RefreshMarket_KeepsActiveEvents()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var activeEvent = new MarketEvent
        {
            Type = MarketEventType.Shortage,
            CommodityId = "water",
            StartTime = market.LastRefresh,
            EndTime = market.LastRefresh.AddHours(48),
            PriceMultiplier = 2.0
        };
        market.ActiveEvents.Add(activeEvent);

        var newTime = market.LastRefresh.AddHours(24);
        PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

        Assert.Single(market.ActiveEvents);
    }

    [Fact]
    public void RefreshMarket_ActiveEventsAffectPrices()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });
        var priceWithoutEvent = market.GetPrice("water");

        // Add a severe shortage event
        var shortageEvent = new MarketEvent
        {
            Type = MarketEventType.Shortage,
            CommodityId = "water",
            StartTime = market.LastRefresh,
            EndTime = market.LastRefresh.AddHours(48),
            PriceMultiplier = 3.0,
            SupplyMultiplier = 0.2,
            DemandMultiplier = 2.0
        };
        market.ActiveEvents.Add(shortageEvent);

        var newTime = market.LastRefresh.AddHours(24);
        PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

        // Price should be affected by the event
        var priceWithEvent = market.GetPrice("water");
        Assert.NotEqual(priceWithoutEvent, priceWithEvent);
    }

    [Fact]
    public void RefreshMarket_PriceChangeIsSmoothed()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            volatility: 0.05, minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });
        var oldPrice = market.GetPrice("water");

        // Refresh with small time step
        var newTime = market.LastRefresh.AddHours(1);
        PriceGenerator.RefreshMarket(market, new[] { commodity }, newTime);

        var newPrice = market.GetPrice("water");
        var maxChange = oldPrice * (decimal)commodity.Volatility * 0.5m;
        var actualChange = Math.Abs(newPrice - oldPrice);

        Assert.True(actualChange <= maxChange,
            $"Price change {actualChange} exceeds max allowed {maxChange}");
    }

    #endregion

    #region CalculateBuyPrice

    [Fact]
    public void CalculateBuyPrice_ReturnsPriceWithinBounds()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var buyPrice = PriceGenerator.CalculateBuyPrice(market, commodity);

        Assert.True(buyPrice >= commodity.MinPrice);
        Assert.True(buyPrice <= commodity.MaxPrice);
    }

    [Fact]
    public void CalculateBuyPrice_IsLessThanSellPrice()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var sellPrice = market.GetPrice("water");
        var buyPrice = PriceGenerator.CalculateBuyPrice(market, commodity);

        Assert.True(buyPrice <= sellPrice,
            $"Buy price {buyPrice} should be <= sell price {sellPrice}");
    }

    [Fact]
    public void CalculateBuyPrice_OversuppliedMarket_LowerBuyPrice()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        // Artificially create oversupply
        market.Supply["water"] = 1000;
        market.Demand["water"] = 10;

        var buyPrice = PriceGenerator.CalculateBuyPrice(market, commodity);
        var sellPrice = market.GetPrice("water");

        // Buy price should be significantly lower than sell price in oversupplied market
        Assert.True(buyPrice < sellPrice * 0.95m,
            $"Buy price {buyPrice} should be significantly lower than sell price {sellPrice} in oversupplied market");
    }

    [Fact]
    public void CalculateBuyPrice_LargeQuantity_ReducesPrice()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var buyPrice1 = PriceGenerator.CalculateBuyPrice(market, commodity, 1);
        var buyPrice100 = PriceGenerator.CalculateBuyPrice(market, commodity, 100);

        // Large quantities should get worse prices
        Assert.True(buyPrice100 <= buyPrice1,
            $"Large quantity buy price {buyPrice100} should be <= small quantity {buyPrice1}");
    }

    #endregion

    #region CalculateSellPrice

    [Fact]
    public void CalculateSellPrice_ReturnsPriceWithinBounds()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var sellPrice = PriceGenerator.CalculateSellPrice(market, commodity);

        Assert.True(sellPrice >= commodity.MinPrice);
        Assert.True(sellPrice <= commodity.MaxPrice);
    }

    [Fact]
    public void CalculateSellPrice_LargeQuantity_IncreasesPrice()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var sellPrice1 = PriceGenerator.CalculateSellPrice(market, commodity, 1);
        var sellPrice100 = PriceGenerator.CalculateSellPrice(market, commodity, 100);

        // Large purchases should have a premium
        Assert.True(sellPrice100 >= sellPrice1,
            $"Large quantity sell price {sellPrice100} should be >= small quantity {sellPrice1}");
    }

    #endregion

    #region CalculateTradeProfit

    [Fact]
    public void CalculateTradeProfit_ReturnsAllComponents()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 100m,
            minPrice: 10m, maxPrice: 10000m);

        var buyMarket = new Market { MarketId = "buy_market", EconomyType = EconomyType.Agricultural, Seed = 42 };
        var sellMarket = new Market { MarketId = "sell_market", EconomyType = EconomyType.Service, Seed = 99 };

        PriceGenerator.GenerateInitialMarket(buyMarket, new[] { commodity });
        PriceGenerator.GenerateInitialMarket(sellMarket, new[] { commodity });

        var (buyPrice, sellPrice, profitPerUnit, profitMargin) =
            PriceGenerator.CalculateTradeProfit(buyMarket, sellMarket, commodity, 10);

        Assert.True(buyPrice > 0);
        Assert.True(sellPrice > 0);
        Assert.Equal(sellPrice - buyPrice, profitPerUnit);
        if (buyPrice > 0)
            Assert.Equal((double)(profitPerUnit / buyPrice), profitMargin);
    }

    [Fact]
    public void CalculateTradeProfit_ProfitableRoute_HasPositiveProfit()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("ore", "Ore", CommodityCategory.Ore, 50m,
            minPrice: 10m, maxPrice: 10000m);

        // Mining produces ore cheaply, Service needs it
        var miningMarket = new Market { MarketId = "mining", EconomyType = EconomyType.Mining, Seed = 42 };
        var serviceMarket = new Market { MarketId = "service", EconomyType = EconomyType.Service, Seed = 99 };

        PriceGenerator.GenerateInitialMarket(miningMarket, new[] { commodity });
        PriceGenerator.GenerateInitialMarket(serviceMarket, new[] { commodity });

        var (_, _, profitPerUnit, _) =
            PriceGenerator.CalculateTradeProfit(miningMarket, serviceMarket, commodity, 10);

        // Mining -> Service for ore should be profitable
        Assert.True(profitPerUnit > 0,
            $"Expected positive profit for Mining->Service ore trade, got {profitPerUnit}");
    }

    #endregion

    #region FindBestTradeRoutes

    [Fact]
    public void FindBestTradeRoutes_ReturnsRoutes()
    {
        CommodityRegistry.Clear();
        var ore = CreateAndRegisterCommodity("ore", "Ore", CommodityCategory.Ore, 50m,
            minPrice: 10m, maxPrice: 10000m);
        var food = CreateAndRegisterCommodity("food", "Food", CommodityCategory.Organics, 25m,
            minPrice: 5m, maxPrice: 5000m);

        var miningMarket = new Market { MarketId = "mining", Name = "Mining Colony", EconomyType = EconomyType.Mining, Seed = 42 };
        var agriMarket = new Market { MarketId = "agri", Name = "Farm World", EconomyType = EconomyType.Agricultural, Seed = 99 };
        var serviceMarket = new Market { MarketId = "service", Name = "Service Hub", EconomyType = EconomyType.Service, Seed = 123 };

        PriceGenerator.GenerateInitialMarket(miningMarket, new[] { ore, food });
        PriceGenerator.GenerateInitialMarket(agriMarket, new[] { ore, food });
        PriceGenerator.GenerateInitialMarket(serviceMarket, new[] { ore, food });

        var routes = PriceGenerator.FindBestTradeRoutes(
            miningMarket,
            new[] { agriMarket, serviceMarket },
            new[] { ore, food },
            maxResults: 10);

        Assert.NotEmpty(routes);
        Assert.All(routes, r => Assert.True(r.ProfitPerUnit > 0));
        Assert.All(routes, r => Assert.True(r.TotalProfit > 0));
    }

    [Fact]
    public void FindBestTradeRoutes_ExcludesSourceMarket()
    {
        CommodityRegistry.Clear();
        var ore = CreateAndRegisterCommodity("ore", "Ore", CommodityCategory.Ore, 50m,
            minPrice: 10m, maxPrice: 10000m);

        var miningMarket = new Market { MarketId = "mining", Name = "Mining Colony", EconomyType = EconomyType.Mining, Seed = 42 };
        var otherMarket = new Market { MarketId = "other", Name = "Other", EconomyType = EconomyType.Balanced, Seed = 99 };

        PriceGenerator.GenerateInitialMarket(miningMarket, new[] { ore });
        PriceGenerator.GenerateInitialMarket(otherMarket, new[] { ore });

        var routes = PriceGenerator.FindBestTradeRoutes(
            miningMarket,
            new[] { miningMarket, otherMarket }, // Include source
            new[] { ore });

        // No route should have source == destination
        Assert.All(routes, r => Assert.NotEqual(r.SourceMarketId, r.DestinationMarketId));
    }

    [Fact]
    public void FindBestTradeRoutes_RespectsMaxResults()
    {
        CommodityRegistry.Clear();
        var commodities = new List<Commodity>();
        for (int i = 0; i < 5; i++)
        {
            commodities.Add(CreateAndRegisterCommodity($"comm_{i}", $"Commodity {i}", CommodityCategory.Ore, 50m,
                minPrice: 10m, maxPrice: 10000m));
        }

        var markets = new List<Market>();
        for (int i = 0; i < 5; i++)
        {
            var m = new Market { MarketId = $"market_{i}", Name = $"Market {i}", EconomyType = EconomyType.Balanced, Seed = i * 100 };
            PriceGenerator.GenerateInitialMarket(m, commodities);
            markets.Add(m);
        }

        var routes = PriceGenerator.FindBestTradeRoutes(
            markets[0], markets.Skip(1), commodities, maxResults: 3);

        Assert.True(routes.Count <= 3);
    }

    [Fact]
    public void FindBestTradeRoutes_SortedByTotalProfit()
    {
        CommodityRegistry.Clear();
        var ore = CreateAndRegisterCommodity("ore", "Ore", CommodityCategory.Ore, 50m,
            minPrice: 10m, maxPrice: 10000m);

        var miningMarket = new Market { MarketId = "mining", Name = "Mining", EconomyType = EconomyType.Mining, Seed = 42 };
        var market1 = new Market { MarketId = "dest1", Name = "Dest 1", EconomyType = EconomyType.Service, Seed = 99 };
        var market2 = new Market { MarketId = "dest2", Name = "Dest 2", EconomyType = EconomyType.Service, Seed = 123 };

        PriceGenerator.GenerateInitialMarket(miningMarket, new[] { ore });
        PriceGenerator.GenerateInitialMarket(market1, new[] { ore });
        PriceGenerator.GenerateInitialMarket(market2, new[] { ore });

        var routes = PriceGenerator.FindBestTradeRoutes(
            miningMarket, new[] { market1, market2 }, new[] { ore });

        if (routes.Count >= 2)
        {
            Assert.True(routes[0].TotalProfit >= routes[1].TotalProfit,
                "Routes should be sorted by total profit descending");
        }
    }

    #endregion

    #region ExecuteTrade

    [Fact]
    public void ExecuteTrade_UpdatesBothMarkets()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m,
            minPrice: 5m, maxPrice: 500m);

        var buyMarket = new Market { MarketId = "buy", EconomyType = EconomyType.Agricultural, Seed = 42 };
        var sellMarket = new Market { MarketId = "sell", EconomyType = EconomyType.Service, Seed = 99 };

        PriceGenerator.GenerateInitialMarket(buyMarket, new[] { commodity });
        PriceGenerator.GenerateInitialMarket(sellMarket, new[] { commodity });

        var buySupplyBefore = buyMarket.GetSupply("water");
        var sellSupplyBefore = sellMarket.GetSupply("water");
        var buyDemandBefore = buyMarket.GetDemand("water");
        var sellDemandBefore = sellMarket.GetDemand("water");

        PriceGenerator.ExecuteTrade(buyMarket, sellMarket, commodity, 10);

        // Both BuyFromMarket and SellToMarket decrease supply (named from market's perspective)
        // BuyFromMarket: market buys from player -> supply decreases
        // SellToMarket: market sells to player -> supply decreases
        Assert.True(buyMarket.GetSupply("water") < buySupplyBefore,
            "Buy market supply should decrease (market bought from player)");
        Assert.True(sellMarket.GetSupply("water") < sellSupplyBefore,
            "Sell market supply should decrease (market sold to player)");

        // Both operations increase demand
        Assert.True(buyMarket.GetDemand("water") > buyDemandBefore,
            "Buy market demand should increase");
        Assert.True(sellMarket.GetDemand("water") > sellDemandBefore,
            "Sell market demand should increase");
    }

    [Fact]
    public void ExecuteTrade_DoesNotExceedSupply()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m,
            minPrice: 5m, maxPrice: 500m);

        var buyMarket = new Market { MarketId = "buy", EconomyType = EconomyType.Agricultural, Seed = 42 };
        var sellMarket = new Market { MarketId = "sell", EconomyType = EconomyType.Service, Seed = 99 };

        PriceGenerator.GenerateInitialMarket(buyMarket, new[] { commodity });
        PriceGenerator.GenerateInitialMarket(sellMarket, new[] { commodity });

        var buySupplyBefore = buyMarket.GetSupply("water");

        // Try to trade more than available
        PriceGenerator.ExecuteTrade(buyMarket, sellMarket, commodity, buySupplyBefore + 1000);

        // Supply should not go negative
        Assert.True(buyMarket.GetSupply("water") >= 0);
    }

    #endregion

    #region GenerateRandomEvent

    [Fact]
    public void GenerateRandomEvent_WithHighChance_ReturnsEvent()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var evt = PriceGenerator.GenerateRandomEvent(market, new[] { commodity }, DateTime.UtcNow, chance: 1.0);

        Assert.NotNull(evt);
        Assert.NotEmpty(evt!.Description);
        Assert.True(evt.Severity >= 20 && evt.Severity <= 100);
    }

    [Fact]
    public void GenerateRandomEvent_WithZeroChance_ReturnsNull()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var evt = PriceGenerator.GenerateRandomEvent(market, new[] { commodity }, DateTime.UtcNow, chance: 0.0);

        Assert.Null(evt);
    }

    [Fact]
    public void GenerateRandomEvent_EventHasValidDuration()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        var now = DateTime.UtcNow;
        var evt = PriceGenerator.GenerateRandomEvent(market, new[] { commodity }, now, chance: 1.0);

        Assert.NotNull(evt);
        Assert.Equal(now, evt!.StartTime);
        Assert.True(evt.EndTime > evt.StartTime);
        var duration = evt.EndTime - evt.StartTime;
        Assert.True(duration.TotalHours >= 6 && duration.TotalHours <= 54,
            $"Duration {duration.TotalHours}h should be between 6 and 54");
    }

    [Fact]
    public void GenerateRandomEvent_MultipliersAreClamped()
    {
        CommodityRegistry.Clear();
        var commodity = CreateAndRegisterCommodity("water", "Water", CommodityCategory.Organics, 10m);
        var market = new Market { MarketId = "m", EconomyType = EconomyType.Balanced, Seed = 42 };

        PriceGenerator.GenerateInitialMarket(market, new[] { commodity });

        // Generate many events to ensure clamping works
        for (int i = 0; i < 20; i++)
        {
            var evt = PriceGenerator.GenerateRandomEvent(market, new[] { commodity }, DateTime.UtcNow, chance: 1.0);
            if (evt != null)
            {
                Assert.True(evt.PriceMultiplier >= 0.1 && evt.PriceMultiplier <= 5.0,
                    $"PriceMultiplier {evt.PriceMultiplier} out of [0.1, 5.0]");
                Assert.True(evt.SupplyMultiplier >= 0.05 && evt.SupplyMultiplier <= 5.0,
                    $"SupplyMultiplier {evt.SupplyMultiplier} out of [0.05, 5.0]");
                Assert.True(evt.DemandMultiplier >= 0.05 && evt.DemandMultiplier <= 5.0,
                    $"DemandMultiplier {evt.DemandMultiplier} out of [0.05, 5.0]");
            }
        }
    }

    #endregion

    #region TradeRoute

    [Fact]
    public void TradeRoute_ToString_ContainsKeyInfo()
    {
        var route = new TradeRoute
        {
            SourceMarketName = "Mining Colony",
            DestinationMarketName = "Service Hub",
            CommodityName = "Iron Ore",
            MaxQuantity = 50,
            BuyPrice = 30m,
            SellPrice = 80m,
            ProfitPerUnit = 50m,
            ProfitMargin = 1.67,
            TotalProfit = 2500m
        };

        var str = route.ToString();

        Assert.Contains("Mining Colony", str);
        Assert.Contains("Service Hub", str);
        Assert.Contains("Iron Ore", str);
        Assert.Contains("50", str);
    }

    #endregion

    #region Helpers

    private static Commodity CreateAndRegisterCommodity(
        string id, string name, CommodityCategory category, decimal basePrice,
        double volatility = 0.1, decimal minPrice = 10m, decimal maxPrice = 10000m)
    {
        var commodity = new Commodity
        {
            Id = id,
            Name = name,
            Category = category,
            BasePrice = basePrice,
            Volatility = volatility,
            Legality = CommodityLegality.Legal,
            BaseVolume = 100,
            MassPerUnit = 1.0,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };
        CommodityRegistry.Register(commodity);
        return commodity;
    }

    #endregion
}
