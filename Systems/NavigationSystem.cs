using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;

namespace NeonTrader.Systems;

/// <summary>
/// NavigationSystem - handles star map navigation, jump routes between planets,
/// fuel consumption calculation, travel time, and random encounter triggers during travel.
/// Integrates with PlanetRegistry for planet data/jump routes and GameState for location/fuel.
/// </summary>
public sealed class NavigationSystem : IGameSystem
{
    private readonly ILogger<NavigationSystem> _logger;
    private GameState _gameState = null!;
    private IEventBus _eventBus = null!;
    private bool _isRunning;

    // Subscriptions
    private IDisposable? _timeAdvancedSubscription;

    // Travel state
    private bool _isTraveling;
    private string _travelDestination = string.Empty;
    private string _travelOrigin = string.Empty;
    private double _travelDistance;
    private double _travelTotalTimeHours;
    private double _travelElapsedHours;
    private int _travelFuelCost;
    private double _encounterCheckAccumulator;
    private const double EncounterCheckIntervalHours = 1.0;
    private readonly Random _random = new();

    // IGameSystem implementation
    public string SystemId => "NavigationSystem";
    public int Priority => 20;
    public bool IsRunning => _isRunning;

    public NavigationSystem(ILogger<NavigationSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the navigation system. Subscribes to time advancement for travel progress.
    /// </summary>
    public Task InitializeAsync(
        GameState gameState,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        _gameState = gameState;
        _eventBus = eventBus;

        _timeAdvancedSubscription = _eventBus.Subscribe<TimeAdvancedEvent>(OnTimeAdvanced);

        _isRunning = true;
        _logger.LogInformation(
            "NavigationSystem initialized. Current location: {Location}, Fuel: {Fuel}/{Capacity}",
            _gameState.CurrentLocation,
            _gameState.CurrentFuel,
            _gameState.FuelCapacity);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Update - travel progress is driven by TimeAdvancedEvent, not real-time delta.
    /// </summary>
    public Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shutdown the navigation system. Cancels any in-progress travel and cleans up subscriptions.
    /// </summary>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;

        if (_isTraveling)
        {
            _logger.LogWarning("Shutdown during travel from {Origin} to {Destination}",
                _travelOrigin, _travelDestination);
            CancelTravelInternal("Game shutdown");
        }

        _timeAdvancedSubscription?.Dispose();
        _logger.LogInformation("NavigationSystem shutdown");
        await Task.CompletedTask;
    }

    // ─── Public API ───────────────────────────────────────────────

    /// <summary>
    /// Whether the player is currently in transit between locations.
    /// </summary>
    public bool IsTraveling => _isTraveling;

    /// <summary>
    /// The destination planet ID being traveled to (empty if not traveling).
    /// </summary>
    public string CurrentDestination => _travelDestination;

    /// <summary>
    /// The origin planet ID the travel started from (empty if not traveling).
    /// </summary>
    public string CurrentOrigin => _travelOrigin;

    /// <summary>
    /// Travel progress from 0.0 (just departed) to 1.0 (arrived).
    /// </summary>
    public double TravelProgress => _isTraveling
        ? Math.Clamp(_travelElapsedHours / _travelTotalTimeHours, 0.0, 1.0)
        : 0.0;

    /// <summary>
    /// Remaining travel time in game hours.
    /// </summary>
    public double TravelTimeRemainingHours => _isTraveling
        ? Math.Max(0, _travelTotalTimeHours - _travelElapsedHours)
        : 0.0;

    /// <summary>
    /// Total travel distance in light-years for the current jump.
    /// </summary>
    public double CurrentJumpDistance => _travelDistance;

    /// <summary>
    /// Fuel cost for the current jump.
    /// </summary>
    public int CurrentJumpFuelCost => _travelFuelCost;

    /// <summary>
    /// Gets all available jump destinations from the current location.
    /// Only returns discovered locations by default.
    /// </summary>
    /// <param name="includeUndiscovered">If true, includes undiscovered locations</param>
    /// <returns>List of reachable planet IDs with basic info</returns>
    public List<JumpDestination> GetAvailableDestinations(bool includeUndiscovered = false)
    {
        var results = new List<JumpDestination>();
        var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);

        if (currentPlanet == null)
        {
            _logger.LogWarning("Current location '{Location}' not found in PlanetRegistry",
                _gameState.CurrentLocation);
            return results;
        }

        var ship = GetPlayerShip();

        foreach (var connectedId in currentPlanet.ConnectedLocations)
        {
            var destination = PlanetRegistry.Get(connectedId);
            if (destination == null) continue;

            // Filter undiscovered unless explicitly included
            if (!includeUndiscovered && !destination.IsDiscovered) continue;

            var distance = currentPlanet.GetDistanceTo(connectedId);
            var fuelCost = CalculateFuelCost(distance, ship);
            var travelTimeHours = CalculateTravelTime(distance, ship);

            results.Add(new JumpDestination
            {
                PlanetId = connectedId,
                PlanetName = destination.Name,
                PlanetType = destination.Type,
                SystemName = destination.SystemName,
                DistanceLY = distance,
                FuelCost = fuelCost,
                TravelTimeHours = travelTimeHours,
                DangerLevel = destination.TravelDanger,
                SecurityLevel = destination.SecurityLevel,
                FactionId = destination.FactionId,
                HasShipyard = destination.HasShipyard,
                HasOutfitter = destination.HasOutfitter,
                HasMarket = destination.HasCommodityExchange,
                IsDiscovered = destination.IsDiscovered
            });
        }

        // Sort by distance (closest first)
        results.Sort((a, b) => a.DistanceLY.CompareTo(b.DistanceLY));
        return results;
    }

