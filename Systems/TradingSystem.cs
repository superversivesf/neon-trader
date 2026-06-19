using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;

namespace NeonTrader.Systems;

/// <summary>
/// TradingSystem - handles all market operations: price generation, buy/sell transactions,
/// cargo management integration, profit calculation, and market refresh on turn advance.
/// Priority 10: runs after DataLoader (0) but before most gameplay systems.
/// </summary>
public sealed class TradingSystem : IGameSystem
{
    private readonly ILogger<TradingSystem> _logger;
    private GameState? _gameState;
    private IEventBus? _eventBus;
    private bool _isRunning;

    // Event subscriptions (disposed on shutdown)
    private IDisposable? _timeAdvancedSubscription;
    private IDisposable? _locationChangedSubscription;

    // Lock for atomic buy/sell transactions
    private readonly object _transactionLock = new();

    // Market refresh interval (game time)
    private static readonly TimeSpan MarketRefreshInterval = TimeSpan.FromHours(1);

    // Track last refresh time per market to avoid redundant refreshes
    private readonly Dictionary<string, DateTime> _lastRefreshTimes = new();

    // IGameSystem implementation
    public string SystemId => "tradingsystem";
    public int Priority => 10;
    public bool IsRunning => _isRunning;

    public TradingSystem(ILogger<TradingSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the trading system: generate initial markets, subscribe to events,
    /// and sync current location's market prices to GameState.
    /// </summary>
    public Task InitializeAsync(
        GameState gameState,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        _gameState = gameState;
        _eventBus = eventBus;

        // Subscribe to time advancement for market refresh
        _timeAdvancedSubscription = _eventBus.Subscribe<TimeAdvancedEvent>(OnTimeAdvanced);

        // Subscribe to location changes for market price sync
        _locationChangedSubscription = _eventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);

        // Generate initial markets for all planets
        InitializeAllMarkets();

        // Sync current location's market prices to GameState
        SyncMarketPricesToGameState(gameState.CurrentLocation);

        _isRunning = true;
        _logger.LogInformation(
            "TradingSystem initialized. Markets generated for {Count} locations. Current: {Location}",
            PlanetRegistry.All.Count,
            gameState.CurrentLocation);

        _eventBus.Publish(new SystemInitializedEvent { SystemId = SystemId });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Update - no per-frame work. TradingSystem is event-driven.
    /// </summary>
    public Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shutdown the trading system: dispose subscriptions, clear state.
    /// </summary>
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;

        _timeAdvancedSubscription?.Dispose();
        _locationChangedSubscription?.Dispose();
        _lastRefreshTimes.Clear();

        _logger.LogInformation("TradingSystem shutdown");

        _eventBus?.Publish(new SystemShutdownEvent { SystemId = SystemId });

        return Task.CompletedTask;
    }

    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Player buys a commodity from the current location's market.
    /// Returns the transaction result with quantity bought, total cost, and price per unit.
    /// </summary>
    /// <param name="commodityId">Commodity to buy</param>
    /// <param name="quantity">Desired quantity</param>
    /// <returns>Transaction result</returns>
    public TradeResult BuyFromMarket(string commodityId, int quantity)
    {
        if (!_isRunning || _gameState == null || _eventBus == null)
            return TradeResult.Failed("Trading system is not running");

        if (string.IsNullOrWhiteSpace(commodityId))
            return TradeResult.Failed("Commodity ID cannot be empty");

        if (quantity <= 0)
            return TradeResult.Failed("Quantity must be positive");

        var commodity = CommodityRegistry.Get(commodityId);
        if (commodity == null)
            return TradeResult.Failed($"Unknown commodity: {commodityId}");

        var locationId = _gameState.CurrentLocation;
        var planet = PlanetRegistry.Get(locationId);
        if (planet == null)
            return TradeResult.Failed($"Unknown location: {locationId}");

        if (!planet.HasCommodityExchange)
            return TradeResult.Failed($"No commodity exchange at {planet.Name}");

        var market = planet.Market;

        lock (_transactionLock)
        {
            // Check availability
            var availableSupply = market.GetSupply(commodityId);
            if (availableSupply < quantity)
                return TradeResult.Failed(
                    $"Insufficient supply. Available: {availableSupply}, Requested: {quantity}");

            // Calculate price (what market charges the player)
            var pricePerUnit = PriceGenerator.CalculateSellPrice(market, commodity, quantity);
            var totalCost = (long)(pricePerUnit * quantity);

            // Check credits
            if (_gameState.Credits < totalCost)
                return TradeResult.Failed(
                    $"Insufficient credits. Have: {_gameState.Credits:N0}, Need: {totalCost:N0}");

            // Check cargo space
            var availableSpace = _gameState.GetAvailableCargoSpace();
            if (availableSpace < quantity)
                return TradeResult.Failed(
                    $"Insufficient cargo space. Available: {availableSpace}, Requested: {quantity}");

            // Execute transaction
            var previousCredits = _gameState.Credits;
            var previousCargoQty = _gameState.GetCargoQuantity(commodityId);

            // Deduct credits
            _gameState.Credits -= totalCost;

            // Add cargo
            if (!_gameState.AddCargo(commodityId, quantity))
            {
                // Rollback credits (shouldn't happen since we checked space)
                _gameState.Credits = previousCredits;
                return TradeResult.Failed("Failed to add cargo");
            }

            // Update market supply/demand
            market.Supply.AddOrUpdate(commodityId,
                _ => Math.Max(0, availableSupply - quantity),
                (_, existing) => Math.Max(0, existing - quantity));

            market.Demand.AddOrUpdate(commodityId,
                _ => quantity,
                (_, existing) => existing + quantity);

            // Update GameState market prices snapshot
            _gameState.MarketPrices[commodityId] = pricePerUnit;

            // Update statistics
            _gameState.Statistics.TradesCompleted++;
            _gameState.Statistics.TotalCreditsSpent += totalCost;

            // Publish events
            _eventBus.Publish(new TradeExecutedEvent
            {
                CommodityId = commodityId,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                IsBuy = true,
                TotalCost = totalCost,
                CorrelationId = Guid.NewGuid()
            });

            _eventBus.Publish(new CreditsChangedEvent
            {
                PreviousCredits = previousCredits,
                NewCredits = _gameState.Credits,
                Delta = -totalCost
            });

            _eventBus.Publish(new CargoChangedEvent
            {
                CommodityId = commodityId,
                PreviousQuantity = previousCargoQty,
                NewQuantity = _gameState.GetCargoQuantity(commodityId)
            });

            _logger.LogInformation(
                "BUY: {Commodity} x{Quantity} @{PricePerUnit:F0} = {TotalCost:N0} cr | " +
                "Location: {Location} | Remaining credits: {Credits:N0}",
                commodity.Name, quantity, pricePerUnit, totalCost,
                planet.Name, _gameState.Credits);

            return TradeResult.Completed(quantity, pricePerUnit, totalCost, commodity.Name);
        }
    }

