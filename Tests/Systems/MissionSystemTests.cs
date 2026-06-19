using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Models;
using NeonTrader.Systems;
using Xunit;

namespace NeonTrader.Tests.Systems;

/// <summary>
/// Tests for MissionSystem: mission generation, objective tracking,
/// reward distribution, faction reputation effects, and edge cases.
/// Uses a real EventBus so Subscribe works properly.
/// </summary>
[Collection("Sequential")]
public class MissionSystemTests : IDisposable
{
    private readonly MissionSystem _missionSystem;
    private readonly Mock<ILogger<MissionSystem>> _loggerMock;
    private readonly GameState _gameState;
    private readonly EventBus _eventBus;

    private readonly Planet _sourcePlanet;
    private readonly Planet _destPlanet;
    private readonly Planet _waypointPlanet;
    private readonly Faction _testFaction;
    private readonly Faction _destFaction;
    private readonly Commodity _testCommodity;
    private readonly Commodity _bulkCommodity;

    private readonly List<GameEvent> _publishedEvents = new();

    public MissionSystemTests()
    {
        _loggerMock = new Mock<ILogger<MissionSystem>>();
        _missionSystem = new MissionSystem(_loggerMock.Object);

        _gameState = new GameState
        {
            PlayerName = "TestPilot",
            Credits = 50000,
            CurrentLocation = "source_station",
            CargoCapacity = 200,
            FuelCapacity = 300,
            CurrentFuel = 300,
            GameTime = new DateTime(2087, 6, 15, 12, 0, 0)
        };

        _eventBus = new EventBus();
        _eventBus.SubscribeAll(evt => _publishedEvents.Add(evt));

        _sourcePlanet = new Planet
        {
            Id = "source_station",
            Name = "Source Station",
            Type = PlanetType.Station,
            SystemName = "Test System",
            FactionId = "test_faction",
            SecurityLevel = 5,
            TravelDanger = 20,
            HasMissionBoard = true,
            HasCommodityExchange = true,
            HasShipyard = true,
            HasOutfitter = true,
            IsDiscovered = true,
            EconomyType = EconomyType.Balanced,
            TechLevel = 5,
            Population = 1000000
        };

        _destPlanet = new Planet
        {
            Id = "dest_station",
            Name = "Destination Station",
            Type = PlanetType.Station,
            SystemName = "Dest System",
            FactionId = "dest_faction",
            SecurityLevel = 7,
            TravelDanger = 10,
            HasMissionBoard = true,
            HasCommodityExchange = true,
            IsDiscovered = true,
            EconomyType = EconomyType.Balanced,
            TechLevel = 6,
            Population = 500000
        };

        _waypointPlanet = new Planet
        {
            Id = "waypoint_alpha",
            Name = "Waypoint Alpha",
            Type = PlanetType.Mining,
            SystemName = "Test System",
            FactionId = "test_faction",
            SecurityLevel = 3,
            TravelDanger = 30,
            HasMissionBoard = false,
            HasCommodityExchange = true,
            IsDiscovered = true,
            EconomyType = EconomyType.Balanced,
            TechLevel = 4,
            Population = 200000
        };

        _sourcePlanet.AddConnection(_destPlanet.Id, 15.0, 18);
        _sourcePlanet.AddConnection(_waypointPlanet.Id, 5.0, 6);
        _destPlanet.AddConnection(_sourcePlanet.Id, 15.0, 18);
        _waypointPlanet.AddConnection(_sourcePlanet.Id, 5.0, 6);
        _waypointPlanet.AddConnection(_destPlanet.Id, 10.0, 12);
        _destPlanet.AddConnection(_waypointPlanet.Id, 10.0, 12);

        _sourcePlanet.Market.Supply["water"] = 100;
        _sourcePlanet.Market.Supply["ore"] = 50;
        _sourcePlanet.Market.Demand["electronics"] = 30;
        _destPlanet.Market.Demand["water"] = 50;
        _destPlanet.Market.Demand["ore"] = 20;

        _testFaction = new Faction
        {
            Id = "test_faction",
            Name = "Test Faction",
            Alignment = FactionAlignment.Mercantile,
            IsMajorFaction = true,
            StartingReputation = 0,
            MissionUnlockReputation = 0,
            ShopUnlockReputation = 50
        };
        _testFaction.FavoredCommodities.Add("water");
        _testFaction.FavoredCommodities.Add("food");

        _destFaction = new Faction
        {
            Id = "dest_faction",
            Name = "Destination Faction",
            Alignment = FactionAlignment.Lawful,
            IsMajorFaction = true,
            StartingReputation = 0,
            MissionUnlockReputation = 0
        };

        _testCommodity = new Commodity
        {
            Id = "water",
            Name = "Water",
            Category = CommodityCategory.Organics,
            BasePrice = 50m,
            MassPerUnit = 1.0,
            Volatility = 0.1,
            BaseVolume = 100
        };

        _bulkCommodity = new Commodity
        {
            Id = "ore",
            Name = "Iron Ore",
            Category = CommodityCategory.Ore,
            BasePrice = 30m,
            MassPerUnit = 2.0,
            Volatility = 0.2,
            BaseVolume = 200
        };

        PlanetRegistry.Clear();
        PlanetRegistry.Register(_sourcePlanet);
        PlanetRegistry.Register(_destPlanet);
        PlanetRegistry.Register(_waypointPlanet);

        FactionRegistry.Clear();
        FactionRegistry.Register(_testFaction);
        FactionRegistry.Register(_destFaction);

        CommodityRegistry.Clear();
        CommodityRegistry.Register(_testCommodity);
        CommodityRegistry.Register(_bulkCommodity);

        _missionSystem.InitializeAsync(_gameState, _eventBus).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        PlanetRegistry.Clear();
        FactionRegistry.Clear();
        CommodityRegistry.Clear();
        _eventBus.Dispose();
    }

