using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Models;
using NeonTrader.Systems;
using Xunit;

namespace NeonTrader.Tests.Systems;

/// <summary>
/// Tests for NavigationSystem: jump routes, fuel consumption, travel time,
/// encounter generation, travel state machine, and edge cases.
/// Uses a real EventBus so Subscribe works properly; published events
/// are captured via test-side subscriptions for verification.
/// </summary>
[Collection("Sequential")]
public class NavigationSystemTests : IDisposable
{
    private readonly NavigationSystem _nav;
    private readonly Mock<ILogger<NavigationSystem>> _loggerMock;
    private readonly GameState _gameState;
    private readonly EventBus _eventBus;

    // Test planet data
    private readonly Planet _originPlanet;
    private readonly Planet _destPlanet;
    private readonly Planet _undiscoveredPlanet;
    private readonly Ship _testShip;

    // Captured events for verification
    private readonly List<GameEvent> _publishedEvents = new();
    private Action<TimeAdvancedEvent>? _timeAdvancedHandler;

    public NavigationSystemTests()
    {
        _loggerMock = new Mock<ILogger<NavigationSystem>>();
        _nav = new NavigationSystem(_loggerMock.Object);

        _gameState = new GameState
        {
            PlayerName = "TestPilot",
            Credits = 50000,
            Health = 100,
            MaxHealth = 100,
            CurrentLocation = "Test Station Alpha",
            PreviousLocation = "",
            GameTime = new DateTime(2087, 6, 15, 12, 0, 0),
            ShipId = "test_ship",
            CargoCapacity = 100,
            FuelCapacity = 200,
            CurrentFuel = 200
        };

        _eventBus = new EventBus();

        // Subscribe to all events for verification
        _eventBus.SubscribeAll(evt => _publishedEvents.Add(evt));

        // Set up test planets
        _originPlanet = new Planet
        {
            Id = "Test Station Alpha",
            Name = "Test Station Alpha",
            Type = PlanetType.Station,
            SystemName = "Test System",
            FactionId = "test_faction",
            SecurityLevel = 5,
            TravelDanger = 20,
            HasShipyard = true,
            HasOutfitter = true,
            HasCommodityExchange = true,
            HasMissionBoard = true,
            IsDiscovered = true
        };

        _destPlanet = new Planet
        {
            Id = "dest_planet",
            Name = "Destination Beta",
            Type = PlanetType.Terrestrial,
            SystemName = "Beta System",
            FactionId = "beta_faction",
            SecurityLevel = 7,
            TravelDanger = 15,
            HasShipyard = false,
            HasOutfitter = true,
            HasCommodityExchange = true,
            HasMissionBoard = true,
            IsDiscovered = true
        };

        _undiscoveredPlanet = new Planet
        {
            Id = "undiscovered_gamma",
            Name = "Gamma Unknown",
            Type = PlanetType.Mining,
            SystemName = "Gamma System",
            FactionId = "gamma_faction",
            SecurityLevel = 2,
            TravelDanger = 60,
            IsDiscovered = false
        };

        _originPlanet.AddConnection(_destPlanet.Id, 12.5, 15);
        _originPlanet.AddConnection(_undiscoveredPlanet.Id, 8.0, 10);
        _destPlanet.AddConnection(_originPlanet.Id, 12.5, 15);

        _testShip = new Ship
        {
            Id = "test_ship",
            Name = "Test Runner",
            ShipClassId = "test_class",
            CurrentHull = 100,
            MaxHull = 100,
            CurrentShield = 50,
            MaxShield = 50,
            ShieldRechargeRate = 5.0,
            FuelCapacity = 200,
            CurrentFuel = 200,
            FuelConsumption = 1.2,
            MaxSpeed = 150.0,
            CargoCapacity = 100
        };

        PlanetRegistry.Clear();
        PlanetRegistry.Register(_originPlanet);
        PlanetRegistry.Register(_destPlanet);
        PlanetRegistry.Register(_undiscoveredPlanet);

        ShipRegistry.Clear();
        ShipRegistry.Register(_testShip);

        _nav.InitializeAsync(_gameState, _eventBus).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        PlanetRegistry.Clear();
        ShipRegistry.Clear();
        _eventBus.Dispose();
    }