    /// <summary>
    /// Player sells a commodity to the current location's market.
    /// Returns the transaction result with quantity sold, total revenue, and price per unit.
    /// </summary>
    /// <param name="commodityId">Commodity to sell</param>
    /// <param name="quantity">Desired quantity</param>
    /// <returns>Transaction result</returns>
    public TradeResult SellToMarket(string commodityId, int quantity)
    {
        if (!_isRunning || _gameState == null || _eventBus == null)
            return TradeResult.Failed("Trading system is not running");

        if (string.IsNullOrWhiteSpace(commodityId))
            return TradeResult.Failed("Commodity ID cannot be empty");

        if (quantity <= 0)
            return TradeResult.Failed("Quantity must be positive");

        var commodity = CommodityRegistry.Get(commodityId);
        if (commodity == null)
            return TradeResult.Failed($"Unknown commodity: {commodityId}");

        var locationId = _gameState.CurrentLocation;
        var planet = PlanetRegistry.Get(locationId);
        if (planet == null)
            return TradeResult.Failed($"Unknown location: {locationId}");

        if (!planet.HasCommodityExchange)
            return TradeResult.Failed($"No commodity exchange at {planet.Name}");

        var market = planet.Market;

        lock (_transactionLock)
        {
            // Check player has cargo
            var playerCargo = _gameState.GetCargoQuantity(commodityId);
            if (playerCargo < quantity)
                return TradeResult.Failed(
                    $"Insufficient cargo. Have: {playerCargo}, Requested: {quantity}");

            // Calculate price (what market pays the player)
            var pricePerUnit = PriceGenerator.CalculateBuyPrice(market, commodity, quantity);
            var totalRevenue = (long)(pricePerUnit * quantity);

            // Execute transaction
            var previousCredits = _gameState.Credits;
            var previousCargoQty = playerCargo;

            // Remove cargo
            if (!_gameState.RemoveCargo(commodityId, quantity))
            {
                return TradeResult.Failed("Failed to remove cargo");
            }

            // Add credits
            _gameState.Credits += totalRevenue;

            // Update market supply/demand (market gains supply, demand decreases slightly)
            market.Supply.AddOrUpdate(commodityId,
                _ => quantity,
                (_, existing) => existing + quantity);

            market.Demand.AddOrUpdate(commodityId,
                _ => Math.Max(0, -quantity / 2),
                (_, existing) => Math.Max(0, existing - quantity / 2));

            // Update GameState market prices snapshot
            _gameState.MarketPrices[commodityId] = pricePerUnit;

            // Update statistics
            _gameState.Statistics.TradesCompleted++;
            _gameState.Statistics.TotalCreditsEarned += totalRevenue;

            // Publish events
            _eventBus.Publish(new TradeExecutedEvent
            {
                CommodityId = commodityId,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                IsBuy = false,
                TotalCost = totalRevenue,
                CorrelationId = Guid.NewGuid()
            });

            _eventBus.Publish(new CreditsChangedEvent
            {
                PreviousCredits = previousCredits,
                NewCredits = _gameState.Credits,
                Delta = totalRevenue
            });

            _eventBus.Publish(new CargoChangedEvent
            {
                CommodityId = commodityId,
                PreviousQuantity = previousCargoQty,
                NewQuantity = _gameState.GetCargoQuantity(commodityId)
            });

            _logger.LogInformation(
                "SELL: {Commodity} x{Quantity} @{PricePerUnit:F0} = {TotalRevenue:N0} cr | " +
                "Location: {Location} | Total credits: {Credits:N0}",
                commodity.Name, quantity, pricePerUnit, totalRevenue,
                planet.Name, _gameState.Credits);

            return TradeResult.Completed(quantity, pricePerUnit, totalRevenue, commodity.Name);
        }
    }