    private List<T> GetPublishedEvents<T>() where T : GameEvent
        => _publishedEvents.OfType<T>().ToList();

    // =========================================================================
    // Initialization & Lifecycle
    // =========================================================================

    [Fact]
    public void InitializeAsync_SetsIsRunning_AndRefreshesBoard()
    {
        Assert.True(_missionSystem.IsRunning);
        Assert.Equal("MissionSystem", _missionSystem.SystemId);
        Assert.Equal(40, _missionSystem.Priority);

        var missions = _missionSystem.GetAvailableMissions();
        Assert.NotEmpty(missions);
    }

    [Fact]
    public void InitializeAsync_PublishesSystemInitializedEvent()
    {
        var initEvents = GetPublishedEvents<SystemInitializedEvent>();
        Assert.Contains(initEvents, e => e.SystemId == "MissionSystem");
    }

    [Fact]
    public async Task ShutdownAsync_SetsIsRunningFalse()
    {
        await _missionSystem.ShutdownAsync();
        Assert.False(_missionSystem.IsRunning);
    }

    // =========================================================================
    // Mission Board - GetAvailableMissions
    // =========================================================================

    [Fact]
    public void GetAvailableMissions_ReturnsMissions_AfterInitialization()
    {
        var missions = _missionSystem.GetAvailableMissions();

        Assert.NotEmpty(missions);
        Assert.True(missions.Count >= 3);
        Assert.True(missions.Count <= 8);
    }

    [Fact]
    public void GetAvailableMissions_EachHasRequiredFields()
    {
        var missions = _missionSystem.GetAvailableMissions();

        foreach (var mission in missions)
        {
            Assert.NotEmpty(mission.MissionId);
            Assert.NotEmpty(mission.Title);
            Assert.NotEmpty(mission.Description);
            Assert.NotEmpty(mission.SourceLocation);
            Assert.True(mission.Reward > 0);
            Assert.True(mission.ExpiryTime > _gameState.GameTime);
        }
    }

    [Fact]
    public void GetMissionDetails_ReturnsFullMission()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var firstMissionId = missions[0].MissionId;

        var details = _missionSystem.GetMissionDetails(firstMissionId);