    /// <summary>
    /// Gets detailed jump information for a specific destination.
    /// </summary>
    /// <param name="destinationId">Target planet ID</param>
    /// <returns>Jump info or null if destination is unreachable</returns>
    public JumpInfo? GetJumpInfo(string destinationId)
    {
        var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);
        var destination = PlanetRegistry.Get(destinationId);

        if (currentPlanet == null || destination == null)
            return null;

        if (!currentPlanet.IsConnectedTo(destinationId))
            return null;

        var ship = GetPlayerShip();
        var distance = currentPlanet.GetDistanceTo(destinationId);
        var fuelCost = CalculateFuelCost(distance, ship);
        var travelTimeHours = CalculateTravelTime(distance, ship);
        var canJump = CanJumpTo(destinationId);

        return new JumpInfo
        {
            OriginId = _gameState.CurrentLocation,
            OriginName = currentPlanet.Name,
            DestinationId = destinationId,
            DestinationName = destination.Name,
            DestinationType = destination.Type,
            DestinationSystem = destination.SystemName,
            DistanceLY = distance,
            FuelCost = fuelCost,
            FuelAvailable = _gameState.CurrentFuel,
            TravelTimeHours = travelTimeHours,
            DangerLevel = destination.TravelDanger,
            SecurityLevel = destination.SecurityLevel,
            FactionId = destination.FactionId,
            CanJump = canJump,
            BlockReason = canJump ? null : GetBlockReason(destinationId, fuelCost)
        };
    }

    /// <summary>
    /// Checks whether the player can jump to the specified destination.
    /// </summary>
    /// <param name="destinationId">Target planet ID</param>
    /// <returns>True if the jump is possible</returns>
    public bool CanJumpTo(string destinationId)
    {
        if (_isTraveling)
            return false;

        var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);
        if (currentPlanet == null)
            return false;

        if (!currentPlanet.IsConnectedTo(destinationId))
            return false;

        var destination = PlanetRegistry.Get(destinationId);
        if (destination == null)
            return false;

        var ship = GetPlayerShip();
        var distance = currentPlanet.GetDistanceTo(destinationId);
        var fuelCost = CalculateFuelCost(distance, ship);

        if (_gameState.CurrentFuel < fuelCost)
            return false;

        return true;
    }

    /// <summary>
    /// Initiates a jump to the specified destination.
    /// Consumes fuel, sets travel state, and publishes TravelStartedEvent.
    /// </summary>
    /// <param name="destinationId">Target planet ID</param>
    /// <returns>True if the jump was started successfully</returns>
    public bool StartJump(string destinationId)
    {
        if (!CanJumpTo(destinationId))
        {
            _logger.LogWarning("Cannot jump to {Destination}: blocked or insufficient resources",
                destinationId);
            return false;
        }

        var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);
        var destination = PlanetRegistry.Get(destinationId);
        if (currentPlanet == null || destination == null)
            return false;

        var ship = GetPlayerShip();
        var distance = currentPlanet.GetDistanceTo(destinationId);
        var fuelCost = CalculateFuelCost(distance, ship);
        var travelTimeHours = CalculateTravelTime(distance, ship);

        // Consume fuel
        _gameState.CurrentFuel -= fuelCost;

        // Set travel state
        _isTraveling = true;
        _travelOrigin = _gameState.CurrentLocation;
        _travelDestination = destinationId;
        _travelDistance = distance;
        _travelTotalTimeHours = travelTimeHours;
        _travelElapsedHours = 0;
        _travelFuelCost = fuelCost;
        _encounterCheckAccumulator = 0;

        _logger.LogInformation(
            "Jump initiated: {Origin} -> {Destination} | Distance: {Distance:F1} LY | " +
            "Fuel: {FuelCost} | ETA: {ETA:F1} hours",
            currentPlanet.Name, destination.Name, distance, fuelCost, travelTimeHours);

        _eventBus.Publish(new TravelStartedEvent
        {
            OriginId = _travelOrigin,
            OriginName = currentPlanet.Name,
            DestinationId = destinationId,
            DestinationName = destination.Name,
            DistanceLY = distance,
            FuelCost = fuelCost,
            TravelTimeHours = travelTimeHours,
            DangerLevel = destination.TravelDanger
        });

        return true;
    }

    /// <summary>
    /// Cancels an in-progress jump, returning the player to the origin.
    /// Partial fuel is not refunded.
    /// </summary>
    /// <returns>True if a jump was canceled</returns>
    public bool CancelJump()
    {
        if (!_isTraveling) return false;
        CancelTravelInternal("Player canceled");
        return true;
    }

    /// <summary>
    /// Calculates the fuel cost for a jump of the given distance.
    /// Uses the ship's FuelConsumption rate (fuel per light-year).
    /// Falls back to the planet's pre-calculated FuelCosts if no ship data is available.
    /// </summary>
    /// <param name="distanceLY">Distance in light-years</param>
    /// <param name="ship">Player's ship (nullable)</param>
    /// <returns>Fuel units required</returns>
    public int CalculateFuelCost(double distanceLY, Ship? ship = null)
    {
        ship ??= GetPlayerShip();

        if (ship != null)
        {
            // Dynamic calculation: distance * fuel consumption rate
            return (int)Math.Ceiling(distanceLY * ship.FuelConsumption);
        }

        // Fallback: use planet's pre-calculated fuel cost
        var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);
        if (currentPlanet != null)
        {
            // This is a rough fallback - caller should provide destinationId for accuracy
            return (int)Math.Ceiling(distanceLY * 1.0); // Default consumption rate
        }

        return (int)Math.Ceiling(distanceLY);
    }

    /// <summary>
    /// Calculates travel time for a jump of the given distance.
    /// Travel time = distance / speed in game hours.
    /// </summary>
    /// <param name="distanceLY">Distance in light-years</param>
    /// <param name="ship">Player's ship (nullable)</param>
    /// <returns>Travel time in game hours</returns>
    public double CalculateTravelTime(double distanceLY, Ship? ship = null)
    {
        ship ??= GetPlayerShip();

        var speed = ship?.MaxSpeed ?? 100.0; // Default speed if no ship data
        if (speed <= 0) speed = 100.0;

        // Base travel time: distance / speed (hours per light-year at max speed)
        // With a minimum of 0.5 hours for very short jumps
        var baseTime = distanceLY / speed;
        return Math.Max(0.5, baseTime);
    }

    /// <summary>
    /// Gets the encounter risk for a route, factoring in danger level and security.
    /// Returns a value from 0.0 (safe) to 1.0 (extremely dangerous).
    /// </summary>
    /// <param name="destinationId">Target planet ID</param>
    /// <returns>Risk factor 0.0-1.0</returns>
    public double GetEncounterRisk(string destinationId)
    {
        var destination = PlanetRegistry.Get(destinationId);
        if (destination == null) return 0.5;

        // Danger contributes positively, security negatively
        var dangerFactor = destination.TravelDanger / 100.0; // 0.0 to 1.0
        var securityFactor = 1.0 - (destination.SecurityLevel / 10.0); // 1.0 to 0.0

        // Combined risk: average of danger and (lack of) security
        return Math.Clamp((dangerFactor + securityFactor) / 2.0, 0.0, 1.0);
    }

    // ─── Event Handlers ───────────────────────────────────────────

    /// <summary>
    /// Handles time advancement to progress travel and trigger encounters.
    /// </summary>
    private void OnTimeAdvanced(TimeAdvancedEvent evt)
    {
        if (!_isRunning || !_isTraveling) return;

        var gameHoursAdvanced = evt.DeltaTime.TotalHours;

        // Advance travel progress
        _travelElapsedHours += gameHoursAdvanced;

        // Check for random encounters
        _encounterCheckAccumulator += gameHoursAdvanced;
        while (_encounterCheckAccumulator >= EncounterCheckIntervalHours)
        {
            _encounterCheckAccumulator -= EncounterCheckIntervalHours;
            CheckForEncounter();
        }

        // Check if travel is complete
        if (_travelElapsedHours >= _travelTotalTimeHours)
        {
            CompleteTravel();
        }
    }

    // ─── Private Helpers ──────────────────────────────────────────

    /// <summary>
    /// Completes the current travel: updates GameState location, publishes events.
    /// </summary>
    private void CompleteTravel()
    {
        var originId = _travelOrigin;
        var destinationId = _travelDestination;
        var distance = _travelDistance;
        var elapsedHours = _travelElapsedHours;

        // Update game state
        _gameState.PreviousLocation = originId;
        _gameState.CurrentLocation = destinationId;

        // Update statistics
        _gameState.Statistics.DistanceTraveled += (int)Math.Ceiling(distance);

        // Mark destination as discovered
        var destination = PlanetRegistry.Get(destinationId);
        if (destination != null && !destination.IsDiscovered)
        {
            destination.IsDiscovered = true;
            _logger.LogInformation("Discovered new location: {Name} ({Id})",
                destination.Name, destinationId);
        }

        // Reset travel state
        _isTraveling = false;
        _travelDestination = string.Empty;
        _travelOrigin = string.Empty;
        _travelDistance = 0;
        _travelTotalTimeHours = 0;
        _travelElapsedHours = 0;
        _travelFuelCost = 0;
        _encounterCheckAccumulator = 0;

        _logger.LogInformation(
            "Travel complete: {Origin} -> {Destination} | Distance: {Distance:F1} LY | " +
            "Duration: {Duration:F1} hours | Fuel remaining: {Fuel}",
            originId, destinationId, distance, elapsedHours, _gameState.CurrentFuel);

        // Publish events
        _eventBus.Publish(new LocationChangedEvent
        {
            PreviousLocation = originId,
            NewLocation = destinationId
        });

        _eventBus.Publish(new TravelCompletedEvent
        {
            OriginId = originId,
            DestinationId = destinationId,
            DistanceLY = distance,
            TravelTimeHours = elapsedHours,
            FuelConsumed = _travelFuelCost,
            FuelRemaining = _gameState.CurrentFuel
        });

        _eventBus.Publish(new RefreshUIEvent());
    }

    /// <summary>
    /// Checks for a random encounter during travel based on danger and security levels.
    /// </summary>
    private void CheckForEncounter()
    {
        var destination = PlanetRegistry.Get(_travelDestination);
        if (destination == null) return;

        var risk = GetEncounterRisk(_travelDestination);

        // Base encounter chance per check interval, scaled by risk
        var baseChance = 0.15; // 15% base chance per game hour
        var encounterChance = baseChance * risk;

        if (_random.NextDouble() >= encounterChance)
            return; // No encounter this check

        // Determine encounter type based on danger and security
        var encounterType = RollEncounterType(destination);
        var encounter = GenerateEncounter(encounterType, destination);

        _logger.LogInformation(
            "Travel encounter! Type: {Type}, Description: {Description}",
            encounterType, encounter.Description);

        _eventBus.Publish(new TravelEncounterEvent
        {
            EncounterType = encounterType,
            Description = encounter.Description,
            OriginId = _travelOrigin,
            DestinationId = _travelDestination,
            TravelProgress = TravelProgress,
            DamageTaken = encounter.DamageTaken,
            CreditsLost = encounter.CreditsLost,
            CargoLost = encounter.CargoLost
        });

        // Apply encounter effects
        ApplyEncounterEffects(encounter);
    }

    /// <summary>
    /// Rolls for an encounter type weighted by destination characteristics.
    /// </summary>
    private EncounterType RollEncounterType(Planet destination)
    {
        var roll = _random.NextDouble();
        var danger = destination.TravelDanger / 100.0;
        var security = destination.SecurityLevel / 10.0;

        // Weight distributions based on danger and security
        var pirateWeight = 0.25 + (danger * 0.3) - (security * 0.15);
        var patrolWeight = 0.15 + (security * 0.2) - (danger * 0.1);
        var derelictWeight = 0.15 + (danger * 0.1);
        var anomalyWeight = 0.10;
        var smugglerWeight = 0.10 + (danger * 0.1);
        var distressWeight = 0.10;
        var nothingWeight = 0.15;

        // Normalize
        var total = pirateWeight + patrolWeight + derelictWeight +
                    anomalyWeight + smugglerWeight + distressWeight + nothingWeight;

        var cumulative = 0.0;
        cumulative += pirateWeight / total;
        if (roll < cumulative) return EncounterType.Pirates;

        cumulative += patrolWeight / total;
        if (roll < cumulative) return EncounterType.Patrol;

        cumulative += derelictWeight / total;
        if (roll < cumulative) return EncounterType.Derelict;

        cumulative += anomalyWeight / total;
        if (roll < cumulative) return EncounterType.Anomaly;

        cumulative += smugglerWeight / total;
        if (roll < cumulative) return EncounterType.Smugglers;

        cumulative += distressWeight / total;
        if (roll < cumulative) return EncounterType.DistressSignal;

        return EncounterType.Nothing;
    }

    /// <summary>
    /// Generates a specific encounter based on type and destination context.
    /// </summary>
    private TravelEncounter GenerateEncounter(EncounterType type, Planet destination)
    {
        return type switch
        {
            EncounterType.Pirates => new TravelEncounter
            {
                EncounterType = type,
                Description = $"Pirates ambush you en route to {destination.Name}! " +
                              "They demand cargo or credits.",
                DamageTaken = _random.Next(5, 25),
                CreditsLost = _random.Next(500, 5000),
                CargoLost = _random.Next(0, 10)
            },
            EncounterType.Patrol => new TravelEncounter
            {
                EncounterType = type,
                Description = $"A {destination.FactionId} patrol ship intercepts you near {destination.Name}. " +
                              "They scan your cargo for contraband.",
                DamageTaken = 0,
                CreditsLost = _random.Next(0, 500), // Possible fine
                CargoLost = _random.Next(0, 3) // Possible contraband confiscation
            },
            EncounterType.Derelict => new TravelEncounter
            {
                EncounterType = type,
                Description = $"You detect a derelict vessel drifting near the route to {destination.Name}. " +
                              "Salvage opportunity detected.",
                DamageTaken = _random.Next(0, 10), // Minor hazard damage
                CreditsLost = 0,
                CargoLost = 0
            },
            EncounterType.Anomaly => new TravelEncounter
            {
                EncounterType = type,
                Description = $"A spatial anomaly disrupts your jump to {destination.Name}! " +
                              "Systems flicker and shields take strain.",
                DamageTaken = _random.Next(10, 40),
                CreditsLost = 0,
                CargoLost = 0
            },
            EncounterType.Smugglers => new TravelEncounter
            {
                EncounterType = type,
                Description = $"Smugglers hail you near {destination.Name}. " +
                              "They offer black market goods at discounted prices.",
                DamageTaken = 0,
                CreditsLost = 0,
                CargoLost = 0
            },
            EncounterType.DistressSignal => new TravelEncounter
            {
                EncounterType = type,
                Description = $"You receive a distress signal from a stranded ship near {destination.Name}. " +
                              "A potential rescue mission.",
                DamageTaken = 0,
                CreditsLost = 0,
                CargoLost = 0
            },
            _ => new TravelEncounter
            {
                EncounterType = EncounterType.Nothing,
                Description = "Nothing unusual happens during this leg of the journey.",
                DamageTaken = 0,
                CreditsLost = 0,
                CargoLost = 0
            }
        };
    }

    /// <summary>
    /// Applies the effects of a travel encounter to the game state.
    /// </summary>
    private void ApplyEncounterEffects(TravelEncounter encounter)
    {
        if (encounter.DamageTaken > 0)
        {
            _eventBus.Publish(new PlayerDamagedEvent
            {
                Damage = encounter.DamageTaken,
                RemainingHealth = _gameState.Health,
                Source = $"Travel encounter: {encounter.EncounterType}"
            });
        }

        if (encounter.CreditsLost > 0)
        {
            var previousCredits = _gameState.Credits;
            _gameState.Credits = Math.Max(0, _gameState.Credits - encounter.CreditsLost);
            _eventBus.Publish(new CreditsChangedEvent
            {
                PreviousCredits = previousCredits,
                NewCredits = _gameState.Credits,
                Delta = -(encounter.CreditsLost)
            });
        }

        if (encounter.CargoLost > 0 && _gameState.Cargo.Count > 0)
        {
            // Lose random cargo items
            var cargoKeys = _gameState.Cargo.Keys.ToList();
            var lostCount = Math.Min(encounter.CargoLost, cargoKeys.Count);
            for (int i = 0; i < lostCount; i++)
            {
                var key = cargoKeys[_random.Next(cargoKeys.Count)];
                if (_gameState.Cargo.TryGetValue(key, out var qty) && qty > 0)
                {
                    var lostQty = Math.Min(_random.Next(1, 4), qty);
                    _gameState.RemoveCargo(key, lostQty);
                    cargoKeys.Remove(key); // Don't hit the same commodity twice
                }
            }
        }
    }

    /// <summary>
    /// Internal travel cancellation with reason.
    /// </summary>
    private void CancelTravelInternal(string reason)
    {
        var origin = _travelOrigin;
        var destination = _travelDestination;
        var progress = TravelProgress;

        _isTraveling = false;
        _travelDestination = string.Empty;
        _travelOrigin = string.Empty;
        _travelDistance = 0;
        _travelTotalTimeHours = 0;
        _travelElapsedHours = 0;
        _travelFuelCost = 0;
        _encounterCheckAccumulator = 0;

        _logger.LogWarning(
            "Travel interrupted: {Origin} -> {Destination} | Progress: {Progress:P0} | Reason: {Reason}",
            origin, destination, progress, reason);

        _eventBus.Publish(new TravelInterruptedEvent
        {
            OriginId = origin,
            DestinationId = destination,
            Progress = progress,
            Reason = reason
        });
    }

    /// <summary>
    /// Gets the reason why a jump is blocked.
    /// </summary>
    private string? GetBlockReason(string destinationId, int fuelCost)
    {
        if (_isTraveling)
            return "Already in transit";

        var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);
        if (currentPlanet == null)
            return "Current location unknown";

        if (!currentPlanet.IsConnectedTo(destinationId))
            return "No jump route available";

        if (_gameState.CurrentFuel < fuelCost)
            return $"Insufficient fuel (need {fuelCost}, have {_gameState.CurrentFuel})";

        return null;
    }

    /// <summary>
    /// Gets the player's ship from ShipRegistry using GameState.ShipId.
    /// </summary>
    private Ship? GetPlayerShip()
    {
        if (string.IsNullOrEmpty(_gameState.ShipId))
            return null;

        return ShipRegistry.Get(_gameState.ShipId);
    }
}