    /// <summary>
    /// Gets the current market prices for the player's current location.
    /// Returns a dictionary of commodityId -> price.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> GetCurrentMarketPrices()
    {
        if (_gameState == null)
            return new Dictionary<string, decimal>();

        var locationId = _gameState.CurrentLocation;
        var planet = PlanetRegistry.Get(locationId);
        if (planet == null)
            return new Dictionary<string, decimal>();

        var prices = new Dictionary<string, decimal>();
        foreach (var kvp in planet.Market.Prices)
        {
            prices[kvp.Key] = kvp.Value;
        }
        return prices;
    }

    /// <summary>
    /// Gets detailed market information for the current location.
    /// </summary>
    public MarketInfo? GetCurrentMarketInfo()
    {
        if (_gameState == null)
            return null;

        var locationId = _gameState.CurrentLocation;
        var planet = PlanetRegistry.Get(locationId);
        if (planet == null)
            return null;

        return BuildMarketInfo(planet);
    }

    /// <summary>
    /// Gets detailed market information for a specific location.
    /// </summary>
    public MarketInfo? GetMarketInfo(string locationId)
    {
        var planet = PlanetRegistry.Get(locationId);
        if (planet == null)
            return null;

        return BuildMarketInfo(planet);
    }

    /// <summary>
    /// Finds the best trade routes from the current location to all known markets.
    /// </summary>
    /// <param name="maxResults">Maximum number of routes to return</param>
    public List<TradeRoute> GetBestTradeRoutes(int maxResults = 10)
    {
        if (_gameState == null)
            return new List<TradeRoute>();

        var locationId = _gameState.CurrentLocation;
        var sourcePlanet = PlanetRegistry.Get(locationId);
        if (sourcePlanet == null)
            return new List<TradeRoute>();

        var sourceMarket = sourcePlanet.Market;

        // Get all destination markets (discovered planets with commodity exchanges)
        var destinationMarkets = PlanetRegistry.GetDiscovered()
            .Where(p => p.Id != locationId && p.HasCommodityExchange)
            .Select(p => p.Market)
            .ToList();

        if (destinationMarkets.Count == 0)
            return new List<TradeRoute>();

        return PriceGenerator.FindBestTradeRoutes(
            sourceMarket,
            destinationMarkets,
            CommodityRegistry.All,
            maxResults);
    }

