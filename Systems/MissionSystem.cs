using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;

namespace NeonTrader.Systems;

/// <summary>
/// MissionSystem - manages mission generation, tracking, and reward distribution.
/// Handles delivery, procurement, combat, and patrol missions.
/// Integrates with GameState, FactionRegistry, PlanetRegistry, Reputation, and EventBus.
/// Priority 40 - runs after DataLoader (0) and SaveSystem (10).
/// </summary>
public sealed class MissionSystem : IGameSystem
{
    private readonly ILogger<MissionSystem> _logger;
    private GameState? _gameState;
    private IEventBus? _eventBus;
    private bool _isRunning;
    private readonly Random _rng = new();

    // Internal mission store with rich objective/reward data
    private readonly Dictionary<string, Mission> _missions = new();

    // Mission board refresh tracking
    private DateTime _lastBoardRefresh;
    private const double BoardRefreshIntervalHours = 24.0;

    // Configuration constants
    private const int MaxAvailableMissions = 8;
    private const int MinAvailableMissions = 3;
    private const int MissionIdCounter = 0;

    // Mission title templates
    private static readonly string[] DeliveryTitlePrefixes =
    {
        "Urgent Delivery:", "Cargo Run:", "Supply Drop:", "Express Shipment:",
        "Priority Transport:", "Freight Contract:", "Courier Mission:"
    };

    private static readonly string[] ProcurementTitlePrefixes =
    {
        "Resource Acquisition:", "Procurement Order:", "Supply Request:",
        "Material Sourcing:", "Acquisition Contract:", "Resource Hunt:"
    };

    private static readonly string[] CombatTitlePrefixes =
    {
        "Bounty Contract:", "Elimination Order:", "Defense Mission:",
        "Hunt Order:", "Combat Patrol:", "Strike Mission:"
    };

    private static readonly string[] PatrolTitlePrefixes =
    {
        "Patrol Route:", "Survey Mission:", "Reconnaissance:",
        "Scouting Run:", "System Patrol:", "Exploration Contract:"
    };

    // Enemy types for combat missions
    private static readonly string[] EnemyTypes =
    {
        "pirate_raider", "pirate_interceptor", "pirate_gunship",
        "rival_mercenary", "hostile_drone", "criminal_smuggler"
    };

    private static readonly string[] EnemyDisplayNames =
    {
        "Pirate Raiders", "Pirate Interceptors", "Pirate Gunships",
        "Rival Mercenaries", "Hostile Drones", "Criminal Smugglers"
    };

    /// <summary>
    /// Unique system identifier
    /// </summary>
    public string SystemId => "MissionSystem";

    /// <summary>
    /// Priority 40 - runs after DataLoader (0) and SaveSystem (10)
    /// </summary>
    public int Priority => 40;

    /// <summary>
    /// Whether the system is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    public MissionSystem(ILogger<MissionSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the mission system
    /// </summary>
    public async Task InitializeAsync(
        GameState gameState,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        _gameState = gameState;
        _eventBus = eventBus;
        _isRunning = true;

        _logger.LogInformation("MissionSystem initializing...");

        // Subscribe to location changes to refresh mission board
        _eventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);

        // Load any existing missions from GameState
        LoadMissionsFromGameState();

        // Initial board refresh
        RefreshMissionBoard();

        _logger.LogInformation(
            "MissionSystem initialized. Available missions: {Count}",
            _gameState.AvailableMissions.Count);