// ─── Navigation Data Types ────────────────────────────────────────

/// <summary>
/// Represents a reachable jump destination from the current location.
/// </summary>
public sealed class JumpDestination
{
    /// <summary>Planet ID</summary>
    public string PlanetId { get; init; } = string.Empty;

    /// <summary>Display name</summary>
    public string PlanetName { get; init; } = string.Empty;

    /// <summary>Planet type</summary>
    public PlanetType PlanetType { get; init; }

    /// <summary>Star system name</summary>
    public string SystemName { get; init; } = string.Empty;

    /// <summary>Distance in light-years</summary>
    public double DistanceLY { get; init; }

    /// <summary>Fuel units required</summary>
    public int FuelCost { get; init; }

    /// <summary>Travel time in game hours</summary>
    public double TravelTimeHours { get; init; }

    /// <summary>Danger level (0-100)</summary>
    public int DangerLevel { get; init; }

    /// <summary>Security level (0-10)</summary>
    public int SecurityLevel { get; init; }

    /// <summary>Controlling faction ID</summary>
    public string FactionId { get; init; } = string.Empty;

    /// <summary>Whether this location has a shipyard</summary>
    public bool HasShipyard { get; init; }

    /// <summary>Whether this location has an outfitter</summary>
    public bool HasOutfitter { get; init; }