    /// <summary>
    /// Calculates profit for a potential trade between two locations.
    /// </summary>
    public TradeProfit CalculateTradeProfit(
        string sourceLocationId,
        string destinationLocationId,
        string commodityId,
        int quantity)
    {
        var sourcePlanet = PlanetRegistry.Get(sourceLocationId);
        var destPlanet = PlanetRegistry.Get(destinationLocationId);
        var commodity = CommodityRegistry.Get(commodityId);

        if (sourcePlanet == null || destPlanet == null || commodity == null)
            return TradeProfit.Zero;

        var (buyPrice, sellPrice, profitPerUnit, profitMargin) =
            PriceGenerator.CalculateTradeProfit(
                sourcePlanet.Market,
                destPlanet.Market,
                commodity,
                quantity);

        return new TradeProfit
        {
            CommodityId = commodityId,
            CommodityName = commodity.Name,
            Quantity = quantity,
            BuyPrice = buyPrice,
            SellPrice = sellPrice,
            ProfitPerUnit = profitPerUnit,
            ProfitMargin = profitMargin,
            TotalProfit = profitPerUnit * quantity
        };
    }

    /// <summary>
    /// Manually refresh the market at the current location.
    /// </summary>
    public void RefreshCurrentMarket()
    {
        if (_gameState == null)
            return;

        var locationId = _gameState.CurrentLocation;
        RefreshMarketAtLocation(locationId, _gameState.GameTime);
    }

    /// <summary>
    /// Manually refresh the market at a specific location.
    /// </summary>
    public void RefreshMarketAtLocation(string locationId, DateTime currentTime)
    {
        var planet = PlanetRegistry.Get(locationId);
        if (planet == null)
            return;

        var market = planet.Market;
        var previousPrices = new Dictionary<string, decimal>(market.Prices);

        PriceGenerator.RefreshMarket(market, CommodityRegistry.All, currentTime);

        _lastRefreshTimes[locationId] = currentTime;

        // Publish MarketUpdatedEvent for each changed commodity
        if (_eventBus != null)
        {
            foreach (var commodity in CommodityRegistry.All)
            {
                var newPrice = market.GetPrice(commodity.Id);
                var previousPrice = previousPrices.TryGetValue(commodity.Id, out var prev)
                    ? prev
                    : newPrice;

                if (previousPrice != newPrice)
                {
                    _eventBus.Publish(new MarketUpdatedEvent
                    {
                        CommodityId = commodity.Id,
                        PreviousPrice = previousPrice,
                        NewPrice = newPrice
                    });
                }
            }
        }

        // Sync to GameState if this is the current location
        if (_gameState != null && locationId == _gameState.CurrentLocation)
        {
            SyncMarketPricesToGameState(locationId);
        }

        _logger.LogDebug("Market refreshed at {Location} ({Time})",
            planet.Name, currentTime);
    }

    /// <summary>
    /// Gets the supply/demand ratio for a commodity at the current location.
    /// Higher values mean oversupply (lower prices), lower values mean undersupply (higher prices).
    /// </summary>
    public double GetSupplyDemandRatio(string commodityId)
    {
        if (_gameState == null)
            return 1.0;

        var planet = PlanetRegistry.Get(_gameState.CurrentLocation);
        if (planet == null)
            return 1.0;

        return planet.Market.GetSupplyDemandRatio(commodityId);
    }

    /// <summary>
    /// Gets the price trend for a commodity at the current location.
    /// Returns -1.0 (falling) to 1.0 (rising).
    /// </summary>
    public double GetPriceTrend(string commodityId)
    {
        if (_gameState == null)
            return 0.0;

        var planet = PlanetRegistry.Get(_gameState.CurrentLocation);
        if (planet == null)
            return 0.0;

        return planet.Market.GetPriceTrend(commodityId);
    }

    // ─── Event Handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Handle time advancement: refresh markets that need it.
    /// </summary>
    private void OnTimeAdvanced(TimeAdvancedEvent evt)
    {
        if (!_isRunning || _gameState == null)
            return;

        _logger.LogDebug("Time advanced to {Time}, checking market refreshes...", evt.NewTime);

        // Refresh all discovered planet markets that need it
        foreach (var planet in PlanetRegistry.GetDiscovered())
        {
            if (!planet.HasCommodityExchange)
                continue;

            var market = planet.Market;
            if (market.NeedsRefresh(evt.NewTime, MarketRefreshInterval))
            {
                RefreshMarketAtLocation(planet.Id, evt.NewTime);
            }
        }

        // Always sync current location prices to GameState after time advance
        SyncMarketPricesToGameState(_gameState.CurrentLocation);
    }

