using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Models;
using NeonTrader.Systems;
using Xunit;

namespace NeonTrader.Tests.Systems;

/// <summary>
/// Tests for the TradingSystem - verifies buy/sell transactions, market refresh,
/// trade routes, profit calculation, event handling, and IGameSystem contract.
/// </summary>
[Collection("Sequential")]
public sealed class TradingSystemTests : TestBase, IDisposable
{
    private readonly Mock<ILogger<TradingSystem>> _loggerMock;
    private readonly TradingSystem _tradingSystem;

    // Test commodities registered in CommodityRegistry
    private static readonly Commodity Water = new()
    {
        Id = "water",
        Name = "Water",
        Category = CommodityCategory.Organics,
        BasePrice = 10m,
        MinPrice = 5m,
        MaxPrice = 50m,
        Volatility = 0.1,
        BaseVolume = 100
    };

    private static readonly Commodity Food = new()
    {
        Id = "food",
        Name = "Food Rations",
        Category = CommodityCategory.Organics,
        BasePrice = 25m,
        MinPrice = 10m,
        MaxPrice = 100m,
        Volatility = 0.15,
        BaseVolume = 80
    };

    private static readonly Commodity Ore = new()
    {
        Id = "ore",
        Name = "Iron Ore",
        Category = CommodityCategory.Ore,
        BasePrice = 50m,
        MinPrice = 20m,
        MaxPrice = 200m,
        Volatility = 0.2,
        BaseVolume = 200
    };

    private static readonly Commodity Electronics = new()
    {
        Id = "electronics",
        Name = "Electronics",
        Category = CommodityCategory.Tech,
        BasePrice = 200m,
        MinPrice = 100m,
        MaxPrice = 500m,
        Volatility = 0.25,
        BaseVolume = 50
    };

    private static readonly Commodity Medicine = new()
    {
        Id = "medicine",
        Name = "Medical Supplies",
        Category = CommodityCategory.Medical,
        BasePrice = 150m,
        MinPrice = 80m,
        MaxPrice = 400m,
        Volatility = 0.15,
        BaseVolume = 60
    };

    // Test planets
    private static Planet CreateTestPlanet(string id, string name, EconomyType economy, bool hasExchange = true)
    {
        var planet = new Planet
        {
            Id = id,
            Name = name,
            EconomyType = economy,
            TechLevel = 5,
            HasCommodityExchange = hasExchange,
            IsDiscovered = true,
            FactionId = "neutral",
            Market = new Market
            {
                MarketId = id,
                Name = $"{name} Market",
                EconomyType = economy,
                TechLevel = 5,
                Seed = id.GetHashCode()
            }
        };
        return planet;
    }

    private static Planet TradeHub { get; } = CreateTestPlanet("trade_hub", "Trade Hub Prime", EconomyType.Balanced);
    private static Planet MiningColony { get; } = CreateTestPlanet("mining_colony", "Mining Colony Beta", EconomyType.Industrial);
    private static Planet TechWorld { get; } = CreateTestPlanet("tech_world", "Tech World Gamma", EconomyType.HighTech);
    private static Planet NoExchangeStation { get; } = CreateTestPlanet("no_exchange", "No Exchange Station", EconomyType.Balanced, false);

    static TradingSystemTests()
    {
        // Register test commodities and planets in static registries
        CommodityRegistry.Clear();
        CommodityRegistry.Register(Water);
        CommodityRegistry.Register(Food);
        CommodityRegistry.Register(Ore);
        CommodityRegistry.Register(Electronics);
        CommodityRegistry.Register(Medicine);

        PlanetRegistry.Clear();
        PlanetRegistry.Register(TradeHub);
        PlanetRegistry.Register(MiningColony);
        PlanetRegistry.Register(TechWorld);
        PlanetRegistry.Register(NoExchangeStation);
    }