    /// <summary>Whether this location has a commodity exchange</summary>
    public bool HasMarket { get; init; }

    /// <summary>Whether this location is discovered</summary>
    public bool IsDiscovered { get; init; }
}

/// <summary>
/// Detailed jump information for a specific destination.
/// </summary>
public sealed class JumpInfo
{
    public string OriginId { get; init; } = string.Empty;
    public string OriginName { get; init; } = string.Empty;
    public string DestinationId { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public PlanetType DestinationType { get; init; }
    public string DestinationSystem { get; init; } = string.Empty;
    public double DistanceLY { get; init; }
    public int FuelCost { get; init; }
    public int FuelAvailable { get; init; }
    public double TravelTimeHours { get; init; }
    public int DangerLevel { get; init; }
    public int SecurityLevel { get; init; }
    public string FactionId { get; init; } = string.Empty;
    public bool CanJump { get; init; }
    public string? BlockReason { get; init; }
}

/// <summary>
/// Internal encounter data used during encounter generation.
/// </summary>
internal sealed class TravelEncounter
{
    public EncounterType EncounterType { get; init; }
    public string Description { get; init; } = string.Empty;
    public int DamageTaken { get; init; }
    public int CreditsLost { get; init; }
    public int CargoLost { get; init; }
}

// ─── Travel Events ────────────────────────────────────────────────

/// <summary>
/// Types of encounters that can occur during travel.
/// </summary>
public enum EncounterType
{
    /// <summary>No encounter</summary>
    Nothing,