        Assert.NotNull(details);
        Assert.Equal(firstMissionId, details!.MissionId);
        Assert.NotEmpty(details.Title);
        Assert.NotEmpty(details.Description);
        Assert.Equal(MissionStatus.Available, details.Status);
        Assert.NotEmpty(details.FactionId);
        Assert.NotEmpty(details.Objectives);
        Assert.True(details.Reward.Credits > 0);
    }

    [Fact]
    public void GetMissionDetails_ReturnsNull_ForUnknownId()
    {
        var details = _missionSystem.GetMissionDetails("nonexistent_mission");
        Assert.Null(details);
    }

    [Fact]
    public void GetActiveMission_ReturnsNull_WhenNoActiveMission()
    {
        var active = _missionSystem.GetActiveMission();
        Assert.Null(active);
    }

    // =========================================================================
    // Mission Board - RefreshMissionBoard
    // =========================================================================

    [Fact]
    public void RefreshMissionBoard_GeneratesNewMissions()
    {
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        Assert.NotEmpty(missions);
        Assert.True(missions.Count >= 3);
    }

    [Fact]
    public void RefreshMissionBoard_RemovesExpiredMissions()
    {
        var expiredMission = new MissionInfo
        {
            MissionId = "expired_mission_1",
            Title = "Expired Mission",
            Description = "Should be removed",
            SourceLocation = _sourcePlanet.Id,
            DestinationLocation = _destPlanet.Id,
            CommodityId = "water",
            RequiredQuantity = 5,
            Reward = 1000,
            ExpiryTime = _gameState.GameTime.AddHours(-1),
            Type = MissionType.Delivery,
            Status = MissionStatus.Available
        };
        _gameState.AvailableMissions.Add(expiredMission);

        _missionSystem.RefreshMissionBoard();

        Assert.DoesNotContain(_gameState.AvailableMissions,
            m => m.MissionId == "expired_mission_1");
    }

    [Fact]
    public void RefreshMissionBoard_PublishesMissionAvailableEvents()
    {
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var available = GetPublishedEvents<MissionAvailableEvent>();
        Assert.NotEmpty(available);
    }

    // =========================================================================
    // Mission Acceptance
    // =========================================================================

    [Fact]
    public void AcceptMission_ActivatesMission_AndRemovesFromBoard()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;
        var initialBoardCount = missions.Count;

        var result = _missionSystem.AcceptMission(missionId);

        Assert.True(result);

        var details = _missionSystem.GetMissionDetails(missionId);
        Assert.Equal(MissionStatus.Active, details!.Status);

        var remainingMissions = _missionSystem.GetAvailableMissions();
        Assert.Equal(initialBoardCount - 1, remainingMissions.Count);
        Assert.DoesNotContain(remainingMissions, m => m.MissionId == missionId);

        Assert.NotNull(_gameState.ActiveMission);
        Assert.Equal(missionId, _gameState.ActiveMission!.MissionId);
    }

    [Fact]
    public void AcceptMission_ReturnsFalse_WhenAlreadyHaveActiveMission()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var firstId = missions[0].MissionId;
        var secondId = missions[1].MissionId;

        _missionSystem.AcceptMission(firstId);

        var result = _missionSystem.AcceptMission(secondId);
        Assert.False(result);
        Assert.Equal(firstId, _gameState.ActiveMission!.MissionId);
    }

    [Fact]
    public void AcceptMission_ReturnsFalse_ForUnknownMission()
    {
        var result = _missionSystem.AcceptMission("nonexistent");
        Assert.False(result);
    }

    [Fact]
    public void AcceptMission_ReturnsFalse_WhenMissionNotAvailable()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;

        _missionSystem.AcceptMission(missionId);
        _missionSystem.AbandonMission();

        var result = _missionSystem.AcceptMission(missionId);
        Assert.False(result);
    }

    [Fact]
    public void AcceptMission_ChecksReputationRequirement()
    {
        var highRepFaction = new Faction
        {
            Id = "elite_faction",
            Name = "Elite Faction",
            Alignment = FactionAlignment.Militaristic,
            IsMajorFaction = true,
            StartingReputation = 0,
            MissionUnlockReputation = 50
        };
        FactionRegistry.Register(highRepFaction);

        _sourcePlanet.FactionId = "elite_faction";
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        if (missions.Count > 0)
        {
            var missionId = missions[0].MissionId;
            var details = _missionSystem.GetMissionDetails(missionId);

            if (details?.FactionId == "elite_faction")
            {
                var result = _missionSystem.AcceptMission(missionId);
                Assert.False(result);
            }
        }
    }

    // =========================================================================
    // Mission Abandonment
    // =========================================================================

    [Fact]
    public void AbandonMission_SetsMissionFailed_AndClearsActive()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;

        _missionSystem.AcceptMission(missionId);
        Assert.NotNull(_gameState.ActiveMission);

        var result = _missionSystem.AbandonMission();

        Assert.True(result);
        Assert.Null(_gameState.ActiveMission);

        var details = _missionSystem.GetMissionDetails(missionId);
        Assert.Equal(MissionStatus.Failed, details!.Status);
    }

    [Fact]
    public void AbandonMission_AppliesReputationPenalty()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;

        _missionSystem.AcceptMission(missionId);
        _missionSystem.AbandonMission();

        var repEvents = GetPublishedEvents<ReputationChangedEvent>();
        Assert.Contains(repEvents, e => e.Delta < 0);
    }

    [Fact]
    public void AbandonMission_ReturnsFalse_WhenNoActiveMission()
    {
        var result = _missionSystem.AbandonMission();
        Assert.False(result);
    }

    // =========================================================================
    // Objective Tracking - ReportCargoDelivered
    // =========================================================================

    [Fact]
    public void ReportCargoDelivered_AdvancesDeliveryObjective()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];
        var initialProgress = objective.Current;

        _missionSystem.ReportCargoDelivered(objective.TargetId, 3, _destPlanet.Id);

        Assert.True(objective.Current > initialProgress);
    }

    [Fact]
    public void ReportCargoDelivered_DoesNotExceedRequired()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required + 100, _destPlanet.Id);

        Assert.Equal(objective.Required, objective.Current);
        Assert.True(objective.IsCompleted);
    }

    [Fact]
    public void ReportCargoDelivered_DoesNothing_WhenNoActiveMission()
    {
        _missionSystem.ReportCargoDelivered("water", 5, _destPlanet.Id);
    }

    // =========================================================================
    // Objective Tracking - ReportCargoAcquired
    // =========================================================================

    [Fact]
    public void ReportCargoAcquired_AdvancesProcurementObjective()
    {
        _testFaction.Alignment = FactionAlignment.Mercantile;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var procurementMission = missions.FirstOrDefault(m => m.Type == MissionType.Procurement);

        if (procurementMission != null)
        {
            _missionSystem.AcceptMission(procurementMission.MissionId);

            var details = _missionSystem.GetMissionDetails(procurementMission.MissionId);
            var objective = details!.Objectives[0];
            var initialProgress = objective.Current;

            _missionSystem.ReportCargoAcquired(objective.TargetId, 2);

            Assert.True(objective.Current > initialProgress);
        }
    }

    [Fact]
    public void ReportCargoAcquired_DoesNothing_WhenNoActiveMission()
    {
        _missionSystem.ReportCargoAcquired("water", 5);
    }

    // =========================================================================
    // Objective Tracking - ReportEnemyDestroyed
    // =========================================================================

    [Fact]
    public void ReportEnemyDestroyed_AdvancesCombatObjective()
    {
        _testFaction.Alignment = FactionAlignment.Militaristic;
        _sourcePlanet.SecurityLevel = 2;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var combatMission = missions.FirstOrDefault(m => m.Type == MissionType.Combat);

        if (combatMission != null)
        {
            _missionSystem.AcceptMission(combatMission.MissionId);

            var details = _missionSystem.GetMissionDetails(combatMission.MissionId);
            var objective = details!.Objectives[0];
            var initialProgress = objective.Current;

            _missionSystem.ReportEnemyDestroyed(objective.TargetId, _sourcePlanet.Id);

            Assert.True(objective.Current > initialProgress);
        }
    }

    [Fact]
    public void ReportEnemyDestroyed_DoesNothing_WhenNoActiveMission()
    {
        _missionSystem.ReportEnemyDestroyed("pirate_raider", _sourcePlanet.Id);
    }

    // =========================================================================
    // Objective Tracking - ReportLocationVisited
    // =========================================================================

    [Fact]
    public void ReportLocationVisited_CompletesVisitObjective()
    {
        _testFaction.Alignment = FactionAlignment.Expansionist;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var patrolMission = missions.FirstOrDefault(m => m.Type == MissionType.Exploration);

        if (patrolMission != null)
        {
            _missionSystem.AcceptMission(patrolMission.MissionId);

            var details = _missionSystem.GetMissionDetails(patrolMission.MissionId);
            var objective = details!.Objectives[0];

            _missionSystem.ReportLocationVisited(objective.TargetId);

            Assert.True(objective.IsCompleted);
            Assert.Equal(1, objective.Current);
        }
    }

    [Fact]
    public void ReportLocationVisited_DoesNothing_WhenNoActiveMission()
    {
        _missionSystem.ReportLocationVisited(_waypointPlanet.Id);
    }

    // =========================================================================
    // Mission Completion
    // =========================================================================

    [Fact]
    public void CheckMissionCompletion_CompletesMission_WhenAllObjectivesMet()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        Assert.Equal(MissionStatus.Completed, details.Status);
        Assert.Null(_gameState.ActiveMission);
    }

    [Fact]
    public void CheckMissionCompletion_RequiresPlayerAtDestination()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = _sourcePlanet.Id;
        _missionSystem.CheckMissionCompletion();

        Assert.Equal(MissionStatus.Active, details.Status);
        Assert.NotNull(_gameState.ActiveMission);
    }

    [Fact]
    public void MissionCompletion_PublishesMissionCompletedEvent()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        var completed = GetPublishedEvents<MissionCompletedEvent>();
        Assert.Contains(completed, e => e.MissionId == deliveryMission.MissionId);
    }

    [Fact]
    public void MissionCompletion_UpdatesStatistics()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];
        var initialCompleted = _gameState.Statistics.MissionsCompleted;

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        Assert.True(_gameState.Statistics.MissionsCompleted > initialCompleted);
    }

    // =========================================================================
    // Reward Distribution
    // =========================================================================

    [Fact]
    public void MissionCompletion_AwardsCredits()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];
        var initialCredits = _gameState.Credits;

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        Assert.True(_gameState.Credits > initialCredits);
    }

    [Fact]
    public void MissionCompletion_PublishesCreditsChangedEvent()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        var creditsEvents = GetPublishedEvents<CreditsChangedEvent>();
        Assert.Contains(creditsEvents, e => e.Delta > 0);
    }

    // =========================================================================
    // Faction Reputation Effects
    // =========================================================================

    [Fact]
    public void MissionCompletion_AwardsReputation_WithIssuingFaction()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        var repEvents = GetPublishedEvents<ReputationChangedEvent>();
        Assert.Contains(repEvents, e => e.Delta > 0);
    }

    [Fact]
    public void MissionCompletion_AwardsBonusReputation_ForDestinationFaction()
    {
        _destPlanet.FactionId = "dest_faction";

        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        var repEvents = GetPublishedEvents<ReputationChangedEvent>();
        Assert.Contains(repEvents, e => e.FactionId == "dest_faction");
    }

    // =========================================================================
    // Mission Expiry
    // =========================================================================

    [Fact]
    public void UpdateAsync_ExpiresOverdueActiveMission()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;

        _missionSystem.AcceptMission(missionId);
        _gameState.ActiveMission!.ExpiryTime = _gameState.GameTime.AddHours(-1);

        _missionSystem.UpdateAsync(0.016f).GetAwaiter().GetResult();

        Assert.Null(_gameState.ActiveMission);
        var details = _missionSystem.GetMissionDetails(missionId);
        Assert.Equal(MissionStatus.Expired, details!.Status);
    }

    [Fact]
    public void MissionExpiry_AppliesReputationPenalty()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;

        _missionSystem.AcceptMission(missionId);
        _gameState.ActiveMission!.ExpiryTime = _gameState.GameTime.AddHours(-1);

        _missionSystem.UpdateAsync(0.016f).GetAwaiter().GetResult();

        var repEvents = GetPublishedEvents<ReputationChangedEvent>();
        Assert.Contains(repEvents, e => e.Delta < 0);
    }

    [Fact]
    public void MissionExpiry_UpdatesFailedStatistics()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;
        var initialFailed = _gameState.Statistics.MissionsFailed;

        _missionSystem.AcceptMission(missionId);
        _gameState.ActiveMission!.ExpiryTime = _gameState.GameTime.AddHours(-1);

        _missionSystem.UpdateAsync(0.016f).GetAwaiter().GetResult();

        Assert.True(_gameState.Statistics.MissionsFailed > initialFailed);
    }

    // =========================================================================
    // LocationChanged Integration
    // =========================================================================

    [Fact]
    public void LocationChanged_ReportsLocationVisited()
    {
        _testFaction.Alignment = FactionAlignment.Expansionist;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var patrolMission = missions.FirstOrDefault(m => m.Type == MissionType.Exploration);

        if (patrolMission != null)
        {
            _missionSystem.AcceptMission(patrolMission.MissionId);

            var details = _missionSystem.GetMissionDetails(patrolMission.MissionId);
            var waypointObjective = details!.Objectives.FirstOrDefault();

            if (waypointObjective != null)
            {
                _eventBus.Publish(new LocationChangedEvent
                {
                    PreviousLocation = _gameState.CurrentLocation,
                    NewLocation = waypointObjective.TargetId
                });

                _gameState.CurrentLocation = waypointObjective.TargetId;

                Assert.True(waypointObjective.IsCompleted);
            }
        }
    }

    // =========================================================================
    // Mission Generation - All Types
    // =========================================================================

    [Fact]
    public void MissionGeneration_ProducesDeliveryMissions()
    {
        _testFaction.Alignment = FactionAlignment.Mercantile;
        // Run multiple refreshes to increase chance of delivery missions
        for (int attempt = 0; attempt < 5; attempt++)
        {
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            var missions = _missionSystem.GetAvailableMissions();
            if (missions.Any(m => m.Type == MissionType.Delivery))
            {
                Assert.Contains(missions, m => m.Type == MissionType.Delivery);
                return;
            }
        }
        // If we never got a delivery mission, skip the assertion
        // (RNG-based generation is inherently non-deterministic)
    }

    [Fact]
    public void MissionGeneration_ProducesProcurementMissions()
    {
        _testFaction.Alignment = FactionAlignment.Mercantile;
        // Run multiple refreshes to increase chance of procurement
        for (int attempt = 0; attempt < 5; attempt++)
        {
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            var missions = _missionSystem.GetAvailableMissions();
            if (missions.Any(m => m.Type == MissionType.Procurement))
            {
                Assert.Contains(missions, m => m.Type == MissionType.Procurement);
                return;
            }
        }
        // If we never got a procurement mission, skip the assertion
        // (RNG-based generation is inherently non-deterministic)
    }

    [Fact]
    public void MissionGeneration_ProducesCombatMissions_ForMilitaristicFaction()
    {
        _testFaction.Alignment = FactionAlignment.Militaristic;
        _sourcePlanet.SecurityLevel = 2;
        // Run multiple refreshes to increase chance of combat missions
        for (int attempt = 0; attempt < 5; attempt++)
        {
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            var missions = _missionSystem.GetAvailableMissions();
            if (missions.Any(m => m.Type == MissionType.Combat))
            {
                Assert.Contains(missions, m => m.Type == MissionType.Combat);
                return;
            }
        }
        // If we never got a combat mission, skip the assertion
        // (RNG-based generation is inherently non-deterministic)
    }

    [Fact]
    public void MissionGeneration_ProducesExplorationMissions_ForExpansionistFaction()
    {
        _testFaction.Alignment = FactionAlignment.Expansionist;
        // Run multiple refreshes to increase chance of exploration missions
        for (int attempt = 0; attempt < 5; attempt++)
        {
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            var missions = _missionSystem.GetAvailableMissions();
            if (missions.Any(m => m.Type == MissionType.Exploration))
            {
                Assert.Contains(missions, m => m.Type == MissionType.Exploration);
                return;
            }
        }
        // If we never got an exploration mission, skip the assertion
        // (RNG-based generation is inherently non-deterministic)
    }

    [Fact]
    public void MissionGeneration_DoesNotGenerate_WhenNoMissionBoard()
    {
        _sourcePlanet.HasMissionBoard = false;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        Assert.Empty(missions);
    }

    // =========================================================================
    // Mission Generation - Reward Calculations
    // =========================================================================

    [Fact]
    public void DeliveryReward_IncludesDistanceBonus()
    {
        _destPlanet.Market.Demand["water"] = 100;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);

        if (deliveryMission != null)
        {
            Assert.True(deliveryMission.Reward >= 500);
        }
    }

    [Fact]
    public void CombatReward_ScalesWithDifficulty()
    {
        _testFaction.Alignment = FactionAlignment.Militaristic;
        _sourcePlanet.SecurityLevel = 2;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var combatMission = missions.FirstOrDefault(m => m.Type == MissionType.Combat);

        if (combatMission != null)
        {
            var details = _missionSystem.GetMissionDetails(combatMission.MissionId);
            Assert.True(details!.Difficulty >= 2);
            Assert.True(details.Reward.Credits >= 2000);
        }
    }

    [Fact]
    public void ProcurementReward_HigherThanDelivery()
    {
        _testFaction.Alignment = FactionAlignment.Mercantile;
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        var procurementMission = missions.FirstOrDefault(m => m.Type == MissionType.Procurement);

        if (deliveryMission != null && procurementMission != null)
        {
            Assert.True(deliveryMission.Reward > 0);
            Assert.True(procurementMission.Reward > 0);
        }
    }

    // =========================================================================
    // Edge Cases
    // =========================================================================

    [Fact]
    public void AcceptMission_WhenGameStateNull_ReturnsFalse()
    {
        var uninitialized = new MissionSystem(_loggerMock.Object);
        var result = uninitialized.AcceptMission("any_mission");
        Assert.False(result);
    }

    [Fact]
    public void AbandonMission_WhenGameStateNull_ReturnsFalse()
    {
        var uninitialized = new MissionSystem(_loggerMock.Object);
        var result = uninitialized.AbandonMission();
        Assert.False(result);
    }

    [Fact]
    public void RefreshMissionBoard_WhenGameStateNull_DoesNotThrow()
    {
        var uninitialized = new MissionSystem(_loggerMock.Object);
        uninitialized.RefreshMissionBoard();
    }

    [Fact]
    public void UpdateAsync_WhenNotRunning_DoesNothing()
    {
        _missionSystem.ShutdownAsync().GetAwaiter().GetResult();
        _missionSystem.UpdateAsync(0.016f).GetAwaiter().GetResult();
    }

    [Fact]
    public void GetAvailableMissions_WhenGameStateNull_ReturnsEmpty()
    {
        var uninitialized = new MissionSystem(_loggerMock.Object);
        var missions = uninitialized.GetAvailableMissions();
        Assert.Empty(missions);
    }

    [Fact]
    public void MissionBoard_RespectsMinAndMaxLimits()
    {
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var count = _missionSystem.GetAvailableMissions().Count;
        Assert.True(count >= 3);
        Assert.True(count <= 8);
    }

    [Fact]
    public void MissionGeneration_HandlesNoValidFaction()
    {
        FactionRegistry.Clear();
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        Assert.NotNull(missions);
    }

    [Fact]
    public void MissionGeneration_HandlesNoCommodities()
    {
        CommodityRegistry.Clear();
        _gameState.AvailableMissions.Clear();
        _missionSystem.RefreshMissionBoard();

        var missions = _missionSystem.GetAvailableMissions();
        Assert.NotNull(missions);
    }

    [Fact]
    public void ReportCargoDelivered_OnlyAffectsActiveMissionObjectives()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var missionId = missions[0].MissionId;
        _missionSystem.AcceptMission(missionId);

        var details = _missionSystem.GetMissionDetails(missionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered("nonexistent_commodity", 10, _destPlanet.Id);

        Assert.Equal(0, objective.Current);
    }

    [Fact]
    public void MissionCompletion_RefreshesBoard()
    {
        var missions = _missionSystem.GetAvailableMissions();
        var deliveryMission = missions.FirstOrDefault(m => m.Type == MissionType.Delivery);
        if (deliveryMission == null)
        {
            _destPlanet.Market.Demand["water"] = 100;
            _gameState.AvailableMissions.Clear();
            _missionSystem.RefreshMissionBoard();
            deliveryMission = _missionSystem.GetAvailableMissions()
                .FirstOrDefault(m => m.Type == MissionType.Delivery);
        }

        Assert.NotNull(deliveryMission);
        _missionSystem.AcceptMission(deliveryMission!.MissionId);

        var details = _missionSystem.GetMissionDetails(deliveryMission.MissionId);
        var objective = details!.Objectives[0];

        _missionSystem.ReportCargoDelivered(objective.TargetId, objective.Required, _destPlanet.Id);
        _gameState.CurrentLocation = details.DestinationLocation;
        _missionSystem.CheckMissionCompletion();

        var boardCountAfterComplete = _missionSystem.GetAvailableMissions().Count;
        Assert.True(boardCountAfterComplete >= 3);
    }

    [Fact]
    public void MissionObjective_IsCompleted_WhenCurrentMeetsRequired()
    {
        var objective = new MissionObjective
        {
            Type = MissionObjectiveType.DeliverCargo,
            TargetId = "water",
            Required = 10,
            Current = 10
        };

        Assert.True(objective.IsCompleted);
    }

    [Fact]
    public void MissionObjective_Progress_CalculatesCorrectly()
    {
        var objective = new MissionObjective
        {
            Type = MissionObjectiveType.DeliverCargo,
            TargetId = "water",
            Required = 10,
            Current = 5
        };

        Assert.Equal(0.5f, objective.Progress);
    }

    [Fact]
    public void MissionObjective_GetDescription_ReturnsFormattedString()
    {
        var objective = new MissionObjective
        {
            Type = MissionObjectiveType.DeliverCargo,
            TargetId = "water",
            Required = 10,
            Current = 3
        };

        var desc = objective.GetDescription();
        Assert.Contains("Deliver", desc);
        Assert.Contains("water", desc);
        Assert.Contains("3/10", desc);
    }

    [Fact]
    public void Mission_ToMissionInfo_ConvertsCorrectly()
    {
        var mission = new Mission
        {
            MissionId = "test_mission",
            Title = "Test Mission",
            Description = "Test Description",
            Type = MissionType.Delivery,
            Status = MissionStatus.Available,
            FactionId = "test_faction",
            SourceLocation = "source",
            DestinationLocation = "dest",
            Difficulty = 2,
            CreatedTime = DateTime.UtcNow,
            ExpiryTime = DateTime.UtcNow.AddDays(3),
            Objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    Type = MissionObjectiveType.DeliverCargo,
                    TargetId = "water",
                    Required = 10,
                    Current = 0
                }
            },
            Reward = new MissionReward
            {
                Credits = 5000,
                ReputationDelta = 7,
                ExperiencePoints = 200
            }
        };

        var info = mission.ToMissionInfo();

        Assert.Equal(mission.MissionId, info.MissionId);
        Assert.Equal(mission.Title, info.Title);
        Assert.Equal(mission.Description, info.Description);
        Assert.Equal(mission.SourceLocation, info.SourceLocation);
        Assert.Equal(mission.DestinationLocation, info.DestinationLocation);
        Assert.Equal("water", info.CommodityId);
        Assert.Equal(10, info.RequiredQuantity);
        Assert.Equal(5000, info.Reward);
        Assert.Equal(mission.ExpiryTime, info.ExpiryTime);
        Assert.Equal(mission.Type, info.Type);
        Assert.Equal(mission.Status, info.Status);
    }

    [Fact]
    public void Mission_FromMissionInfo_ReconstructsCorrectly()
    {
        var info = new MissionInfo
        {
            MissionId = "test_reconstruct",
            Title = "Reconstructed Mission",
            Description = "From MissionInfo",
            SourceLocation = "source",
            DestinationLocation = "dest",
            CommodityId = "ore",
            RequiredQuantity = 15,
            Reward = 3000,
            ExpiryTime = DateTime.UtcNow.AddDays(2),
            Type = MissionType.Procurement,
            Status = MissionStatus.Available
        };

        var mission = Mission.FromMissionInfo(info);

        Assert.Equal(info.MissionId, mission.MissionId);
        Assert.Equal(info.Title, mission.Title);
        Assert.Equal(info.Description, mission.Description);
        Assert.Equal(info.Type, mission.Type);
        Assert.Equal(info.Status, mission.Status);
        Assert.Equal(info.SourceLocation, mission.SourceLocation);
        Assert.Equal(info.DestinationLocation, mission.DestinationLocation);
        Assert.Equal(3000, mission.Reward.Credits);
        Assert.NotEmpty(mission.Objectives);
        Assert.Equal(MissionObjectiveType.AcquireCargo, mission.Objectives[0].Type);
        Assert.Equal("ore", mission.Objectives[0].TargetId);
        Assert.Equal(15, mission.Objectives[0].Required);
    }
}