    // Helper to publish time advancement
    private void AdvanceTime(double hours)
    {
        _eventBus.Publish(new TimeAdvancedEvent
        {
            NewTime = _gameState.GameTime.AddHours(hours),
            DeltaTime = TimeSpan.FromHours(hours)
        });
    }

    // Helper to get events of a specific type
    private List<T> GetPublishedEvents<T>() where T : GameEvent
        => _publishedEvents.OfType<T>().ToList();

    // =========================================================================
    // Initialization & Lifecycle
    // =========================================================================

    [Fact]
    public void InitializeAsync_SetsIsRunning()
    {
        Assert.True(_nav.IsRunning);
        Assert.Equal("NavigationSystem", _nav.SystemId);
        Assert.Equal(20, _nav.Priority);
    }

    [Fact]
    public async Task ShutdownAsync_CancelsTravel()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        Assert.True(_nav.IsTraveling);

        await _nav.ShutdownAsync();

        Assert.False(_nav.IsRunning);
        Assert.False(_nav.IsTraveling);

        var interrupted = GetPublishedEvents<TravelInterruptedEvent>();
        Assert.NotEmpty(interrupted);
    }

    [Fact]
    public void UpdateAsync_ReturnsCompletedTask()
    {
        var task = _nav.UpdateAsync(0.016f);
        Assert.True(task.IsCompletedSuccessfully);
    }

    // =========================================================================
    // GetAvailableDestinations
    // =========================================================================

    [Fact]
    public void GetAvailableDestinations_ReturnsConnectedDiscoveredLocations()
    {
        var destinations = _nav.GetAvailableDestinations();

        Assert.Single(destinations);
        Assert.Equal(_destPlanet.Id, destinations[0].PlanetId);
        Assert.Equal(_destPlanet.Name, destinations[0].PlanetName);
        Assert.Equal(12.5, destinations[0].DistanceLY);
        Assert.True(destinations[0].FuelCost > 0);
        Assert.True(destinations[0].TravelTimeHours > 0);
    }

    [Fact]
    public void GetAvailableDestinations_ExcludesUndiscoveredByDefault()
    {
        var destinations = _nav.GetAvailableDestinations(includeUndiscovered: false);
        Assert.DoesNotContain(destinations, d => d.PlanetId == _undiscoveredPlanet.Id);
    }

    [Fact]
    public void GetAvailableDestinations_IncludesUndiscoveredWhenRequested()
    {
        var destinations = _nav.GetAvailableDestinations(includeUndiscovered: true);
        Assert.Contains(destinations, d => d.PlanetId == _undiscoveredPlanet.Id);
        Assert.Equal(2, destinations.Count);
    }

    [Fact]
    public void GetAvailableDestinations_SortsByDistance()
    {
        var destinations = _nav.GetAvailableDestinations(includeUndiscovered: true);
        Assert.Equal(_undiscoveredPlanet.Id, destinations[0].PlanetId);
        Assert.Equal(_destPlanet.Id, destinations[1].PlanetId);
    }

    [Fact]
    public void GetAvailableDestinations_ReturnsEmpty_WhenCurrentLocationNotFound()
    {
        _gameState.CurrentLocation = "nonexistent_planet";
        var destinations = _nav.GetAvailableDestinations();
        Assert.Empty(destinations);
    }

    [Fact]
    public void GetAvailableDestinations_IncludesAllDestinationProperties()
    {
        var destinations = _nav.GetAvailableDestinations();
        var dest = destinations[0];

        Assert.Equal(_destPlanet.Id, dest.PlanetId);
        Assert.Equal(_destPlanet.Name, dest.PlanetName);
        Assert.Equal(_destPlanet.Type, dest.PlanetType);
        Assert.Equal(_destPlanet.SystemName, dest.SystemName);
        Assert.Equal(_destPlanet.TravelDanger, dest.DangerLevel);
        Assert.Equal(_destPlanet.SecurityLevel, dest.SecurityLevel);
        Assert.Equal(_destPlanet.FactionId, dest.FactionId);
        Assert.Equal(_destPlanet.HasShipyard, dest.HasShipyard);
        Assert.Equal(_destPlanet.HasOutfitter, dest.HasOutfitter);
        Assert.Equal(_destPlanet.HasCommodityExchange, dest.HasMarket);
        Assert.True(dest.IsDiscovered);
    }

    // =========================================================================
    // GetJumpInfo
    // =========================================================================

    [Fact]
    public void GetJumpInfo_ReturnsDetailedInfo_ForValidDestination()
    {
        var info = _nav.GetJumpInfo(_destPlanet.Id);

        Assert.NotNull(info);
        Assert.Equal(_gameState.CurrentLocation, info!.OriginId);
        Assert.Equal(_destPlanet.Id, info.DestinationId);
        Assert.Equal(12.5, info.DistanceLY);
        Assert.True(info.FuelCost > 0);
        Assert.Equal(_gameState.CurrentFuel, info.FuelAvailable);
        Assert.True(info.TravelTimeHours > 0);
        Assert.Equal(_destPlanet.TravelDanger, info.DangerLevel);
        Assert.Equal(_destPlanet.SecurityLevel, info.SecurityLevel);
        Assert.Equal(_destPlanet.FactionId, info.FactionId);
        Assert.True(info.CanJump);
        Assert.Null(info.BlockReason);
    }

    [Fact]
    public void GetJumpInfo_ReturnsNull_ForInvalidDestination()
    {
        var info = _nav.GetJumpInfo("nonexistent");
        Assert.Null(info);
    }

    [Fact]
    public void GetJumpInfo_ReturnsNull_ForUnconnectedDestination()
    {
        var unconnectedPlanet = new Planet
        {
            Id = "unconnected",
            Name = "Unconnected",
            IsDiscovered = true
        };
        PlanetRegistry.Register(unconnectedPlanet);

        var info = _nav.GetJumpInfo("unconnected");
        Assert.Null(info);
    }

    [Fact]
    public void GetJumpInfo_ShowsBlockReason_WhenInsufficientFuel()
    {
        _gameState.CurrentFuel = 1;
        var info = _nav.GetJumpInfo(_destPlanet.Id);

        Assert.NotNull(info);
        Assert.False(info!.CanJump);
        Assert.NotNull(info.BlockReason);
        Assert.Contains("Insufficient fuel", info.BlockReason);
    }

    [Fact]
    public void GetJumpInfo_ShowsBlockReason_WhenAlreadyTraveling()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var info = _nav.GetJumpInfo(_destPlanet.Id);
        Assert.NotNull(info);
        Assert.False(info!.CanJump);
        Assert.Equal("Already in transit", info.BlockReason);
    }

    // =========================================================================
    // CanJumpTo
    // =========================================================================

    [Fact]
    public void CanJumpTo_ReturnsTrue_WhenAllConditionsMet()
    {
        Assert.True(_nav.CanJumpTo(_destPlanet.Id));
    }

    [Fact]
    public void CanJumpTo_ReturnsFalse_WhenAlreadyTraveling()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        Assert.False(_nav.CanJumpTo(_destPlanet.Id));
    }

    [Fact]
    public void CanJumpTo_ReturnsFalse_WhenInsufficientFuel()
    {
        _gameState.CurrentFuel = 0;
        Assert.False(_nav.CanJumpTo(_destPlanet.Id));
    }

    [Fact]
    public void CanJumpTo_ReturnsFalse_WhenNotConnected()
    {
        Assert.False(_nav.CanJumpTo("nonexistent"));
    }

    [Fact]
    public void CanJumpTo_ReturnsFalse_WhenCurrentLocationUnknown()
    {
        _gameState.CurrentLocation = "nonexistent";
        Assert.False(_nav.CanJumpTo(_destPlanet.Id));
    }

    // =========================================================================
    // StartJump
    // =========================================================================

    [Fact]
    public void StartJump_ConsumesFuel_AndSetsTravelState()
    {
        SetupJumpPrerequisites();
        var initialFuel = _gameState.CurrentFuel;

        var result = _nav.StartJump(_destPlanet.Id);

        Assert.True(result);
        Assert.True(_nav.IsTraveling);
        Assert.Equal(_destPlanet.Id, _nav.CurrentDestination);
        Assert.Equal(_gameState.CurrentLocation, _nav.CurrentOrigin);
        Assert.True(_gameState.CurrentFuel < initialFuel);
        Assert.Equal(0.0, _nav.TravelProgress);
        Assert.True(_nav.TravelTimeRemainingHours > 0);
    }

    [Fact]
    public void StartJump_PublishesTravelStartedEvent()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var started = GetPublishedEvents<TravelStartedEvent>();
        Assert.NotEmpty(started);
        Assert.Equal(_destPlanet.Id, started[0].DestinationId);
        Assert.True(started[0].DistanceLY > 0);
        Assert.True(started[0].FuelCost > 0);
    }

    [Fact]
    public void StartJump_ReturnsFalse_WhenCannotJump()
    {
        _gameState.CurrentFuel = 0;
        var result = _nav.StartJump(_destPlanet.Id);
        Assert.False(result);
        Assert.False(_nav.IsTraveling);
    }

    [Fact]
    public void StartJump_ReturnsFalse_WhenAlreadyTraveling()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        var result = _nav.StartJump(_destPlanet.Id);
        Assert.False(result);
    }

    [Fact]
    public void StartJump_SetsCorrectJumpDistance()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        Assert.Equal(12.5, _nav.CurrentJumpDistance);
    }

    [Fact]
    public void StartJump_SetsCorrectFuelCost()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        Assert.Equal(15, _nav.CurrentJumpFuelCost);
    }

    // =========================================================================
    // CancelJump
    // =========================================================================

    [Fact]
    public void CancelJump_ResetsTravelState()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var result = _nav.CancelJump();

        Assert.True(result);
        Assert.False(_nav.IsTraveling);
        Assert.Empty(_nav.CurrentDestination);
        Assert.Empty(_nav.CurrentOrigin);
        Assert.Equal(0.0, _nav.TravelProgress);
        Assert.Equal(0.0, _nav.TravelTimeRemainingHours);
    }

    [Fact]
    public void CancelJump_PublishesTravelInterruptedEvent()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        _nav.CancelJump();

        var interrupted = GetPublishedEvents<TravelInterruptedEvent>();
        Assert.NotEmpty(interrupted);
        Assert.Equal("Player canceled", interrupted[0].Reason);
    }

    [Fact]
    public void CancelJump_ReturnsFalse_WhenNotTraveling()
    {
        var result = _nav.CancelJump();
        Assert.False(result);
    }

    // =========================================================================
    // CalculateFuelCost
    // =========================================================================

    [Fact]
    public void CalculateFuelCost_UsesShipFuelConsumption()
    {
        var cost = _nav.CalculateFuelCost(10.0, _testShip);
        Assert.Equal(12, cost);
    }

    [Fact]
    public void CalculateFuelCost_CeilingRoundsUp()
    {
        var cost = _nav.CalculateFuelCost(10.1, _testShip);
        Assert.Equal(13, cost);
    }

    [Fact]
    public void CalculateFuelCost_FallsBackToDefault_WhenNoShip()
    {
        ShipRegistry.Clear();
        var cost = _nav.CalculateFuelCost(10.0);
        Assert.Equal(10, cost);
    }

    [Fact]
    public void CalculateFuelCost_ZeroDistance_ReturnsZero()
    {
        var cost = _nav.CalculateFuelCost(0.0, _testShip);
        Assert.Equal(0, cost);
    }

    // =========================================================================
    // CalculateTravelTime
    // =========================================================================

    [Fact]
    public void CalculateTravelTime_UsesShipMaxSpeed()
    {
        var time = _nav.CalculateTravelTime(150.0, _testShip);
        Assert.Equal(1.0, time);
    }

    [Fact]
    public void CalculateTravelTime_MinimumHalfHour()
    {
        var time = _nav.CalculateTravelTime(0.01, _testShip);
        Assert.Equal(0.5, time);
    }

    [Fact]
    public void CalculateTravelTime_FallsBackToDefaultSpeed_WhenNoShip()
    {
        ShipRegistry.Clear();
        var time = _nav.CalculateTravelTime(100.0);
        Assert.Equal(1.0, time);
    }

    [Fact]
    public void CalculateTravelTime_HandlesZeroSpeed()
    {
        var zeroSpeedShip = new Ship
        {
            Id = "zero_speed",
            ShipClassId = "zero_class",
            MaxSpeed = 0,
            FuelConsumption = 1.0
        };
        ShipRegistry.Register(zeroSpeedShip);

        var time = _nav.CalculateTravelTime(100.0, zeroSpeedShip);
        Assert.Equal(1.0, time);
    }

    // =========================================================================
    // GetEncounterRisk
    // =========================================================================

    [Fact]
    public void GetEncounterRisk_CalculatesFromDangerAndSecurity()
    {
        var risk = _nav.GetEncounterRisk(_destPlanet.Id);
        Assert.Equal(0.225, risk, precision: 3);
    }

    [Fact]
    public void GetEncounterRisk_HighDangerLowSecurity_GivesHighRisk()
    {
        var risk = _nav.GetEncounterRisk(_undiscoveredPlanet.Id);
        Assert.Equal(0.7, risk, precision: 3);
    }

    [Fact]
    public void GetEncounterRisk_ReturnsDefault_ForUnknownDestination()
    {
        var risk = _nav.GetEncounterRisk("nonexistent");
        Assert.Equal(0.5, risk);
    }

    [Fact]
    public void GetEncounterRisk_ClampedToZeroToOne()
    {
        var extremePlanet = new Planet
        {
            Id = "extreme",
            TravelDanger = 200,
            SecurityLevel = 0
        };
        PlanetRegistry.Register(extremePlanet);

        var risk = _nav.GetEncounterRisk("extreme");
        Assert.True(risk >= 0.0 && risk <= 1.0);
    }

    // =========================================================================
    // Travel Progress via TimeAdvancedEvent
    // =========================================================================

    [Fact]
    public void TimeAdvancedEvent_AdvancesTravelProgress()
    {
        SetupJumpPrerequisites();
        var started = _nav.StartJump(_destPlanet.Id);
        Assert.True(started, "StartJump should succeed");

        // Travel time is 0.5 hours (minimum), advance 0.2 hours
        AdvanceTime(0.2);

        Assert.True(_nav.TravelProgress > 0);
        Assert.True(_nav.TravelTimeRemainingHours < _nav.CalculateTravelTime(12.5, _testShip));
    }

    [Fact]
    public void TimeAdvancedEvent_CompletesTravel_WhenElapsedExceedsTotal()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var totalTime = _nav.CalculateTravelTime(12.5, _testShip);
        AdvanceTime(totalTime + 1);

        Assert.False(_nav.IsTraveling);
        Assert.Equal(_destPlanet.Id, _gameState.CurrentLocation);
        Assert.Equal("Test Station Alpha", _gameState.PreviousLocation);
    }

    [Fact]
    public void CompleteTravel_PublishesLocationChangedEvent()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var totalTime = _nav.CalculateTravelTime(12.5, _testShip);
        AdvanceTime(totalTime + 1);

        var locationChanged = GetPublishedEvents<LocationChangedEvent>();
        Assert.NotEmpty(locationChanged);
        Assert.Equal("Test Station Alpha", locationChanged[0].PreviousLocation);
        Assert.Equal(_destPlanet.Id, locationChanged[0].NewLocation);
    }

    [Fact]
    public void CompleteTravel_PublishesTravelCompletedEvent()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var totalTime = _nav.CalculateTravelTime(12.5, _testShip);
        AdvanceTime(totalTime + 1);

        var completed = GetPublishedEvents<TravelCompletedEvent>();
        Assert.NotEmpty(completed);
        Assert.Equal(_destPlanet.Id, completed[0].DestinationId);
        Assert.Equal(12.5, completed[0].DistanceLY);
    }

    [Fact]
    public void CompleteTravel_PublishesRefreshUIEvent()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var totalTime = _nav.CalculateTravelTime(12.5, _testShip);
        AdvanceTime(totalTime + 1);

        var refresh = GetPublishedEvents<RefreshUIEvent>();
        Assert.NotEmpty(refresh);
    }

    [Fact]
    public void CompleteTravel_UpdatesStatistics()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        var initialDistance = _gameState.Statistics.DistanceTraveled;
        var totalTime = _nav.CalculateTravelTime(12.5, _testShip);
        AdvanceTime(totalTime + 1);

        Assert.True(_gameState.Statistics.DistanceTraveled > initialDistance);
    }

    [Fact]
    public void CompleteTravel_DiscoversUndiscoveredDestination()
    {
        _undiscoveredPlanet.IsDiscovered = false;
        _gameState.CurrentFuel = 200;
        _nav.StartJump(_undiscoveredPlanet.Id);

        var totalTime = _nav.CalculateTravelTime(8.0, _testShip);
        AdvanceTime(totalTime + 1);

        Assert.True(_undiscoveredPlanet.IsDiscovered);
    }

    // =========================================================================
    // Travel Progress Properties
    // =========================================================================

    [Fact]
    public void TravelProgress_ReturnsZero_WhenNotTraveling()
    {
        Assert.Equal(0.0, _nav.TravelProgress);
    }

    [Fact]
    public void TravelTimeRemainingHours_ReturnsZero_WhenNotTraveling()
    {
        Assert.Equal(0.0, _nav.TravelTimeRemainingHours);
    }

    [Fact]
    public void CurrentJumpDistance_ReturnsZero_WhenNotTraveling()
    {
        Assert.Equal(0.0, _nav.CurrentJumpDistance);
    }

    [Fact]
    public void CurrentJumpFuelCost_ReturnsZero_WhenNotTraveling()
    {
        Assert.Equal(0, _nav.CurrentJumpFuelCost);
    }

    [Fact]
    public void CurrentDestination_ReturnsEmpty_WhenNotTraveling()
    {
        Assert.Empty(_nav.CurrentDestination);
    }

    [Fact]
    public void CurrentOrigin_ReturnsEmpty_WhenNotTraveling()
    {
        Assert.Empty(_nav.CurrentOrigin);
    }

    // =========================================================================
    // Encounter Generation (via TimeAdvancedEvent)
    // =========================================================================

    [Fact]
    public void TimeAdvancedEvent_TriggersEncounterCheck_EachHour()
    {
        SetupJumpPrerequisites();
        var started = _nav.StartJump(_destPlanet.Id);
        Assert.True(started, "StartJump should succeed");

        // Advance 0.3 hours (less than 0.5 minimum travel time)
        AdvanceTime(0.3);

        Assert.True(_nav.TravelProgress > 0);
    }

    [Fact]
    public void TimeAdvancedEvent_AccumulatesEncounterChecks_ForMultipleHours()
    {
        SetupJumpPrerequisites();
        var started = _nav.StartJump(_destPlanet.Id);
        Assert.True(started, "StartJump should succeed");

        // Advance 0.4 hours (less than 0.5 minimum travel time)
        AdvanceTime(0.4);

        Assert.True(_nav.TravelProgress > 0);
    }

    [Fact]
    public void TimeAdvancedEvent_DoesNotTriggerEncounters_WhenNotTraveling()
    {
        AdvanceTime(1);

        var encounters = GetPublishedEvents<TravelEncounterEvent>();
        Assert.Empty(encounters);
    }

    // =========================================================================
    // Edge Cases
    // =========================================================================

    [Fact]
    public void StartJump_WithExactFuel_Works()
    {
        var cost = _nav.CalculateFuelCost(12.5, _testShip);
        _gameState.CurrentFuel = cost;

        var result = _nav.StartJump(_destPlanet.Id);
        Assert.True(result);
        Assert.Equal(0, _gameState.CurrentFuel);
    }

    [Fact]
    public void StartJump_WithOneFuelShort_Fails()
    {
        var cost = _nav.CalculateFuelCost(12.5, _testShip);
        _gameState.CurrentFuel = cost - 1;

        var result = _nav.StartJump(_destPlanet.Id);
        Assert.False(result);
    }

    [Fact]
    public void GetAvailableDestinations_HandlesNullShip()
    {
        ShipRegistry.Clear();
        var destinations = _nav.GetAvailableDestinations();
        Assert.Single(destinations);
    }

    [Fact]
    public void TravelProgress_ClampedToZeroToOne()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);

        AdvanceTime(1000);

        Assert.Equal(0.0, _nav.TravelProgress);
    }

    [Fact]
    public void MultipleJumps_WorkSequentially()
    {
        SetupJumpPrerequisites();
        _nav.StartJump(_destPlanet.Id);
        Assert.True(_nav.IsTraveling);

        var totalTime = _nav.CalculateTravelTime(12.5, _testShip);
        AdvanceTime(totalTime + 1);

        Assert.False(_nav.IsTraveling);
        Assert.Equal(_destPlanet.Id, _gameState.CurrentLocation);

        _gameState.CurrentFuel = 200;
        Assert.True(_nav.CanJumpTo(_originPlanet.Id));

        var result = _nav.StartJump(_originPlanet.Id);
        Assert.True(result);
        Assert.True(_nav.IsTraveling);
    }

    private void SetupJumpPrerequisites()
    {
        _gameState.CurrentLocation = _originPlanet.Id;
        _gameState.CurrentFuel = 200;
        _gameState.ShipId = _testShip.Id;
    }
}