    /// <summary>Pirate attack - combat or tribute</summary>
    Pirates,

    /// <summary>Faction patrol - cargo scan, possible fines</summary>
    Patrol,

    /// <summary>Derelict vessel - salvage opportunity</summary>
    Derelict,

    /// <summary>Spatial anomaly - environmental hazard</summary>
    Anomaly,

    /// <summary>Smuggler contact - black market opportunity</summary>
    Smugglers,

    /// <summary>Distress signal - rescue mission opportunity</summary>
    DistressSignal
}

/// <summary>
/// Event fired when a jump between locations begins.
/// </summary>
public sealed record TravelStartedEvent : GameEvent
{
    public required string OriginId { get; init; }
    public required string OriginName { get; init; }
    public required string DestinationId { get; init; }
    public required string DestinationName { get; init; }
    public required double DistanceLY { get; init; }
    public required int FuelCost { get; init; }
    public required double TravelTimeHours { get; init; }
    public required int DangerLevel { get; init; }
}

/// <summary>
/// Event fired when a jump is completed successfully.
/// </summary>
public sealed record TravelCompletedEvent : GameEvent
{
    public required string OriginId { get; init; }
    public required string DestinationId { get; init; }
    public required double DistanceLY { get; init; }
    public required double TravelTimeHours { get; init; }
    public required int FuelConsumed { get; init; }
    public required int FuelRemaining { get; init; }
}

/// <summary>
/// Event fired when a random encounter occurs during travel.
/// </summary>
public sealed record TravelEncounterEvent : GameEvent
{
    public required EncounterType EncounterType { get; init; }
    public required string Description { get; init; }
    public required string OriginId { get; init; }
    public required string DestinationId { get; init; }
    public required double TravelProgress { get; init; }
    public int DamageTaken { get; init; }
    public int CreditsLost { get; init; }
    public int CargoLost { get; init; }
}

/// <summary>
/// Event fired when travel is interrupted before completion.
/// </summary>
public sealed record TravelInterruptedEvent : GameEvent
{
    public required string OriginId { get; init; }
    public required string DestinationId { get; init; }
    public required double Progress { get; init; }
    public required string Reason { get; init; }
}