    /// <summary>
    /// Handle location change: ensure market is initialized and sync prices.
    /// </summary>
    private void OnLocationChanged(LocationChangedEvent evt)
    {
        if (!_isRunning || _gameState == null)
            return;

        _logger.LogInformation(
            "Location changed: {Previous} -> {New}. Syncing market prices...",
            evt.PreviousLocation, evt.NewLocation);

        // Ensure the new location's market is initialized
        var planet = PlanetRegistry.Get(evt.NewLocation);
        if (planet != null && planet.HasCommodityExchange)
        {
            EnsureMarketInitialized(planet);
        }

        // Sync market prices to GameState
        SyncMarketPricesToGameState(evt.NewLocation);
    }

    // ─── Internal Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Generate initial markets for all planets in the registry.
    /// </summary>
    private void InitializeAllMarkets()
    {
        var commodities = CommodityRegistry.All;
        var initializedCount = 0;

        foreach (var planet in PlanetRegistry.All)
        {
            if (!planet.HasCommodityExchange)
                continue;

            EnsureMarketInitialized(planet);
            initializedCount++;
        }

        _logger.LogInformation(
            "Initial markets generated for {Count} locations with commodity exchanges",
            initializedCount);
    }

    /// <summary>
    /// Ensure a planet's market is initialized with prices.
    /// Idempotent - skips if already initialized.
    /// </summary>
    private void EnsureMarketInitialized(Planet planet)
    {
        var market = planet.Market;

        // Set market metadata from planet if not already set
        if (string.IsNullOrEmpty(market.MarketId))
            market.MarketId = planet.Id;

        if (string.IsNullOrEmpty(market.Name))
            market.Name = $"{planet.Name} Market";

        if (market.EconomyType == EconomyType.Balanced && planet.EconomyType != EconomyType.Balanced)
            market.EconomyType = planet.EconomyType;

        if (market.TechLevel == 5 && planet.TechLevel != 5)
            market.TechLevel = planet.TechLevel;

        if (string.IsNullOrEmpty(market.FactionId))
            market.FactionId = planet.FactionId;

        market.HasBlackMarket = planet.HasBlackMarket;

        // Generate seed from planet ID for deterministic prices
        if (market.Seed == 0)
            market.Seed = planet.Id.GetHashCode();

        // Generate initial prices if market is empty
        if (market.Prices.Count == 0)
        {
            PriceGenerator.GenerateInitialMarket(market, CommodityRegistry.All);
            _lastRefreshTimes[planet.Id] = DateTime.UtcNow;
            _logger.LogDebug("Initial market generated for {Planet} ({Economy})",
                planet.Name, planet.EconomyType);
        }
    }

    /// <summary>
    /// Sync a location's market prices into GameState.MarketPrices.
    /// </summary>
    private void SyncMarketPricesToGameState(string locationId)
    {
        if (_gameState == null)
            return;

        var planet = PlanetRegistry.Get(locationId);
        if (planet == null || !planet.HasCommodityExchange)
        {
            _gameState.MarketPrices.Clear();
            return;
        }

        var market = planet.Market;

        // Clear and repopulate
        _gameState.MarketPrices.Clear();
        foreach (var kvp in market.Prices)
        {
            _gameState.MarketPrices[kvp.Key] = kvp.Value;
        }

        _logger.LogDebug("Market prices synced for {Location} ({Count} commodities)",
            planet.Name, market.Prices.Count);
    }

    /// <summary>
    /// Build a MarketInfo DTO from a planet's market data.
    /// </summary>
    private MarketInfo BuildMarketInfo(Planet planet)
    {
        var market = planet.Market;

        var commodities = new List<CommodityMarketInfo>();
        foreach (var commodity in CommodityRegistry.All)
        {
            var price = market.GetPrice(commodity.Id);
            if (price <= 0)
                continue;

            commodities.Add(new CommodityMarketInfo
            {
                CommodityId = commodity.Id,
                CommodityName = commodity.Name,
                Category = commodity.Category,
                Price = price,
                Supply = market.GetSupply(commodity.Id),
                Demand = market.GetDemand(commodity.Id),
                Trend = market.GetPriceTrend(commodity.Id),
                IsAvailable = market.IsAvailable(commodity.Id)
            });
        }

        return new MarketInfo
        {
            LocationId = planet.Id,
            LocationName = planet.Name,
            EconomyType = market.EconomyType,
            TechLevel = market.TechLevel,
            FactionId = market.FactionId,
            HasBlackMarket = market.HasBlackMarket,
            LastRefresh = market.LastRefresh,
            Commodities = commodities,
            ActiveEvents = market.ActiveEvents
                .Where(e => e.IsActive(_gameState?.GameTime ?? DateTime.UtcNow))
                .Select(e => new ActiveEventInfo
                {
                    Type = e.Type,
                    Description = e.Description,
                    Severity = e.Severity,
                    EndTime = e.EndTime,
                    AffectedCommodityId = e.CommodityId,
                    AffectedCategory = e.Category
                })
                .ToList()
        };
    }
}