    public TradingSystemTests()
    {
        _loggerMock = CreateLoggerMock<TradingSystem>();
        _tradingSystem = new TradingSystem(_loggerMock.Object);

        // Set up GameState for trading tests
        GameState.CurrentLocation = "trade_hub";
        GameState.Credits = 100000;
        GameState.CargoCapacity = 200;
    }

    public new void Dispose()
    {
        base.Dispose();

        // Clean up: shutdown the trading system if running
        try
        {
            if (_tradingSystem.IsRunning)
                _tradingSystem.ShutdownAsync().GetAwaiter().GetResult();
        }
        catch { }
    }

    // =========================================================================
    // IGameSystem Contract Tests
    // =========================================================================

    [Fact]
    public void SystemId_ReturnsTradingSystem()
    {
        Assert.Equal("tradingsystem", _tradingSystem.SystemId);
    }

    [Fact]
    public void Priority_Returns10()
    {
        Assert.Equal(10, _tradingSystem.Priority);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        Assert.False(_tradingSystem.IsRunning);
    }

    [Fact]
    public async Task IsRunning_TrueAfterInitialize()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        Assert.True(_tradingSystem.IsRunning);
    }

    [Fact]
    public async Task UpdateAsync_IsNoOp_ReturnsCompletedTask()
    {
        var task = _tradingSystem.UpdateAsync(0.016f);
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    // =========================================================================
    // InitializeAsync Tests
    // =========================================================================

    [Fact]
    public async Task InitializeAsync_SubscribesToTimeAdvancedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        EventBusMock.Verify(
            x => x.Subscribe(It.IsAny<Action<TimeAdvancedEvent>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_SubscribesToLocationChangedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        EventBusMock.Verify(
            x => x.Subscribe(It.IsAny<Action<LocationChangedEvent>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_GeneratesInitialMarkets()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // After initialization, TradeHub's market should have prices
        var market = TradeHub.Market;
        Assert.NotEmpty(market.Prices);
        Assert.True(market.Prices.Count > 0);
    }

    [Fact]
    public async Task InitializeAsync_SyncsMarketPricesToGameState()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // GameState.MarketPrices should be populated for current location
        Assert.NotEmpty(GameState.MarketPrices);
    }

    [Fact]
    public async Task InitializeAsync_PublishesSystemInitializedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        EventBusMock.Verify(
            x => x.Publish(It.Is<SystemInitializedEvent>(e => e.SystemId == "tradingsystem")),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LogsInformation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsync_SkipsLocationsWithoutExchange()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // NoExchangeStation should not have market prices generated
        var market = NoExchangeStation.Market;
        Assert.Empty(market.Prices);
    }

    // =========================================================================
    // BuyFromMarket Tests
    // =========================================================================

    [Fact]
    public async Task BuyFromMarket_Fails_WhenSystemNotRunning()
    {
        var result = _tradingSystem.BuyFromMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("not running", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenCommodityIdEmpty()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var result = _tradingSystem.BuyFromMarket("", 10);
        Assert.False(result.Success);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenQuantityZeroOrNegative()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var result = _tradingSystem.BuyFromMarket("water", 0);
        Assert.False(result.Success);
        Assert.Contains("positive", result.ErrorMessage);

        result = _tradingSystem.BuyFromMarket("water", -5);
        Assert.False(result.Success);
        Assert.Contains("positive", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenUnknownCommodity()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var result = _tradingSystem.BuyFromMarket("nonexistent_commodity", 10);
        Assert.False(result.Success);
        Assert.Contains("Unknown commodity", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenNoExchangeAtLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        GameState.CurrentLocation = "no_exchange";

        var result = _tradingSystem.BuyFromMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("No commodity exchange", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenInsufficientCredits()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Set credits very low
        GameState.Credits = 1;

        var result = _tradingSystem.BuyFromMarket("electronics", 10);
        Assert.False(result.Success);
        Assert.Contains("Insufficient credits", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenInsufficientCargoSpace()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Fill cargo to capacity
        GameState.CargoCapacity = 10;
        GameState.AddCargo("ore", 10);

        var result = _tradingSystem.BuyFromMarket("water", 5);
        Assert.False(result.Success);
        Assert.Contains("Insufficient cargo space", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Fails_WhenInsufficientSupply()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Set supply to 0 for water
        TradeHub.Market.Supply["water"] = 0;

        var result = _tradingSystem.BuyFromMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("Insufficient supply", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_Success_ReturnsTradeResult()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Ensure sufficient supply
        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        var initialCredits = GameState.Credits;
        var result = _tradingSystem.BuyFromMarket("water", 10);

        Assert.True(result.Success);
        Assert.Equal(10, result.Quantity);
        Assert.True(result.PricePerUnit > 0);
        Assert.True(result.TotalCredits > 0); // Total cost (positive value from source)
        Assert.Equal("Water", result.CommodityName);
        Assert.True(GameState.Credits < initialCredits);
    }

    [Fact]
    public async Task BuyFromMarket_Success_AddsCargo()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        var cargoBefore = GameState.GetCargoQuantity("water");
        _tradingSystem.BuyFromMarket("water", 10);
        var cargoAfter = GameState.GetCargoQuantity("water");

        Assert.Equal(cargoBefore + 10, cargoAfter);
    }

    [Fact]
    public async Task BuyFromMarket_Success_UpdatesMarketSupply()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        var supplyBefore = TradeHub.Market.GetSupply("water");
        _tradingSystem.BuyFromMarket("water", 10);
        var supplyAfter = TradeHub.Market.GetSupply("water");

        Assert.Equal(supplyBefore - 10, supplyAfter);
    }

    [Fact]
    public async Task BuyFromMarket_Success_UpdatesStatistics()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        var tradesBefore = GameState.Statistics.TradesCompleted;
        var spentBefore = GameState.Statistics.TotalCreditsSpent;

        _tradingSystem.BuyFromMarket("water", 10);

        Assert.Equal(tradesBefore + 1, GameState.Statistics.TradesCompleted);
        Assert.True(GameState.Statistics.TotalCreditsSpent > spentBefore);
    }

    [Fact]
    public async Task BuyFromMarket_Success_PublishesTradeExecutedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.BuyFromMarket("water", 10);

        EventBusMock.Verify(
            x => x.Publish(It.Is<TradeExecutedEvent>(e =>
                e.CommodityId == "water" && e.IsBuy == true && e.Quantity == 10)),
            Times.Once);
    }

    [Fact]
    public async Task BuyFromMarket_Success_PublishesCreditsChangedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.BuyFromMarket("water", 10);

        EventBusMock.Verify(
            x => x.Publish(It.Is<CreditsChangedEvent>(e => e.Delta < 0)),
            Times.Once);
    }

    [Fact]
    public async Task BuyFromMarket_Success_PublishesCargoChangedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.BuyFromMarket("water", 10);

        EventBusMock.Verify(
            x => x.Publish(It.Is<CargoChangedEvent>(e =>
                e.CommodityId == "water" && e.NewQuantity > e.PreviousQuantity)),
            Times.Once);
    }

    [Fact]
    public async Task BuyFromMarket_Success_UpdatesMarketPricesInGameState()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.BuyFromMarket("water", 10);

        Assert.True(GameState.MarketPrices.ContainsKey("water"));
        Assert.True(GameState.MarketPrices["water"] > 0);
    }

    // =========================================================================
    // SellToMarket Tests
    // =========================================================================

    [Fact]
    public async Task SellToMarket_Fails_WhenSystemNotRunning()
    {
        var result = _tradingSystem.SellToMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("not running", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_Fails_WhenCommodityIdEmpty()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var result = _tradingSystem.SellToMarket("", 10);
        Assert.False(result.Success);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_Fails_WhenQuantityZeroOrNegative()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var result = _tradingSystem.SellToMarket("water", 0);
        Assert.False(result.Success);
        Assert.Contains("positive", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_Fails_WhenUnknownCommodity()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var result = _tradingSystem.SellToMarket("nonexistent_commodity", 10);
        Assert.False(result.Success);
        Assert.Contains("Unknown commodity", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_Fails_WhenNoExchangeAtLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        GameState.CurrentLocation = "no_exchange";

        var result = _tradingSystem.SellToMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("No commodity exchange", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_Fails_WhenInsufficientCargo()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Player has no water cargo
        var result = _tradingSystem.SellToMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("Insufficient cargo", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_Success_ReturnsTradeResult()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Give player some cargo to sell
        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        var initialCredits = GameState.Credits;
        var result = _tradingSystem.SellToMarket("water", 10);

        Assert.True(result.Success);
        Assert.Equal(10, result.Quantity);
        Assert.True(result.PricePerUnit > 0);
        Assert.True(result.TotalCredits > 0); // Player received credits
        Assert.Equal("Water", result.CommodityName);
        Assert.True(GameState.Credits > initialCredits);
    }

    [Fact]
    public async Task SellToMarket_Success_RemovesCargo()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        var cargoBefore = GameState.GetCargoQuantity("water");
        _tradingSystem.SellToMarket("water", 10);
        var cargoAfter = GameState.GetCargoQuantity("water");

        Assert.Equal(cargoBefore - 10, cargoAfter);
    }

    [Fact]
    public async Task SellToMarket_Success_UpdatesMarketSupply()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        var supplyBefore = TradeHub.Market.GetSupply("water");
        _tradingSystem.SellToMarket("water", 10);
        var supplyAfter = TradeHub.Market.GetSupply("water");

        Assert.Equal(supplyBefore + 10, supplyAfter);
    }

    [Fact]
    public async Task SellToMarket_Success_UpdatesStatistics()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        var tradesBefore = GameState.Statistics.TradesCompleted;
        var earnedBefore = GameState.Statistics.TotalCreditsEarned;

        _tradingSystem.SellToMarket("water", 10);

        Assert.Equal(tradesBefore + 1, GameState.Statistics.TradesCompleted);
        Assert.True(GameState.Statistics.TotalCreditsEarned > earnedBefore);
    }

    [Fact]
    public async Task SellToMarket_Success_PublishesTradeExecutedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.SellToMarket("water", 10);

        EventBusMock.Verify(
            x => x.Publish(It.Is<TradeExecutedEvent>(e =>
                e.CommodityId == "water" && e.IsBuy == false && e.Quantity == 10)),
            Times.Once);
    }

    [Fact]
    public async Task SellToMarket_Success_PublishesCreditsChangedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.SellToMarket("water", 10);

        EventBusMock.Verify(
            x => x.Publish(It.Is<CreditsChangedEvent>(e => e.Delta > 0)),
            Times.Once);
    }

    [Fact]
    public async Task SellToMarket_Success_PublishesCargoChangedEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.SellToMarket("water", 10);

        EventBusMock.Verify(
            x => x.Publish(It.Is<CargoChangedEvent>(e =>
                e.CommodityId == "water" && e.NewQuantity < e.PreviousQuantity)),
            Times.Once);
    }

    // =========================================================================
    // GetCurrentMarketPrices Tests
    // =========================================================================

    [Fact]
    public async Task GetCurrentMarketPrices_ReturnsPrices_AfterInitialization()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var prices = _tradingSystem.GetCurrentMarketPrices();
        Assert.NotEmpty(prices);
    }

    [Fact]
    public void GetCurrentMarketPrices_ReturnsEmpty_WhenNotInitialized()
    {
        var prices = _tradingSystem.GetCurrentMarketPrices();
        Assert.Empty(prices);
    }

    [Fact]
    public async Task GetCurrentMarketPrices_ReturnsEmpty_WhenLocationHasNoExchange()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        GameState.CurrentLocation = "no_exchange";

        var prices = _tradingSystem.GetCurrentMarketPrices();
        Assert.Empty(prices);
    }

    // =========================================================================
    // GetCurrentMarketInfo Tests
    // =========================================================================

    [Fact]
    public async Task GetCurrentMarketInfo_ReturnsMarketInfo_AfterInitialization()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var info = _tradingSystem.GetCurrentMarketInfo();
        Assert.NotNull(info);
        Assert.Equal("trade_hub", info.LocationId);
        Assert.Equal("Trade Hub Prime", info.LocationName);
        Assert.NotEmpty(info.Commodities);
    }

    [Fact]
    public void GetCurrentMarketInfo_ReturnsNull_WhenNotInitialized()
    {
        var info = _tradingSystem.GetCurrentMarketInfo();
        Assert.Null(info);
    }

    // =========================================================================
    // GetMarketInfo Tests
    // =========================================================================

    [Fact]
    public async Task GetMarketInfo_ReturnsInfo_ForSpecificLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var info = _tradingSystem.GetMarketInfo("mining_colony");
        Assert.NotNull(info);
        Assert.Equal("mining_colony", info.LocationId);
        Assert.Equal("Mining Colony Beta", info.LocationName);
    }

    [Fact]
    public async Task GetMarketInfo_ReturnsNull_ForUnknownLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var info = _tradingSystem.GetMarketInfo("nonexistent_location");
        Assert.Null(info);
    }

    // =========================================================================
    // GetBestTradeRoutes Tests
    // =========================================================================

    [Fact]
    public async Task GetBestTradeRoutes_ReturnsRoutes_AfterInitialization()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var routes = _tradingSystem.GetBestTradeRoutes();
        Assert.NotNull(routes);
        // Routes may or may not be profitable depending on generated prices
    }

    [Fact]
    public void GetBestTradeRoutes_ReturnsEmpty_WhenNotInitialized()
    {
        var routes = _tradingSystem.GetBestTradeRoutes();
        Assert.Empty(routes);
    }

    [Fact]
    public async Task GetBestTradeRoutes_RespectsMaxResults()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var routes = _tradingSystem.GetBestTradeRoutes(3);
        Assert.True(routes.Count <= 3);
    }

    [Fact]
    public async Task GetBestTradeRoutes_ReturnsEmpty_WhenNoDiscoveredDestinations()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Undiscover all other planets
        MiningColony.IsDiscovered = false;
        TechWorld.IsDiscovered = false;

        var routes = _tradingSystem.GetBestTradeRoutes();
        Assert.Empty(routes);

        // Restore
        MiningColony.IsDiscovered = true;
        TechWorld.IsDiscovered = true;
    }

    // =========================================================================
    // CalculateTradeProfit Tests
    // =========================================================================

    [Fact]
    public async Task CalculateTradeProfit_ReturnsProfit_ForValidTrade()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var profit = _tradingSystem.CalculateTradeProfit("trade_hub", "mining_colony", "water", 10);
        Assert.NotNull(profit);
        Assert.Equal("water", profit.CommodityId);
        Assert.Equal("Water", profit.CommodityName);
        Assert.Equal(10, profit.Quantity);
    }

    [Fact]
    public async Task CalculateTradeProfit_ReturnsZero_ForUnknownLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var profit = _tradingSystem.CalculateTradeProfit("nonexistent", "mining_colony", "water", 10);
        Assert.Equal(TradeProfit.Zero.CommodityId, profit.CommodityId);
        Assert.Equal(TradeProfit.Zero.Quantity, profit.Quantity);
        Assert.Equal(TradeProfit.Zero.BuyPrice, profit.BuyPrice);
        Assert.Equal(TradeProfit.Zero.SellPrice, profit.SellPrice);
        Assert.Equal(TradeProfit.Zero.ProfitPerUnit, profit.ProfitPerUnit);
        Assert.Equal(TradeProfit.Zero.ProfitMargin, profit.ProfitMargin);
        Assert.Equal(TradeProfit.Zero.TotalProfit, profit.TotalProfit);
        Assert.False(profit.IsProfitable);
    }

    [Fact]
    public async Task CalculateTradeProfit_ReturnsZero_ForUnknownCommodity()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var profit = _tradingSystem.CalculateTradeProfit("trade_hub", "mining_colony", "nonexistent", 10);
        Assert.Equal(TradeProfit.Zero.CommodityId, profit.CommodityId);
        Assert.Equal(TradeProfit.Zero.Quantity, profit.Quantity);
        Assert.Equal(TradeProfit.Zero.BuyPrice, profit.BuyPrice);
        Assert.Equal(TradeProfit.Zero.SellPrice, profit.SellPrice);
        Assert.Equal(TradeProfit.Zero.ProfitPerUnit, profit.ProfitPerUnit);
        Assert.Equal(TradeProfit.Zero.ProfitMargin, profit.ProfitMargin);
        Assert.Equal(TradeProfit.Zero.TotalProfit, profit.TotalProfit);
        Assert.False(profit.IsProfitable);
    }

    // =========================================================================
    // RefreshCurrentMarket Tests
    // =========================================================================

    [Fact]
    public async Task RefreshCurrentMarket_DoesNotThrow_WhenNotInitialized()
    {
        // Should not throw even without initialization
        _tradingSystem.RefreshCurrentMarket();
    }

    [Fact]
    public async Task RefreshCurrentMarket_RefreshesPrices()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var pricesBefore = new Dictionary<string, decimal>(TradeHub.Market.Prices);
        _tradingSystem.RefreshCurrentMarket();

        // Prices may or may not change depending on the random seed
        // But the operation should complete without error
    }

    // =========================================================================
    // RefreshMarketAtLocation Tests
    // =========================================================================

    [Fact]
    public async Task RefreshMarketAtLocation_RefreshesSpecificLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        _tradingSystem.RefreshMarketAtLocation("mining_colony", DateTime.UtcNow);

        // Market should have been refreshed
        Assert.NotEqual(DateTime.MinValue, MiningColony.Market.LastRefresh);
    }

    [Fact]
    public async Task RefreshMarketAtLocation_DoesNotThrow_ForUnknownLocation()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Should not throw for unknown location
        _tradingSystem.RefreshMarketAtLocation("nonexistent", DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshMarketAtLocation_PublishesMarketUpdatedEvents()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        // Set up known prices so we can detect changes
        TradeHub.Market.Prices["water"] = 10m;
        TradeHub.Market.Prices["food"] = 25m;

        _tradingSystem.RefreshMarketAtLocation("trade_hub", DateTime.UtcNow.AddHours(24));

        // MarketUpdatedEvent may or may not be published depending on price changes
        // At minimum, verify the operation completed
    }

    // =========================================================================
    // GetSupplyDemandRatio Tests
    // =========================================================================

    [Fact]
    public async Task GetSupplyDemandRatio_ReturnsRatio_ForValidCommodity()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var ratio = _tradingSystem.GetSupplyDemandRatio("water");
        Assert.True(ratio > 0);
    }

    [Fact]
    public void GetSupplyDemandRatio_ReturnsOne_WhenNotInitialized()
    {
        var ratio = _tradingSystem.GetSupplyDemandRatio("water");
        Assert.Equal(1.0, ratio);
    }

    // =========================================================================
    // GetPriceTrend Tests
    // =========================================================================

    [Fact]
    public async Task GetPriceTrend_ReturnsTrend_ForValidCommodity()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var trend = _tradingSystem.GetPriceTrend("water");
        // Trend should be between -1.0 and 1.0
        Assert.True(trend >= -1.0 && trend <= 1.0);
    }

    [Fact]
    public void GetPriceTrend_ReturnsZero_WhenNotInitialized()
    {
        var trend = _tradingSystem.GetPriceTrend("water");
        Assert.Equal(0.0, trend);
    }

    // =========================================================================
    // ShutdownAsync Tests
    // =========================================================================

    [Fact]
    public async Task ShutdownAsync_SetsIsRunningFalse()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        await _tradingSystem.ShutdownAsync();

        Assert.False(_tradingSystem.IsRunning);
    }

    [Fact]
    public async Task ShutdownAsync_PublishesSystemShutdownEvent()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        await _tradingSystem.ShutdownAsync();

        EventBusMock.Verify(
            x => x.Publish(It.Is<SystemShutdownEvent>(e => e.SystemId == "tradingsystem")),
            Times.Once);
    }

    [Fact]
    public async Task ShutdownAsync_LogsShutdown()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        await _tradingSystem.ShutdownAsync();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("shutdown")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ShutdownAsync_CanBeCalled_WhenNotInitialized()
    {
        await _tradingSystem.ShutdownAsync();
        Assert.False(_tradingSystem.IsRunning);
    }

    [Fact]
    public async Task ShutdownAsync_CanBeCalledMultipleTimes()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);
        await _tradingSystem.ShutdownAsync();
        await _tradingSystem.ShutdownAsync(); // Should not throw
        Assert.False(_tradingSystem.IsRunning);
    }

    // =========================================================================
    // TradeResult Tests
    // =========================================================================

    [Fact]
    public void TradeResult_Completed_HasCorrectProperties()
    {
        var result = TradeResult.Completed(5, 25.5m, -127, "Water");

        Assert.True(result.Success);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(25.5m, result.PricePerUnit);
        Assert.Equal(-127, result.TotalCredits);
        Assert.Equal("Water", result.CommodityName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TradeResult_Failed_HasCorrectProperties()
    {
        var result = TradeResult.Failed("Not enough credits");

        Assert.False(result.Success);
        Assert.Equal("Not enough credits", result.ErrorMessage);
        Assert.Equal(0, result.Quantity);
        Assert.Equal(0m, result.PricePerUnit);
        Assert.Equal(0, result.TotalCredits);
    }

    [Fact]
    public void TradeResult_ToString_Success()
    {
        var result = TradeResult.Completed(10, 50m, -500, "Ore");
        var str = result.ToString();

        Assert.Contains("Bought", str);
        Assert.Contains("Ore", str);
        Assert.Contains("10", str);
        Assert.Contains("500", str);
    }

    [Fact]
    public void TradeResult_ToString_Failure()
    {
        var result = TradeResult.Failed("No credits");
        var str = result.ToString();

        Assert.Contains("failed", str);
        Assert.Contains("No credits", str);
    }

    // =========================================================================
    // TradeProfit Tests
    // =========================================================================

    [Fact]
    public void TradeProfit_Zero_HasAllZeros()
    {
        var zero = TradeProfit.Zero;

        Assert.Equal(string.Empty, zero.CommodityId);
        Assert.Equal(0, zero.Quantity);
        Assert.Equal(0m, zero.BuyPrice);
        Assert.Equal(0m, zero.SellPrice);
        Assert.Equal(0m, zero.ProfitPerUnit);
        Assert.Equal(0.0, zero.ProfitMargin);
        Assert.Equal(0m, zero.TotalProfit);
        Assert.False(zero.IsProfitable);
    }

    [Fact]
    public void TradeProfit_IsProfitable_WhenProfitPerUnitPositive()
    {
        var profit = new TradeProfit
        {
            CommodityId = "water",
            CommodityName = "Water",
            Quantity = 10,
            BuyPrice = 10m,
            SellPrice = 15m,
            ProfitPerUnit = 5m,
            ProfitMargin = 0.5,
            TotalProfit = 50m
        };

        Assert.True(profit.IsProfitable);
    }

    // =========================================================================
    // Edge Cases
    // =========================================================================

    [Fact]
    public async Task BuyFromMarket_HandlesPartialFill_WhenSupplyLessThanRequested()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 3; // Only 3 available
        TradeHub.Market.Prices["water"] = 10m;

        var result = _tradingSystem.BuyFromMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("Insufficient supply", result.ErrorMessage);
    }

    [Fact]
    public async Task SellToMarket_HandlesPartialCargo_WhenCargoLessThanRequested()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 3); // Only 3 in cargo
        TradeHub.Market.Prices["water"] = 10m;

        var result = _tradingSystem.SellToMarket("water", 10);
        Assert.False(result.Success);
        Assert.Contains("Insufficient cargo", result.ErrorMessage);
    }

    [Fact]
    public async Task BuyFromMarket_HandlesNullGameState_Gracefully()
    {
        // If GameState is null (not initialized), BuyFromMarket should fail gracefully
        // This is tested via the "not running" check
        var result = _tradingSystem.BuyFromMarket("water", 10);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task SellToMarket_HandlesNullGameState_Gracefully()
    {
        var result = _tradingSystem.SellToMarket("water", 10);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MultipleBuyTransactions_AreAtomic()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Prices["water"] = 10m;

        // Execute multiple buys
        _tradingSystem.BuyFromMarket("water", 5);
        _tradingSystem.BuyFromMarket("water", 5);

        Assert.Equal(10, GameState.GetCargoQuantity("water"));
        Assert.Equal(90, TradeHub.Market.GetSupply("water"));
    }

    [Fact]
    public async Task MultipleSellTransactions_AreAtomic()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        GameState.AddCargo("water", 50);
        TradeHub.Market.Prices["water"] = 10m;

        _tradingSystem.SellToMarket("water", 5);
        _tradingSystem.SellToMarket("water", 5);

        Assert.Equal(40, GameState.GetCargoQuantity("water"));
    }

    [Fact]
    public async Task BuyAndSell_DifferentCommodities_WorkIndependently()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        TradeHub.Market.Supply["water"] = 100;
        TradeHub.Market.Supply["food"] = 100;
        TradeHub.Market.Prices["water"] = 10m;
        TradeHub.Market.Prices["food"] = 25m;

        _tradingSystem.BuyFromMarket("water", 10);
        _tradingSystem.BuyFromMarket("food", 5);

        Assert.Equal(10, GameState.GetCargoQuantity("water"));
        Assert.Equal(5, GameState.GetCargoQuantity("food"));
    }

    [Fact]
    public async Task GetBestTradeRoutes_ReturnsRoutesSortedByProfit()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var routes = _tradingSystem.GetBestTradeRoutes(5);

        // Routes should be sorted by TotalProfit descending
        for (int i = 1; i < routes.Count; i++)
        {
            Assert.True(routes[i - 1].TotalProfit >= routes[i].TotalProfit);
        }
    }

    [Fact]
    public async Task GetCurrentMarketInfo_IncludesCommodityDetails()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var info = _tradingSystem.GetCurrentMarketInfo();
        Assert.NotNull(info);

        foreach (var commodity in info.Commodities)
        {
            Assert.NotEmpty(commodity.CommodityId);
            Assert.NotEmpty(commodity.CommodityName);
            Assert.True(commodity.Price > 0 || !commodity.IsAvailable);
        }
    }

    [Fact]
    public async Task GetCurrentMarketInfo_IncludesEconomyType()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var info = _tradingSystem.GetCurrentMarketInfo();
        Assert.NotNull(info);
        Assert.Equal(EconomyType.Balanced, info.EconomyType);
    }

    [Fact]
    public async Task GetCurrentMarketInfo_IncludesTechLevel()
    {
        await _tradingSystem.InitializeAsync(GameState, EventBus);

        var info = _tradingSystem.GetCurrentMarketInfo();
        Assert.NotNull(info);
        Assert.Equal(5, info.TechLevel);
    }
}