        _eventBus.Publish(new SystemInitializedEvent { SystemId = SystemId });
    }

    /// <summary>
    /// Update the mission system - check for expired missions and refresh board
    /// </summary>
    public Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _gameState == null)
            return Task.CompletedTask;

        // Check for expired missions
        ExpireOverdueMissions();

        // Refresh mission board periodically
        var gameHoursSinceRefresh = (_gameState.GameTime - _lastBoardRefresh).TotalHours;
        if (gameHoursSinceRefresh >= BoardRefreshIntervalHours)
        {
            RefreshMissionBoard();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shutdown the mission system
    /// </summary>
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        _logger.LogInformation("MissionSystem shutdown");
        return Task.CompletedTask;
    }

    // =========================================================================
    // Public API - Mission Board
    // =========================================================================

    /// <summary>
    /// Gets all available missions at the player's current location
    /// </summary>
    public List<MissionInfo> GetAvailableMissions()
    {
        return _gameState?.AvailableMissions ?? new List<MissionInfo>();
    }

    /// <summary>
    /// Gets the full mission details (with objectives and rewards)
    /// </summary>
    public Mission? GetMissionDetails(string missionId)
    {
        _missions.TryGetValue(missionId, out var mission);
        return mission;
    }

    /// <summary>
    /// Gets the player's active mission details
    /// </summary>
    public Mission? GetActiveMission()
    {
        if (_gameState?.ActiveMission == null)
            return null;

        _missions.TryGetValue(_gameState.ActiveMission.MissionId, out var mission);
        return mission;
    }

    /// <summary>
    /// Accept a mission from the board
    /// </summary>
    public bool AcceptMission(string missionId)
    {
        if (_gameState == null || _eventBus == null)
            return false;

        if (!_missions.TryGetValue(missionId, out var mission))
        {
            _logger.LogWarning("AcceptMission: mission {MissionId} not found", missionId);
            return false;
        }

        if (mission.Status != MissionStatus.Available)
        {
            _logger.LogWarning("AcceptMission: mission {MissionId} is not available (status: {Status})",
                missionId, mission.Status);
            return false;
        }

        // Check if player already has an active mission
        if (_gameState.ActiveMission != null)
        {
            _logger.LogWarning("AcceptMission: player already has active mission {MissionId}",
                _gameState.ActiveMission.MissionId);
            return false;
        }

        // Check reputation requirement
        if (!string.IsNullOrEmpty(mission.FactionId))
        {
            var faction = FactionRegistry.Get(mission.FactionId);
            if (faction != null)
            {
                // Use Reputation model if available, otherwise check faction's unlock requirement
                var playerRep = 0; // Default
                if (faction.StartingReputation != 0)
                    playerRep = faction.StartingReputation;

                if (playerRep < faction.MissionUnlockReputation)
                {
                    _logger.LogWarning(
                        "AcceptMission: insufficient reputation with {Faction} (need {Required}, have {Current})",
                        mission.FactionId, faction.MissionUnlockReputation, playerRep);
                    return false;
                }
            }
        }

        // Activate the mission
        mission.Status = MissionStatus.Active;
        mission.AcceptedTime = _gameState.GameTime;

        // Update GameState
        _gameState.ActiveMission = mission.ToMissionInfo();
        _gameState.AvailableMissions.RemoveAll(m => m.MissionId == missionId);

        _logger.LogInformation("Mission accepted: {MissionId} - {Title}", missionId, mission.Title);

        return true;
    }

    /// <summary>
    /// Abandon the active mission
    /// </summary>
    public bool AbandonMission()
    {
        if (_gameState == null || _eventBus == null)
            return false;

        if (_gameState.ActiveMission == null)
            return false;

        var missionId = _gameState.ActiveMission.MissionId;
        if (!_missions.TryGetValue(missionId, out var mission))
            return false;

        // Apply reputation penalty
        if (!string.IsNullOrEmpty(mission.FactionId))
        {
            ApplyReputationChange(mission.FactionId, ReputationModifiers.MissionAbandoned,
                $"Abandoned mission: {mission.Title}");
        }

        mission.Status = MissionStatus.Failed;
        _gameState.ActiveMission = null;

        _logger.LogInformation("Mission abandoned: {MissionId} - {Title}", missionId, mission.Title);

        // Refresh board to fill the slot
        RefreshMissionBoard();

        return true;
    }

    /// <summary>
    /// Manually refresh the mission board
    /// </summary>
    public void RefreshMissionBoard()
    {
        if (_gameState == null || _eventBus == null)
            return;

        _lastBoardRefresh = _gameState.GameTime;

        // Remove expired missions
        var expiredIds = new List<string>();
        foreach (var missionInfo in _gameState.AvailableMissions)
        {
            if (_gameState.GameTime > missionInfo.ExpiryTime)
            {
                expiredIds.Add(missionInfo.MissionId);
            }
        }

        foreach (var id in expiredIds)
        {
            _gameState.AvailableMissions.RemoveAll(m => m.MissionId == id);
            if (_missions.TryGetValue(id, out var mission))
            {
                mission.Status = MissionStatus.Expired;
            }
        }

        _logger.LogDebug("Removed {Count} expired missions", expiredIds.Count);

        // Generate new missions to fill the board
        var currentCount = _gameState.AvailableMissions.Count;
        var needed = Math.Max(MinAvailableMissions - currentCount,
            MaxAvailableMissions - currentCount);

        // Always generate at least enough to reach MinAvailableMissions
        var toGenerate = Math.Max(0, MinAvailableMissions - currentCount);
        // But don't exceed MaxAvailableMissions
        toGenerate = Math.Min(toGenerate, MaxAvailableMissions - currentCount);

        for (int i = 0; i < toGenerate; i++)
        {
            var mission = GenerateMission();
            if (mission != null)
            {
                _missions[mission.MissionId] = mission;
                _gameState.AvailableMissions.Add(mission.ToMissionInfo());

                _eventBus.Publish(new MissionAvailableEvent
                {
                    MissionId = mission.MissionId,
                    Title = mission.Title,
                    Description = mission.Description
                });
            }
        }

        _logger.LogDebug(
            "Mission board refreshed. Generated {Generated} missions. Total available: {Total}",
            toGenerate, _gameState.AvailableMissions.Count);
    }

    // =========================================================================
    // Public API - Objective Tracking
    // =========================================================================

    /// <summary>
    /// Report cargo delivered to a location (for delivery mission tracking)
    /// </summary>
    public void ReportCargoDelivered(string commodityId, int quantity, string locationId)
    {
        if (_gameState?.ActiveMission == null)
            return;

        var missionId = _gameState.ActiveMission.MissionId;
        if (!_missions.TryGetValue(missionId, out var mission))
            return;

        if (mission.Status != MissionStatus.Active)
            return;

        foreach (var objective in mission.Objectives)
        {
            if (objective.Type == MissionObjectiveType.DeliverCargo &&
                objective.TargetId == commodityId &&
                !objective.IsCompleted)
            {
                objective.Current = Math.Min(objective.Required, objective.Current + quantity);
                _logger.LogDebug("Mission {MissionId}: delivery progress {Current}/{Required} for {Commodity}",
                    missionId, objective.Current, objective.Required, commodityId);
            }
        }

        CheckMissionCompletion(mission);
    }

    /// <summary>
    /// Report cargo acquired (for procurement mission tracking)
    /// </summary>
    public void ReportCargoAcquired(string commodityId, int quantity)
    {
        if (_gameState?.ActiveMission == null)
            return;

        var missionId = _gameState.ActiveMission.MissionId;
        if (!_missions.TryGetValue(missionId, out var mission))
            return;

        if (mission.Status != MissionStatus.Active)
            return;

        foreach (var objective in mission.Objectives)
        {
            if (objective.Type == MissionObjectiveType.AcquireCargo &&
                objective.TargetId == commodityId &&
                !objective.IsCompleted)
            {
                objective.Current = Math.Min(objective.Required, objective.Current + quantity);
                _logger.LogDebug("Mission {MissionId}: acquisition progress {Current}/{Required} for {Commodity}",
                    missionId, objective.Current, objective.Required, commodityId);
            }
        }

        CheckMissionCompletion(mission);
    }

    /// <summary>
    /// Report enemies destroyed (for combat mission tracking)
    /// </summary>
    public void ReportEnemyDestroyed(string enemyType, string locationId)
    {
        if (_gameState?.ActiveMission == null)
            return;

        var missionId = _gameState.ActiveMission.MissionId;
        if (!_missions.TryGetValue(missionId, out var mission))
            return;

        if (mission.Status != MissionStatus.Active)
            return;

        foreach (var objective in mission.Objectives)
        {
            if (objective.Type == MissionObjectiveType.DestroyEnemies &&
                (objective.TargetId == enemyType || objective.TargetId == "any_enemy") &&
                !objective.IsCompleted)
            {
                objective.Current = Math.Min(objective.Required, objective.Current + 1);
                _logger.LogDebug("Mission {MissionId}: combat progress {Current}/{Required} ({EnemyType})",
                    missionId, objective.Current, objective.Required, enemyType);
            }
        }

        CheckMissionCompletion(mission);
    }

    /// <summary>
    /// Report location visited (for patrol mission tracking)
    /// </summary>
    public void ReportLocationVisited(string locationId)
    {
        if (_gameState?.ActiveMission == null)
            return;

        var missionId = _gameState.ActiveMission.MissionId;
        if (!_missions.TryGetValue(missionId, out var mission))
            return;

        if (mission.Status != MissionStatus.Active)
            return;

        foreach (var objective in mission.Objectives)
        {
            if (objective.Type == MissionObjectiveType.VisitLocation &&
                objective.TargetId == locationId &&
                !objective.IsCompleted)
            {
                objective.Current = 1; // Visit objectives are binary
                _logger.LogDebug("Mission {MissionId}: visited waypoint {Location}",
                    missionId, locationId);
            }
        }

        CheckMissionCompletion(mission);
    }

    /// <summary>
    /// Check if the active mission is complete and process rewards
    /// </summary>
    public void CheckMissionCompletion()
    {
        if (_gameState?.ActiveMission == null)
            return;

        var missionId = _gameState.ActiveMission.MissionId;
        if (!_missions.TryGetValue(missionId, out var mission))
            return;

        CheckMissionCompletion(mission);
    }

    // =========================================================================
    // Mission Generation
    // =========================================================================

    /// <summary>
    /// Generate a random mission appropriate for the player's current location
    /// </summary>
    private Mission? GenerateMission()
    {
        if (_gameState == null)
            return null;

        var currentLocation = _gameState.CurrentLocation;
        var planet = PlanetRegistry.Get(currentLocation);
        if (planet == null || !planet.HasMissionBoard)
            return null;

        // Determine which faction is issuing the mission
        var issuingFaction = DetermineIssuingFaction(planet);
        if (issuingFaction == null)
            return null;

        // Pick a mission type weighted by faction alignment and location economy
        var missionType = PickMissionType(issuingFaction, planet);

        return missionType switch
        {
            MissionType.Delivery => GenerateDeliveryMission(issuingFaction, planet),
            MissionType.Procurement => GenerateProcurementMission(issuingFaction, planet),
            MissionType.Combat => GenerateCombatMission(issuingFaction, planet),
            MissionType.Exploration => GeneratePatrolMission(issuingFaction, planet),
            _ => GenerateDeliveryMission(issuingFaction, planet)
        };
    }

    /// <summary>
    /// Determine which faction issues missions at this location
    /// </summary>
    private Faction? DetermineIssuingFaction(Planet planet)
    {
        // Primary: the controlling faction
        var primaryFaction = FactionRegistry.Get(planet.FactionId);
        if (primaryFaction != null && primaryFaction.IsMajorFaction)
            return primaryFaction;

        // Secondary: any major faction that controls this system
        var systemFactions = FactionRegistry.GetControllingSystem(planet.SystemName)
            .Where(f => f.IsMajorFaction)
            .ToList();

        if (systemFactions.Count > 0)
            return systemFactions[_rng.Next(systemFactions.Count)];

        // Fallback: any major faction
        var majorFactions = FactionRegistry.GetMajorFactions().ToList();
        if (majorFactions.Count > 0)
            return majorFactions[_rng.Next(majorFactions.Count)];

        return null;
    }

    /// <summary>
    /// Pick a mission type based on faction alignment and location economy
    /// </summary>
    private MissionType PickMissionType(Faction faction, Planet planet)
    {
        // Weight distribution by faction alignment
        var weights = new Dictionary<MissionType, int>
        {
            { MissionType.Delivery, 30 },
            { MissionType.Procurement, 25 },
            { MissionType.Combat, 20 },
            { MissionType.Exploration, 25 }
        };

        // Adjust weights based on faction alignment
        switch (faction.Alignment)
        {
            case FactionAlignment.Militaristic:
                weights[MissionType.Combat] += 20;
                weights[MissionType.Delivery] -= 5;
                weights[MissionType.Exploration] -= 5;
                break;
            case FactionAlignment.Mercantile:
                weights[MissionType.Delivery] += 15;
                weights[MissionType.Procurement] += 10;
                weights[MissionType.Combat] -= 10;
                break;
            case FactionAlignment.Criminal:
                weights[MissionType.Combat] += 10;
                weights[MissionType.Procurement] += 5;
                weights[MissionType.Exploration] -= 5;
                break;
            case FactionAlignment.Scientific:
                weights[MissionType.Exploration] += 10;
                weights[MissionType.Procurement] += 5;
                weights[MissionType.Combat] -= 10;
                break;
            case FactionAlignment.Expansionist:
                weights[MissionType.Exploration] += 15;
                weights[MissionType.Combat] += 5;
                weights[MissionType.Delivery] -= 5;
                break;
            case FactionAlignment.Lawful:
                weights[MissionType.Delivery] += 5;
                weights[MissionType.Combat] -= 5;
                break;
            case FactionAlignment.Libertarian:
                weights[MissionType.Procurement] += 5;
                weights[MissionType.Exploration] += 5;
                break;
        }

        // Adjust based on location security (low security = more combat)
        if (planet.SecurityLevel <= 3)
        {
            weights[MissionType.Combat] += 10;
            weights[MissionType.Delivery] -= 5;
        }

        // Weighted random selection
        var totalWeight = weights.Values.Sum();
        var roll = _rng.Next(totalWeight);
        var cumulative = 0;

        foreach (var (type, weight) in weights)
        {
            cumulative += weight;
            if (roll < cumulative)
                return type;
        }

        return MissionType.Delivery;
    }

    /// <summary>
    /// Generate a delivery mission: transport cargo from source to destination
    /// </summary>
    private Mission GenerateDeliveryMission(Faction faction, Planet sourcePlanet)
    {
        // Pick a commodity the faction favors or that the source location produces
        var commodityId = PickDeliveryCommodity(faction, sourcePlanet);
        var commodity = CommodityRegistry.Get(commodityId);

        // Pick a destination that needs this commodity
        var destination = PickDeliveryDestination(sourcePlanet, commodityId);
        if (destination == null)
            destination = PickRandomDestination(sourcePlanet);
        if (destination == null)
            return null; // No valid destination available

        // Calculate quantity based on cargo capacity and difficulty
        var difficulty = _rng.Next(1, 4);
        var quantity = CalculateDeliveryQuantity(commodity, difficulty);

        // Calculate reward
        var distance = sourcePlanet.GetDistanceTo(destination.Id);
        var reward = CalculateDeliveryReward(commodity, quantity, distance, difficulty, faction);

        // Build mission
        var missionId = GenerateMissionId();
        var title = GenerateDeliveryTitle(commodity, destination);
        var description = GenerateDeliveryDescription(commodity, quantity, sourcePlanet, destination, faction);

        var mission = new Mission
        {
            MissionId = missionId,
            Title = title,
            Description = description,
            Type = MissionType.Delivery,
            Status = MissionStatus.Available,
            FactionId = faction.Id,
            SourceLocation = sourcePlanet.Id,
            DestinationLocation = destination.Id,
            Difficulty = difficulty,
            CreatedTime = _gameState!.GameTime,
            ExpiryTime = _gameState.GameTime.AddHours(48 + difficulty * 24),
            Objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    Type = MissionObjectiveType.DeliverCargo,
                    TargetId = commodityId,
                    Required = quantity,
                    Current = 0
                }
            },
            Reward = new MissionReward
            {
                Credits = reward.credits,
                ReputationDelta = reward.reputationDelta,
                ExperiencePoints = reward.experiencePoints
            }
        };

        return mission;
    }

    /// <summary>
    /// Generate a procurement mission: acquire cargo and deliver to source
    /// </summary>
    private Mission GenerateProcurementMission(Faction faction, Planet sourcePlanet)
    {
        // Pick a commodity the faction needs (not their favored ones)
        var commodityId = PickProcurementCommodity(faction, sourcePlanet);
        var commodity = CommodityRegistry.Get(commodityId);

        var difficulty = _rng.Next(1, 4);
        var quantity = CalculateProcurementQuantity(commodity, difficulty);

        // Reward is higher for procurement (player must spend credits to buy)
        var reward = CalculateProcurementReward(commodity, quantity, difficulty, faction);

        var missionId = GenerateMissionId();
        var title = GenerateProcurementTitle(commodity, quantity);
        var description = GenerateProcurementDescription(commodity, quantity, sourcePlanet, faction);

        var mission = new Mission
        {
            MissionId = missionId,
            Title = title,
            Description = description,
            Type = MissionType.Procurement,
            Status = MissionStatus.Available,
            FactionId = faction.Id,
            SourceLocation = sourcePlanet.Id,
            DestinationLocation = sourcePlanet.Id, // Return to source
            Difficulty = difficulty,
            CreatedTime = _gameState!.GameTime,
            ExpiryTime = _gameState.GameTime.AddHours(72 + difficulty * 24),
            Objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    Type = MissionObjectiveType.AcquireCargo,
                    TargetId = commodityId,
                    Required = quantity,
                    Current = 0
                }
            },
            Reward = new MissionReward
            {
                Credits = reward.credits,
                ReputationDelta = reward.reputationDelta,
                ExperiencePoints = reward.experiencePoints
            }
        };

        return mission;
    }

    /// <summary>
    /// Generate a combat mission: destroy enemies in a target system
    /// </summary>
    private Mission GenerateCombatMission(Faction faction, Planet sourcePlanet)
    {
        var difficulty = _rng.Next(2, 6); // Combat missions are harder
        var enemyIndex = _rng.Next(EnemyTypes.Length);
        var enemyType = EnemyTypes[enemyIndex];
        var enemyName = EnemyDisplayNames[enemyIndex];
        var killCount = 3 + difficulty * 2;

        // Pick a target system (could be current system or a connected one)
        var targetSystem = PickCombatTargetSystem(sourcePlanet, faction);

        var reward = CalculateCombatReward(killCount, difficulty, faction);

        var missionId = GenerateMissionId();
        var title = GenerateCombatTitle(enemyName, killCount, targetSystem);
        var description = GenerateCombatDescription(enemyName, killCount, targetSystem, faction);

        var mission = new Mission
        {
            MissionId = missionId,
            Title = title,
            Description = description,
            Type = MissionType.Combat,
            Status = MissionStatus.Available,
            FactionId = faction.Id,
            SourceLocation = sourcePlanet.Id,
            DestinationLocation = sourcePlanet.Id, // Return to source
            Difficulty = difficulty,
            CreatedTime = _gameState!.GameTime,
            ExpiryTime = _gameState.GameTime.AddHours(36 + difficulty * 12),
            Objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    Type = MissionObjectiveType.DestroyEnemies,
                    TargetId = enemyType,
                    Required = killCount,
                    Current = 0
                }
            },
            Reward = new MissionReward
            {
                Credits = reward.credits,
                ReputationDelta = reward.reputationDelta,
                ExperiencePoints = reward.experiencePoints
            }
        };

        return mission;
    }

    /// <summary>
    /// Generate a patrol mission: visit waypoints and return
    /// </summary>
    private Mission GeneratePatrolMission(Faction faction, Planet sourcePlanet)
    {
        var difficulty = _rng.Next(1, 4);
        var waypointCount = 2 + difficulty;

        // Pick waypoints from connected locations
        var waypoints = PickPatrolWaypoints(sourcePlanet, waypointCount);

        var reward = CalculatePatrolReward(waypoints, sourcePlanet, difficulty, faction);

        var missionId = GenerateMissionId();
        var title = GeneratePatrolTitle(waypoints);
        var description = GeneratePatrolDescription(waypoints, sourcePlanet, faction);

        var objectives = new List<MissionObjective>();
        foreach (var wp in waypoints)
        {
            objectives.Add(new MissionObjective
            {
                Type = MissionObjectiveType.VisitLocation,
                TargetId = wp.Id,
                Required = 1,
                Current = 0
            });
        }

        var mission = new Mission
        {
            MissionId = missionId,
            Title = title,
            Description = description,
            Type = MissionType.Exploration, // Patrol maps to Exploration type
            Status = MissionStatus.Available,
            FactionId = faction.Id,
            SourceLocation = sourcePlanet.Id,
            DestinationLocation = sourcePlanet.Id, // Return to source
            Difficulty = difficulty,
            CreatedTime = _gameState!.GameTime,
            ExpiryTime = _gameState.GameTime.AddHours(48 + difficulty * 24),
            Objectives = objectives,
            Reward = new MissionReward
            {
                Credits = reward.credits,
                ReputationDelta = reward.reputationDelta,
                ExperiencePoints = reward.experiencePoints
            }
        };

        return mission;
    }

    // =========================================================================
    // Mission Completion & Rewards
    // =========================================================================

    /// <summary>
    /// Check if all objectives are complete and process rewards
    /// </summary>
    private void CheckMissionCompletion(Mission mission)
    {
        if (_gameState == null || _eventBus == null)
            return;

        if (mission.Status != MissionStatus.Active)
            return;

        // Check if all objectives are completed
        if (!mission.Objectives.All(o => o.IsCompleted))
            return;

        // Check if player is at the destination (for delivery/return missions)
        if (!string.IsNullOrEmpty(mission.DestinationLocation) &&
            _gameState.CurrentLocation != mission.DestinationLocation)
        {
            // Player needs to return to destination to complete
            return;
        }

        // Mission complete!
        mission.Status = MissionStatus.Completed;
        mission.CompletedTime = _gameState.GameTime;

        // Distribute rewards
        DistributeRewards(mission);

        // Update GameState
        _gameState.ActiveMission = null;
        _gameState.Statistics.MissionsCompleted++;

        // Publish event
        _eventBus.Publish(new MissionCompletedEvent
        {
            MissionId = mission.MissionId,
            Reward = mission.Reward.Credits
        });

        _logger.LogInformation(
            "Mission completed: {MissionId} - {Title}. Reward: {Credits} credits, {Rep} reputation",
            mission.MissionId, mission.Title, mission.Reward.Credits, mission.Reward.ReputationDelta);

        // Refresh board
        RefreshMissionBoard();
    }

    /// <summary>
    /// Distribute mission rewards to the player
    /// </summary>
    private void DistributeRewards(Mission mission)
    {
        if (_gameState == null)
            return;

        // Credits
        _gameState.Credits += mission.Reward.Credits;

        // Reputation
        if (!string.IsNullOrEmpty(mission.FactionId))
        {
            ApplyReputationChange(mission.FactionId, mission.Reward.ReputationDelta,
                $"Completed mission: {mission.Title}");

            // Bonus reputation for the faction that controls the destination
            if (!string.IsNullOrEmpty(mission.DestinationLocation))
            {
                var destPlanet = PlanetRegistry.Get(mission.DestinationLocation);
                if (destPlanet != null && destPlanet.FactionId != mission.FactionId)
                {
                    var bonusRep = mission.Reward.ReputationDelta / 3;
                    if (bonusRep != 0)
                    {
                        ApplyReputationChange(destPlanet.FactionId, bonusRep,
                            $"Delivered to {destPlanet.Name}");
                    }
                }
            }
        }

        // Experience
        // (Experience tracking is in Player model; GameState doesn't have XP directly)
        // We publish credits change event for UI
        _eventBus?.Publish(new CreditsChangedEvent
        {
            PreviousCredits = _gameState.Credits - mission.Reward.Credits,
            NewCredits = _gameState.Credits,
            Delta = mission.Reward.Credits
        });

        // Items (if any)
        if (mission.Reward.Items != null)
        {
            foreach (var (itemId, quantity) in mission.Reward.Items)
            {
                _gameState.AddCargo(itemId, quantity);
            }
        }
    }

    /// <summary>
    /// Apply a reputation change and log it
    /// </summary>
    private void ApplyReputationChange(string factionId, int delta, string reason)
    {
        // Reputation is tracked in the Reputation model (Player.Reputation)
        // For now, we track it through the faction's default reputation
        // In a full implementation, this would use Player.Reputation.ChangeReputation()
        var faction = FactionRegistry.Get(factionId);
        if (faction == null) return;

        _logger.LogDebug("Reputation change: {Faction} {Delta:+0;-0} ({Reason})",
            factionId, delta, reason);

        // Publish a reputation change event for UI systems to consume
        _eventBus?.Publish(new ReputationChangedEvent
        {
            FactionId = factionId,
            Delta = delta,
            Reason = reason
        });
    }

    /// <summary>
    /// Expire missions that have passed their deadline
    /// </summary>
    private void ExpireOverdueMissions()
    {
        if (_gameState == null)
            return;

        // Check active mission
        if (_gameState.ActiveMission != null &&
            _gameState.GameTime > _gameState.ActiveMission.ExpiryTime)
        {
            var missionId = _gameState.ActiveMission.MissionId;
            if (_missions.TryGetValue(missionId, out var mission))
            {
                mission.Status = MissionStatus.Expired;

                // Reputation penalty for letting mission expire
                if (!string.IsNullOrEmpty(mission.FactionId))
                {
                    ApplyReputationChange(mission.FactionId, ReputationModifiers.MissionFailure,
                        $"Mission expired: {mission.Title}");
                }

                _gameState.ActiveMission = null;
                _gameState.Statistics.MissionsFailed++;

                _logger.LogInformation("Active mission expired: {MissionId} - {Title}",
                    missionId, mission.Title);

                RefreshMissionBoard();
            }
        }
    }

    /// <summary>
    /// Handle location changed event - check mission objectives
    /// </summary>
    private void OnLocationChanged(LocationChangedEvent evt)
    {
        if (_gameState?.ActiveMission == null)
            return;

        // Report the new location as visited (for patrol missions)
        ReportLocationVisited(evt.NewLocation);

        // Check if mission is now complete (arrived at destination)
        CheckMissionCompletion();
    }

    /// <summary>
    /// Load existing missions from GameState into internal store
    /// </summary>
    private void LoadMissionsFromGameState()
    {
        if (_gameState == null) return;

        foreach (var missionInfo in _gameState.AvailableMissions)
        {
            if (!_missions.ContainsKey(missionInfo.MissionId))
            {
                // Reconstruct a basic Mission from MissionInfo
                _missions[missionInfo.MissionId] = Mission.FromMissionInfo(missionInfo);
            }
        }

        if (_gameState.ActiveMission != null)
        {
            var missionId = _gameState.ActiveMission.MissionId;
            if (!_missions.ContainsKey(missionId))
            {
                _missions[missionId] = Mission.FromMissionInfo(_gameState.ActiveMission);
                _missions[missionId].Status = MissionStatus.Active;
            }
        }
    }

    // =========================================================================
    // Helper Methods - Commodity Selection
    // =========================================================================

    /// <summary>
    /// Pick a commodity for delivery missions
    /// </summary>
    private string PickDeliveryCommodity(Faction faction, Planet sourcePlanet)
    {
        // Prefer faction's favored commodities
        var candidates = new List<string>();

        if (faction.FavoredCommodities.Count > 0)
        {
            candidates.AddRange(faction.FavoredCommodities);
        }

        // Also consider commodities available at the source location
        if (sourcePlanet.Market != null)
        {
            foreach (var kvp in sourcePlanet.Market.Supply)
            {
                if (kvp.Value > 0 && !candidates.Contains(kvp.Key))
                {
                    candidates.Add(kvp.Key);
                }
            }
        }

        // Fallback: any commodity
        if (candidates.Count == 0)
        {
            candidates.AddRange(CommodityRegistry.All.Select(c => c.Id));
        }

        return candidates[_rng.Next(candidates.Count)];
    }

    /// <summary>
    /// Pick a commodity for procurement missions (something the faction needs)
    /// </summary>
    private string PickProcurementCommodity(Faction faction, Planet sourcePlanet)
    {
        var candidates = new List<string>();

        // Pick commodities the faction does NOT favor (they need to import)
        foreach (var commodity in CommodityRegistry.All)
        {
            if (!faction.IsFavoredCommodity(commodity.Id) &&
                !faction.IsBannedCommodity(commodity.Id))
            {
                candidates.Add(commodity.Id);
            }
        }

        // Also consider commodities in demand at the source location
        if (sourcePlanet.Market != null)
        {
            foreach (var kvp in sourcePlanet.Market.Demand)
            {
                if (kvp.Value > 0 && !candidates.Contains(kvp.Key))
                {
                    candidates.Add(kvp.Key);
                }
            }
        }

        if (candidates.Count == 0)
        {
            candidates.AddRange(CommodityRegistry.All.Select(c => c.Id));
        }

        return candidates[_rng.Next(candidates.Count)];
    }

    // =========================================================================
    // Helper Methods - Destination Selection
    // =========================================================================

    /// <summary>
    /// Pick a destination that needs the given commodity
    /// </summary>
    private Planet? PickDeliveryDestination(Planet sourcePlanet, string commodityId)
    {
        var candidates = new List<Planet>();

        foreach (var planet in PlanetRegistry.All)
        {
            // Skip source and undiscovered locations
            if (planet.Id == sourcePlanet.Id) continue;
            if (!planet.IsDiscovered && planet.Id != sourcePlanet.Id) continue;
            if (!planet.HasCommodityExchange) continue;

            // Prefer locations that demand this commodity
            if (planet.Market != null && planet.Market.Demand.ContainsKey(commodityId))
            {
                candidates.Add(planet);
            }
        }

        if (candidates.Count > 0)
            return candidates[_rng.Next(candidates.Count)];

        // Fallback: any connected location
        if (sourcePlanet.ConnectedLocations.Count > 0)
        {
            var connectedId = sourcePlanet.ConnectedLocations[_rng.Next(sourcePlanet.ConnectedLocations.Count)];
            return PlanetRegistry.Get(connectedId);
        }

        return null;
    }

    /// <summary>
    /// Pick a random destination different from source
    /// </summary>
    private Planet? PickRandomDestination(Planet sourcePlanet)
    {
        var candidates = PlanetRegistry.All
            .Where(p => p.Id != sourcePlanet.Id && p.IsDiscovered)
            .ToList();

        if (candidates.Count > 0)
            return candidates[_rng.Next(candidates.Count)];

        return null;
    }

    /// <summary>
    /// Pick a target system for combat missions
    /// </summary>
    private string PickCombatTargetSystem(Planet sourcePlanet, Faction faction)
    {
        // Prefer systems with low security or enemy faction presence
        var candidates = new HashSet<string> { sourcePlanet.SystemName };

        foreach (var connectedId in sourcePlanet.ConnectedLocations)
        {
            var connected = PlanetRegistry.Get(connectedId);
            if (connected != null)
            {
                candidates.Add(connected.SystemName);
            }
        }

        // Add systems controlled by hostile factions
        foreach (var otherFaction in FactionRegistry.All)
        {
            if (faction.GetRelation(otherFaction.Id) <= -30)
            {
                foreach (var system in otherFaction.TerritorySystems)
                {
                    candidates.Add(system);
                }
            }
        }

        var candidateList = candidates.ToList();
        return candidateList[_rng.Next(candidateList.Count)];
    }

    /// <summary>
    /// Pick waypoints for patrol missions
    /// </summary>
    private List<Planet> PickPatrolWaypoints(Planet sourcePlanet, int count)
    {
        var waypoints = new List<Planet>();
        var visited = new HashSet<string> { sourcePlanet.Id };

        var current = sourcePlanet;
        for (int i = 0; i < count; i++)
        {
            var candidates = current.ConnectedLocations
                .Select(id => PlanetRegistry.Get(id))
                .Where(p => p != null && !visited.Contains(p!.Id))
                .Select(p => p!)
                .ToList();

            if (candidates.Count == 0)
                break;

            var next = candidates[_rng.Next(candidates.Count)];
            waypoints.Add(next);
            visited.Add(next.Id);
            current = next;
        }

        return waypoints;
    }

    // =========================================================================
    // Helper Methods - Quantity & Reward Calculation
    // =========================================================================

    /// <summary>
    /// Calculate delivery quantity based on commodity and difficulty
    /// </summary>
    private int CalculateDeliveryQuantity(Commodity? commodity, int difficulty)
    {
        if (commodity == null) return 5 + difficulty * 3;

        // Smaller quantities for expensive/compact goods, larger for bulk
        var baseQuantity = commodity.MassPerUnit <= 0.5 ? 2 : 5;
        return baseQuantity + difficulty * (commodity.MassPerUnit <= 0.5 ? 1 : 3);
    }

    /// <summary>
    /// Calculate procurement quantity
    /// </summary>
    private int CalculateProcurementQuantity(Commodity? commodity, int difficulty)
    {
        if (commodity == null) return 3 + difficulty * 2;

        var baseQuantity = commodity.MassPerUnit <= 0.5 ? 1 : 3;
        return baseQuantity + difficulty * (commodity.MassPerUnit <= 0.5 ? 1 : 2);
    }

    /// <summary>
    /// Calculate delivery mission reward
    /// </summary>
    private (long credits, int reputationDelta, long experiencePoints) CalculateDeliveryReward(
        Commodity? commodity, int quantity, double distance, int difficulty, Faction faction)
    {
        var baseValue = commodity?.BasePrice ?? 100m;
        var cargoValue = baseValue * quantity;
        var distanceBonus = (long)(distance * 200);
        var difficultyMultiplier = 1.0 + (difficulty - 1) * 0.5;

        var credits = (long)(cargoValue * 0.3m * (decimal)difficultyMultiplier) + distanceBonus;
        credits = Math.Max(500, credits);

        var reputationDelta = ReputationModifiers.MissionSuccess + difficulty;
        var experiencePoints = 100L * difficulty + (long)(distance * 50);

        return (credits, reputationDelta, experiencePoints);
    }

    /// <summary>
    /// Calculate procurement mission reward (higher since player pays for goods)
    /// </summary>
    private (long credits, int reputationDelta, long experiencePoints) CalculateProcurementReward(
        Commodity? commodity, int quantity, int difficulty, Faction faction)
    {
        var baseValue = commodity?.BasePrice ?? 100m;
        var cargoValue = baseValue * quantity;
        var difficultyMultiplier = 1.0 + (difficulty - 1) * 0.5;

        // Procurement pays more since player must acquire goods
        var credits = (long)(cargoValue * 0.6m * (decimal)difficultyMultiplier);
        credits = Math.Max(800, credits);

        var reputationDelta = ReputationModifiers.MissionSuccess + difficulty + 1;
        var experiencePoints = 150L * difficulty;

        return (credits, reputationDelta, experiencePoints);
    }

    /// <summary>
    /// Calculate combat mission reward
    /// </summary>
    private (long credits, int reputationDelta, long experiencePoints) CalculateCombatReward(
        int killCount, int difficulty, Faction faction)
    {
        var credits = 1000L * difficulty + killCount * 500L;
        var reputationDelta = ReputationModifiers.MissionSuccess + difficulty + 2;
        var experiencePoints = 200L * difficulty + killCount * 100L;

        return (credits, reputationDelta, experiencePoints);
    }

    /// <summary>
    /// Calculate patrol mission reward
    /// </summary>
    private (long credits, int reputationDelta, long experiencePoints) CalculatePatrolReward(
        List<Planet> waypoints, Planet sourcePlanet, int difficulty, Faction faction)
    {
        var totalDistance = 0.0;
        var current = sourcePlanet;
        foreach (var wp in waypoints)
        {
            totalDistance += current.GetDistanceTo(wp.Id);
            current = wp;
        }
        // Return trip
        totalDistance += current.GetDistanceTo(sourcePlanet.Id);

        var credits = (long)(totalDistance * 300) + 500L * difficulty;
        var reputationDelta = ReputationModifiers.MissionSuccess + difficulty;
        var experiencePoints = (long)(totalDistance * 100) + 100L * difficulty;

        return (credits, reputationDelta, experiencePoints);
    }

    // =========================================================================
    // Helper Methods - Title & Description Generation
    // =========================================================================

    private string GenerateDeliveryTitle(Commodity? commodity, Planet destination)
    {
        var prefix = DeliveryTitlePrefixes[_rng.Next(DeliveryTitlePrefixes.Length)];
        var commodityName = commodity?.Name ?? "Cargo";
        return $"{prefix} {commodityName} to {destination.Name}";
    }

    private string GenerateDeliveryDescription(
        Commodity? commodity, int quantity, Planet source, Planet destination, Faction faction)
    {
        var commodityName = commodity?.Name ?? "cargo";
        return $"{faction.Name} needs {quantity} units of {commodityName} delivered from {source.Name} to {destination.Name}. " +
               $"Distance: {source.GetDistanceTo(destination.Id):F1} LY. Payment on delivery.";
    }

    private string GenerateProcurementTitle(Commodity? commodity, int quantity)
    {
        var prefix = ProcurementTitlePrefixes[_rng.Next(ProcurementTitlePrefixes.Length)];
        var commodityName = commodity?.Name ?? "Materials";
        return $"{prefix} {quantity}x {commodityName}";
    }

    private string GenerateProcurementDescription(
        Commodity? commodity, int quantity, Planet source, Faction faction)
    {
        var commodityName = commodity?.Name ?? "materials";
        return $"{faction.Name} at {source.Name} requires {quantity} units of {commodityName}. " +
               $"Source them from any market and return for payment. Purchase cost not covered.";
    }

    private string GenerateCombatTitle(string enemyName, int killCount, string targetSystem)
    {
        var prefix = CombatTitlePrefixes[_rng.Next(CombatTitlePrefixes.Length)];
        return $"{prefix} {killCount}x {enemyName} in {targetSystem}";
    }

    private string GenerateCombatDescription(
        string enemyName, int killCount, string targetSystem, Faction faction)
    {
        return $"{faction.Name} has authorized a bounty: eliminate {killCount} {enemyName} in the {targetSystem}. " +
               $"Payment on confirmed kills. Warning: targets are armed and dangerous.";
    }

    private string GeneratePatrolTitle(List<Planet> waypoints)
    {
        var prefix = PatrolTitlePrefixes[_rng.Next(PatrolTitlePrefixes.Length)];
        var route = string.Join(" → ", waypoints.Select(w => w.Name));
        return $"{prefix} {route}";
    }

    private string GeneratePatrolDescription(
        List<Planet> waypoints, Planet source, Faction faction)
    {
        var route = string.Join(" → ", waypoints.Select(w => w.Name));
        return $"{faction.Name} requests a patrol of the following route: {source.Name} → {route} → {source.Name}. " +
               $"Report any hostile activity. Payment on return to {source.Name}.";
    }

    // =========================================================================
    // Helper Methods - Misc
    // =========================================================================

    /// <summary>
    /// Generate a unique mission ID
    /// </summary>
    private static int _missionCounter;
    private static string GenerateMissionId()
    {
        var id = Interlocked.Increment(ref _missionCounter);
        return $"mission_{id}_{DateTime.UtcNow:yyyyMMdd}";
    }
}