// ─── Public DTOs / Result Types ──────────────────────────────────────────

/// <summary>
/// Result of a buy or sell transaction.
/// </summary>
public sealed class TradeResult
{
    /// <summary>Whether the trade was successful</summary>
    public bool Success { get; init; }

    /// <summary>Error message if trade failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Actual quantity traded</summary>
    public int Quantity { get; init; }

    /// <summary>Price per unit in credits</summary>
    public decimal PricePerUnit { get; init; }

    /// <summary>Total credits exchanged (positive = player received, negative = player paid)</summary>
    public long TotalCredits { get; init; }

    /// <summary>Name of the commodity traded</summary>
    public string CommodityName { get; init; } = string.Empty;

    public static TradeResult Completed(int quantity, decimal pricePerUnit, long totalCredits, string commodityName)
    {
        return new TradeResult
        {
            Success = true,
            Quantity = quantity,
            PricePerUnit = pricePerUnit,
            TotalCredits = totalCredits,
            CommodityName = commodityName
        };
    }

    public static TradeResult Failed(string errorMessage)
    {
        return new TradeResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    public override string ToString()
    {
        if (!Success)
            return $"Trade failed: {ErrorMessage}";

        var direction = TotalCredits < 0 ? "Bought" : "Sold";
        return $"{direction} {CommodityName} x{Quantity} @{PricePerUnit:F0} = {Math.Abs(TotalCredits):N0} cr";
    }
}

/// <summary>
/// Detailed market information for a location.
/// </summary>
public sealed class MarketInfo
{
    public string LocationId { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public EconomyType EconomyType { get; init; }
    public int TechLevel { get; init; }
    public string FactionId { get; init; } = string.Empty;
    public bool HasBlackMarket { get; init; }
    public DateTime LastRefresh { get; init; }
    public List<CommodityMarketInfo> Commodities { get; init; } = new();
    public List<ActiveEventInfo> ActiveEvents { get; init; } = new();
}

/// <summary>
/// Market information for a single commodity at a location.
/// </summary>
public sealed class CommodityMarketInfo
{
    public string CommodityId { get; init; } = string.Empty;
    public string CommodityName { get; init; } = string.Empty;
    public CommodityCategory Category { get; init; }
    public decimal Price { get; init; }
    public int Supply { get; init; }
    public int Demand { get; init; }
    public double Trend { get; init; }
    public bool IsAvailable { get; init; }

    /// <summary>Supply/demand ratio (higher = oversupplied)</summary>
    public double SupplyDemandRatio =>
        Demand == 0 ? (Supply > 0 ? double.MaxValue : 1.0) : (double)Supply / Demand;
}

/// <summary>
/// Information about an active market event.
/// </summary>
public sealed class ActiveEventInfo
{
    public MarketEventType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public int Severity { get; init; }
    public DateTime EndTime { get; init; }
    public string AffectedCommodityId { get; init; } = string.Empty;
    public CommodityCategory? AffectedCategory { get; init; }
}

/// <summary>
/// Profit calculation for a potential trade.
/// </summary>
public sealed class TradeProfit
{
    public string CommodityId { get; init; } = string.Empty;
    public string CommodityName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal BuyPrice { get; init; }
    public decimal SellPrice { get; init; }
    public decimal ProfitPerUnit { get; init; }
    public double ProfitMargin { get; init; }
    public decimal TotalProfit { get; init; }

    public static TradeProfit Zero => new();

    public bool IsProfitable => ProfitPerUnit > 0;

    public override string ToString()
    {
        return $"{CommodityName} x{Quantity}: Buy @{BuyPrice:F0} -> Sell @{SellPrice:F0} " +
               $"= {ProfitPerUnit:F0}/unit ({ProfitMargin:P1}), {TotalProfit:F0} total";
    }
}