// =============================================================================
// Mission Model Classes
// =============================================================================

/// <summary>
/// Rich mission model with objectives, rewards, and tracking
/// </summary>
public sealed class Mission
{
    public string MissionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MissionType Type { get; set; }
    public MissionStatus Status { get; set; } = MissionStatus.Available;
    public string FactionId { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public int Difficulty { get; set; } = 1;
    public DateTime CreatedTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public DateTime? AcceptedTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public List<MissionObjective> Objectives { get; set; } = new();
    public MissionReward Reward { get; set; } = new();

    /// <summary>
    /// Convert to the lightweight MissionInfo used by GameState
    /// </summary>
    public MissionInfo ToMissionInfo()
    {
        return new MissionInfo
        {
            MissionId = MissionId,
            Title = Title,
            Description = Description,
            SourceLocation = SourceLocation,
            DestinationLocation = DestinationLocation,
            CommodityId = Objectives.FirstOrDefault()?.TargetId ?? string.Empty,
            RequiredQuantity = Objectives.FirstOrDefault()?.Required ?? 0,
            Reward = Reward.Credits,
            ExpiryTime = ExpiryTime,
            Type = Type,
            Status = Status
        };
    }

    /// <summary>
    /// Reconstruct a Mission from a MissionInfo (for save/load)
    /// </summary>
    public static Mission FromMissionInfo(MissionInfo info)
    {
        var mission = new Mission
        {
            MissionId = info.MissionId,
            Title = info.Title,
            Description = info.Description,
            Type = info.Type,
            Status = info.Status,
            SourceLocation = info.SourceLocation,
            DestinationLocation = info.DestinationLocation,
            ExpiryTime = info.ExpiryTime,
            Reward = new MissionReward { Credits = info.Reward }
        };

        // Reconstruct basic objective from MissionInfo data
        if (!string.IsNullOrEmpty(info.CommodityId) && info.RequiredQuantity > 0)
        {
            var objectiveType = info.Type switch
            {
                MissionType.Delivery => MissionObjectiveType.DeliverCargo,
                MissionType.Procurement => MissionObjectiveType.AcquireCargo,
                _ => MissionObjectiveType.DeliverCargo
            };

            mission.Objectives.Add(new MissionObjective
            {
                Type = objectiveType,
                TargetId = info.CommodityId,
                Required = info.RequiredQuantity,
                Current = 0
            });
        }

        return mission;
    }
}

/// <summary>
/// A single objective within a mission
/// </summary>
public sealed class MissionObjective
{
    public MissionObjectiveType Type { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public int Required { get; set; }
    public int Current { get; set; }
    public bool IsCompleted => Current >= Required;

    /// <summary>
    /// Get progress as a percentage (0.0 to 1.0)
    /// </summary>
    public float Progress => Required > 0 ? Math.Min(1.0f, (float)Current / Required) : 0f;

    /// <summary>
    /// Get a human-readable description of this objective
    /// </summary>
    public string GetDescription()
    {
        return Type switch
        {
            MissionObjectiveType.DeliverCargo => $"Deliver {Required} {TargetId} ({Current}/{Required})",
            MissionObjectiveType.AcquireCargo => $"Acquire {Required} {TargetId} ({Current}/{Required})",
            MissionObjectiveType.DestroyEnemies => $"Destroy {Required} {TargetId} ({Current}/{Required})",
            MissionObjectiveType.VisitLocation => $"Visit {TargetId} {(IsCompleted ? "✓" : "○")}",
            MissionObjectiveType.SurviveTime => $"Survive for {Required} seconds ({Current}/{Required})",
            _ => $"Complete objective ({Current}/{Required})"
        };
    }
}

/// <summary>
/// Types of mission objectives
/// </summary>
public enum MissionObjectiveType
{
    /// <summary>Deliver cargo to a destination</summary>
    DeliverCargo,

    /// <summary>Acquire cargo (buy or find)</summary>
    AcquireCargo,

    /// <summary>Destroy enemy ships</summary>
    DestroyEnemies,

    /// <summary>Visit a specific location</summary>
    VisitLocation,

    /// <summary>Survive for a duration (escort/defense)</summary>
    SurviveTime
}

/// <summary>
/// Rewards for completing a mission
/// </summary>
public sealed class MissionReward
{
    /// <summary>Credit reward</summary>
    public long Credits { get; set; }

    /// <summary>Reputation change with issuing faction</summary>
    public int ReputationDelta { get; set; }

    /// <summary>Experience points awarded</summary>
    public long ExperiencePoints { get; set; }

    /// <summary>Item rewards (itemId -> quantity)</summary>
    public Dictionary<string, int>? Items { get; set; }
}

/// <summary>
/// Event fired when reputation changes due to mission outcomes
/// </summary>
public sealed record ReputationChangedEvent : GameEvent
{
    public required string FactionId { get; init; }
    public required int Delta { get; init; }
    public required string Reason { get; init; }
}
