using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;

namespace NeonTrader.Systems;

/// <summary>
/// CombatSystem - handles ship-to-ship combat mechanics including weapon firing,
/// shield absorption, hull damage calculation, enemy AI, flee mechanics, and combat rewards.
/// </summary>
public sealed class CombatSystem : IGameSystem
{
    private readonly ILogger<CombatSystem> _logger;
    private GameState? _gameState;
    private IEventBus? _eventBus;
    private bool _isRunning;
    private readonly Random _rng = new();

    /// <summary>
    /// Active combat encounters keyed by encounter ID
    /// </summary>
    private readonly Dictionary<string, CombatEncounter> _encounters = new();

    /// <summary>
    /// Unique system identifier
    /// </summary>
    public string SystemId => "CombatSystem";

    /// <summary>
    /// Priority 30 - runs after core systems (DataLoader=0, SaveSystem=20)
    /// </summary>
    public int Priority => 30;

    /// <summary>
    /// Whether the system is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    public CombatSystem(ILogger<CombatSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the combat system
    /// </summary>
    public Task InitializeAsync(
        GameState gameState,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        _gameState = gameState;
        _eventBus = eventBus;
        _isRunning = true;

        _logger.LogInformation("CombatSystem initialized");

        _eventBus.Publish(new SystemInitializedEvent { SystemId = SystemId });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Update combat encounters each frame
    /// </summary>
    public Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _gameState == null || _eventBus == null)
            return Task.CompletedTask;

        // Process each active encounter
        var completedEncounters = new List<string>();

        foreach (var (encounterId, encounter) in _encounters)
        {
            if (encounter.State == CombatState.Resolved)
            {
                completedEncounters.Add(encounterId);
                continue;
            }

            UpdateEncounter(encounter, deltaTime);
        }

        // Clean up resolved encounters
        foreach (var id in completedEncounters)
        {
            _encounters.Remove(id);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shutdown the combat system
    /// </summary>
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        _encounters.Clear();
        _logger.LogInformation("CombatSystem shutdown");

        _eventBus?.Publish(new SystemShutdownEvent { SystemId = SystemId });

        return Task.CompletedTask;
    }

    // ─── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates a combat encounter between the player and an enemy ship.
    /// Returns the encounter ID for tracking.
    /// </summary>
    public string StartEncounter(Ship enemyShip, string enemyName, string location)
    {
        if (_gameState == null || _eventBus == null)
            throw new InvalidOperationException("CombatSystem not initialized");

        var playerShip = GetPlayerShip();
        if (playerShip == null)
            throw new InvalidOperationException("Player has no ship");

        var encounterId = $"encounter_{Guid.NewGuid():N}";
        var encounter = new CombatEncounter
        {
            Id = encounterId,
            PlayerShip = playerShip,
            EnemyShip = enemyShip,
            EnemyName = enemyName,
            Location = location,
            State = CombatState.Approaching,
            Distance = GetInitialEngagementDistance(playerShip, enemyShip),
            TimeSinceLastPlayerShot = 0,
            TimeSinceLastEnemyShot = 0,
            PlayerFleeAttempts = 0,
            EnemyFleeAttempts = 0
        };

        _encounters[encounterId] = encounter;

        _logger.LogInformation(
            "Combat encounter {EncounterId} started: Player vs {EnemyName} at {Location}",
            encounterId, enemyName, location);

        _eventBus.Publish(new CombatEncounterStartedEvent
        {
            EncounterId = encounterId,
            EnemyName = enemyName,
            EnemyShipClassId = enemyShip.ShipClassId,
            Location = location
        });

        return encounterId;
    }

    /// <summary>
    /// Gets the current state of a combat encounter
    /// </summary>
    public CombatEncounter? GetEncounter(string encounterId)
    {
        _encounters.TryGetValue(encounterId, out var encounter);
        return encounter;
    }

    /// <summary>
    /// Gets all active encounters
    /// </summary>
    public IReadOnlyCollection<CombatEncounter> ActiveEncounters => _encounters.Values;

    /// <summary>
    /// Player attempts to fire weapons at the enemy in an encounter
    /// </summary>
    public CombatActionResult PlayerFireWeapons(string encounterId)
    {
        if (!_encounters.TryGetValue(encounterId, out var encounter))
            return CombatActionResult.Failed("Encounter not found");

        if (encounter.State != CombatState.Engaged)
            return CombatActionResult.Failed("Not in engagement range");

        return FireWeapons(encounter.PlayerShip, encounter.EnemyShip, encounter.Distance, isPlayer: true);
    }

    /// <summary>
    /// Player attempts to flee from combat
    /// </summary>
    public CombatActionResult PlayerFlee(string encounterId)
    {
        if (!_encounters.TryGetValue(encounterId, out var encounter))
            return CombatActionResult.Failed("Encounter not found");

        if (encounter.State == CombatState.Resolved)
            return CombatActionResult.Failed("Encounter already resolved");

        return AttemptFlee(encounter, isPlayer: true);
    }

    // ─── Core Combat Logic ─────────────────────────────────────────────────

    /// <summary>
    /// Updates a single combat encounter for one frame
    /// </summary>
    private void UpdateEncounter(CombatEncounter encounter, float deltaTime)
    {
        switch (encounter.State)
        {
            case CombatState.Approaching:
                UpdateApproach(encounter, deltaTime);
                break;
            case CombatState.Engaged:
                UpdateEngagement(encounter, deltaTime);
                break;
            case CombatState.Disengaging:
                UpdateDisengage(encounter, deltaTime);
                break;
        }
    }

    /// <summary>
    /// Ships close distance until in weapon range
    /// </summary>
    private void UpdateApproach(CombatEncounter encounter, float deltaTime)
    {
        var closingSpeed = encounter.PlayerShip.MaxSpeed + encounter.EnemyShip.MaxSpeed;
        encounter.Distance -= closingSpeed * deltaTime;

        if (encounter.Distance <= GetMaxWeaponRange(encounter.PlayerShip))
        {
            encounter.State = CombatState.Engaged;
            encounter.Distance = Math.Max(0, encounter.Distance);

            _logger.LogInformation("Encounter {Id}: Engaged at distance {Dist:F0}m",
                encounter.Id, encounter.Distance);

            _eventBus?.Publish(new CombatEngagedEvent
            {
                EncounterId = encounter.Id,
                Distance = encounter.Distance
            });
        }
    }

    /// <summary>
    /// Active combat - both sides fire weapons, shields recharge, AI makes decisions
    /// </summary>
    private void UpdateEngagement(CombatEncounter encounter, float deltaTime)
    {
        // Recharge player shields
        encounter.PlayerShip.RechargeShields(deltaTime);

        // Recharge enemy shields
        encounter.EnemyShip.RechargeShields(deltaTime);

        // Update weapon reloads
        UpdateWeaponReloads(encounter.PlayerShip, deltaTime);
        UpdateWeaponReloads(encounter.EnemyShip, deltaTime);

        // Enemy AI fires automatically
        encounter.TimeSinceLastEnemyShot += deltaTime;
        var enemyFireInterval = GetEnemyFireInterval(encounter.EnemyShip);
        if (encounter.TimeSinceLastEnemyShot >= enemyFireInterval)
        {
            encounter.TimeSinceLastEnemyShot = 0;
            var result = FireWeapons(encounter.EnemyShip, encounter.PlayerShip, encounter.Distance, isPlayer: false);
            if (result.IsSuccess)
            {
                _eventBus?.Publish(new CombatDamageDealtEvent
                {
                    EncounterId = encounter.Id,
                    AttackerName = encounter.EnemyName,
                    DefenderName = _gameState!.PlayerName,
                    TotalDamage = result.TotalDamage,
                    ShieldDamage = result.ShieldDamage,
                    HullDamage = result.HullDamage,
                    IsCritical = result.IsCritical
                });
            }
        }

        // Enemy AI decision-making
        UpdateEnemyAI(encounter, deltaTime);

        // Check for destruction
        if (encounter.PlayerShip.IsDestroyed)
        {
            ResolveEncounter(encounter, CombatResult.PlayerDestroyed);
        }
        else if (encounter.EnemyShip.IsDestroyed)
        {
            ResolveEncounter(encounter, CombatResult.EnemyDestroyed);
        }
    }

    /// <summary>
    /// Ships are separating - check if escape succeeds
    /// </summary>
    private void UpdateDisengage(CombatEncounter encounter, float deltaTime)
    {
        var separationSpeed = Math.Abs(encounter.PlayerShip.MaxSpeed - encounter.EnemyShip.MaxSpeed);
        encounter.Distance += separationSpeed * deltaTime;

        var escapeDistance = GetMaxWeaponRange(encounter.EnemyShip) * 2;

        if (encounter.Distance >= escapeDistance)
        {
            if (encounter.FleeingSide == FleeSide.Player)
            {
                ResolveEncounter(encounter, CombatResult.PlayerFled);
            }
            else
            {
                ResolveEncounter(encounter, CombatResult.EnemyFled);
            }
        }
    }

    /// <summary>
    /// Fires all weapons from attacker at defender
    /// </summary>
    private CombatActionResult FireWeapons(Ship attacker, Ship defender, double distance, bool isPlayer)
    {
        var weapons = GetInstalledWeapons(attacker);
        if (weapons.Count == 0)
            return CombatActionResult.Failed("No weapons installed");

        int totalDamage = 0;
        int shieldDamage = 0;
        int hullDamage = 0;
        bool isCritical = false;
        var hits = 0;
        var misses = 0;

        foreach (var weapon in weapons)
        {
            // Check range
            if (!weapon.IsInRange(distance))
                continue;

            // Try to fire (ammo/energy check)
            if (!weapon.Fire())
                continue;

            // Accuracy check
            var effectiveAccuracy = weapon.Accuracy * (1.0 - (distance / weapon.Range) * 0.3);
            if (_rng.NextDouble() > effectiveAccuracy)
            {
                misses++;
                continue;
            }

            // Calculate damage at range
            var baseDamage = weapon.GetDamageAtRange(distance);
            if (baseDamage <= 0)
                continue;

            // Critical hit check
            var critMultiplier = 1.0;
            if (_rng.NextDouble() < weapon.CritChance)
            {
                critMultiplier = weapon.CritMultiplier;
                isCritical = true;
            }

            var rawDamage = (int)(baseDamage * critMultiplier);

            // Apply damage to defender: shields first, then hull
            var remainingDamage = rawDamage;

            // Shield absorption
            if (defender.CurrentShield > 0)
            {
                var shieldAbsorbed = Math.Min(remainingDamage, defender.CurrentShield);
                defender.CurrentShield -= shieldAbsorbed;
                shieldDamage += shieldAbsorbed;
                remainingDamage -= shieldAbsorbed;
            }

            // Hull damage
            if (remainingDamage > 0)
            {
                defender.CurrentHull = Math.Max(0, defender.CurrentHull - remainingDamage);
                defender.Condition = Math.Max(0, defender.Condition - (remainingDamage * 0.01));
                hullDamage += remainingDamage;
            }

            totalDamage += rawDamage;
            hits++;
        }

        if (hits == 0 && misses == 0)
            return CombatActionResult.Failed("No weapons in range or unable to fire");

        // Publish player damage event if player was hit
        if (!isPlayer && totalDamage > 0 && _eventBus != null && _gameState != null)
        {
            _eventBus.Publish(new PlayerDamagedEvent
            {
                Damage = totalDamage,
                RemainingHealth = defender.CurrentHull,
                Source = "Combat"
            });
        }

        return CombatActionResult.Success(
            totalDamage, shieldDamage, hullDamage, isCritical, hits, misses);
    }

    /// <summary>
    /// Enemy AI decision-making
    /// </summary>
    private void UpdateEnemyAI(CombatEncounter encounter, float deltaTime)
    {
        var enemy = encounter.EnemyShip;
        var player = encounter.PlayerShip;

        // AI decision cooldown (decide every 2 seconds)
        encounter.AIDecisionTimer += deltaTime;
        if (encounter.AIDecisionTimer < 2.0)
            return;
        encounter.AIDecisionTimer = 0;

        var enemyHealthPercent = (double)enemy.CurrentHull / enemy.MaxHull;
        var playerHealthPercent = (double)player.CurrentHull / player.MaxHull;

        // Decision tree
        var decision = DecideEnemyAction(enemy, player, enemyHealthPercent, playerHealthPercent, encounter);

        switch (decision)
        {
            case EnemyAction.Flee:
                if (encounter.EnemyFleeAttempts < 3)
                {
                    _logger.LogInformation("Encounter {Id}: Enemy {Name} attempting to flee",
                        encounter.Id, encounter.EnemyName);
                    AttemptFlee(encounter, isPlayer: false);
                }
                break;

            case EnemyAction.Aggressive:
                // Already handled by automatic firing in UpdateEngagement
                _logger.LogDebug("Encounter {Id}: Enemy {Name} fighting aggressively",
                    encounter.Id, encounter.EnemyName);
                break;

            case EnemyAction.Defensive:
                // Enemy tries to maintain distance
                encounter.Distance = Math.Max(
                    GetMaxWeaponRange(enemy) * 0.8,
                    encounter.Distance + enemy.MaxSpeed * 0.3 * deltaTime);
                _logger.LogDebug("Encounter {Id}: Enemy {Name} maintaining distance",
                    encounter.Id, encounter.EnemyName);
                break;
        }
    }

    /// <summary>
    /// Enemy AI decision tree
    /// </summary>
    private EnemyAction DecideEnemyAction(
        Ship enemy, Ship player,
        double enemyHealthPercent, double playerHealthPercent,
        CombatEncounter encounter)
    {
        // Flee if critically damaged (< 20% hull)
        if (enemyHealthPercent < 0.2)
            return EnemyAction.Flee;

        // Flee if outmatched (enemy hull < 30% of player hull)
        if (enemy.MaxHull < player.MaxHull * 0.3)
            return EnemyAction.Flee;

        // Flee if player is much stronger and enemy is below 50%
        if (enemyHealthPercent < 0.5 && playerHealthPercent > 0.7 &&
            enemy.MaxHull < player.MaxHull * 0.5)
            return EnemyAction.Flee;

        // Aggressive if enemy is stronger
        if (enemy.MaxHull > player.MaxHull * 1.2 && enemyHealthPercent > 0.5)
            return EnemyAction.Aggressive;

        // Aggressive if player is weak
        if (playerHealthPercent < 0.3)
            return EnemyAction.Aggressive;

        // Defensive if evenly matched and enemy is below 60%
        if (enemyHealthPercent < 0.6)
            return EnemyAction.Defensive;

        // Default: aggressive
        return EnemyAction.Aggressive;
    }

    /// <summary>
    /// Attempt to flee from combat
    /// </summary>
    private CombatActionResult AttemptFlee(CombatEncounter encounter, bool isPlayer)
    {
        if (isPlayer)
        {
            encounter.PlayerFleeAttempts++;
            encounter.FleeingSide = FleeSide.Player;
        }
        else
        {
            encounter.EnemyFleeAttempts++;
            encounter.FleeingSide = FleeSide.Enemy;
        }

        // Calculate flee chance based on speed difference
        var fleerSpeed = isPlayer ? encounter.PlayerShip.MaxSpeed : encounter.EnemyShip.MaxSpeed;
        var pursuerSpeed = isPlayer ? encounter.EnemyShip.MaxSpeed : encounter.PlayerShip.MaxSpeed;

        var speedRatio = fleerSpeed / Math.Max(pursuerSpeed, 1);
        var baseFleeChance = Math.Clamp(speedRatio * 0.7, 0.1, 0.9);

        // Bonus for repeated attempts
        var attempts = isPlayer ? encounter.PlayerFleeAttempts : encounter.EnemyFleeAttempts;
        var attemptBonus = (attempts - 1) * 0.15;
        var fleeChance = Math.Min(baseFleeChance + attemptBonus, 0.95);

        var roll = _rng.NextDouble();

        if (roll < fleeChance)
        {
            // Successful flee
            encounter.State = CombatState.Disengaging;
            encounter.Distance += 500; // Initial separation boost

            _logger.LogInformation(
                "Encounter {Id}: {Side} flee attempt {Attempt} succeeded (chance: {Chance:P0}, roll: {Roll:P0})",
                encounter.Id,
                isPlayer ? "Player" : "Enemy",
                attempts,
                fleeChance,
                roll);

            return CombatActionResult.Success(0, 0, 0, false, 0, 0);
        }

        // Failed flee - take parting shot damage
        var partingShotDamage = (int)(encounter.EnemyShip.MaxHull * 0.05);
        if (isPlayer)
        {
            encounter.PlayerShip.TakeDamage(partingShotDamage);

            if (_eventBus != null && _gameState != null)
            {
                _eventBus.Publish(new PlayerDamagedEvent
                {
                    Damage = partingShotDamage,
                    RemainingHealth = encounter.PlayerShip.CurrentHull,
                    Source = "FleeAttempt"
                });
            }
        }

        _logger.LogInformation(
            "Encounter {Id}: {Side} flee attempt {Attempt} failed (chance: {Chance:P0}, roll: {Roll:P0})",
            encounter.Id,
            isPlayer ? "Player" : "Enemy",
            attempts,
            fleeChance,
            roll);

        return CombatActionResult.Failed($"Flee attempt failed. Took {partingShotDamage} parting shot damage.");
    }

    /// <summary>
    /// Resolves a combat encounter and distributes rewards
    /// </summary>
    private void ResolveEncounter(CombatEncounter encounter, CombatResult result)
    {
        encounter.State = CombatState.Resolved;
        encounter.Result = result;

        _logger.LogInformation(
            "Encounter {Id} resolved: {Result}",
            encounter.Id, result);

        // Calculate and distribute rewards
        var rewards = CalculateRewards(encounter, result);

        if (result == CombatResult.EnemyDestroyed && _gameState != null)
        {
            // Credit reward
            _gameState.Credits += rewards.Credits;

            // Loot enemy cargo
            foreach (var (commodityId, quantity) in rewards.Loot)
            {
                _gameState.AddCargo(commodityId, quantity);
            }

            // Update statistics
            _gameState.Statistics.MissionsCompleted++;

            _logger.LogInformation(
                "Encounter {Id}: Rewards - {Credits} credits, {LootCount} loot items",
                encounter.Id, rewards.Credits, rewards.Loot.Count);
        }

        // Publish resolution event
        _eventBus?.Publish(new CombatEncounterEndedEvent
        {
            EncounterId = encounter.Id,
            Result = result.ToString(),
            EnemyName = encounter.EnemyName,
            CreditsEarned = rewards.Credits,
            LootCount = rewards.Loot.Count,
            PlayerHullRemaining = encounter.PlayerShip.CurrentHull,
            PlayerShieldRemaining = encounter.PlayerShip.CurrentShield
        });
    }

    /// <summary>
    /// Calculates combat rewards based on enemy ship and result
    /// </summary>
    private CombatRewards CalculateRewards(CombatEncounter encounter, CombatResult result)
    {
        var rewards = new CombatRewards();

        if (result != CombatResult.EnemyDestroyed)
            return rewards;

        var enemyShip = encounter.EnemyShip;
        var shipClass = enemyShip.GetShipClass();

        // Base credit reward: 10-30% of enemy ship's base price
        var baseValue = shipClass?.BasePrice ?? 50000;
        var rewardPercent = 0.1 + _rng.NextDouble() * 0.2; // 10-30%
        rewards.Credits = (long)(baseValue * rewardPercent);

        // Bonus for tougher enemies
        var difficultyMultiplier = Math.Max(1.0, (double)enemyShip.MaxHull / 100);
        rewards.Credits = (long)(rewards.Credits * difficultyMultiplier);

        // Loot: take some of enemy's cargo
        var lootChance = 0.5;
        foreach (var (commodityId, quantity) in enemyShip.Cargo)
        {
            if (_rng.NextDouble() < lootChance)
            {
                var lootQty = Math.Max(1, (int)(quantity * (0.3 + _rng.NextDouble() * 0.4)));
                rewards.Loot[commodityId] = lootQty;
            }
        }

        // Equipment salvage chance (rare)
        if (_rng.NextDouble() < 0.15 && enemyShip.InstalledEquipment.Count > 0)
        {
            var randomSlot = enemyShip.InstalledEquipment.Keys
                .OrderBy(_ => _rng.Next())
                .First();
            var equipmentId = enemyShip.InstalledEquipment[randomSlot];
            rewards.SalvagedEquipment = equipmentId;
        }

        return rewards;
    }

    // ─── Helper Methods ────────────────────────────────────────────────────

    /// <summary>
    /// Gets the player's ship from GameState
    /// </summary>
    private Ship? GetPlayerShip()
    {
        if (_gameState == null)
            return null;

        return ShipRegistry.Get(_gameState.ShipId);
    }

    /// <summary>
    /// Gets all installed weapons from a ship
    /// </summary>
    private List<Weapon> GetInstalledWeapons(Ship ship)
    {
        var weapons = new List<Weapon>();

        foreach (var (slotId, equipmentId) in ship.InstalledEquipment)
        {
            var equipment = EquipmentRegistry.Get(equipmentId);
            if (equipment is Weapon weapon)
            {
                weapons.Add(weapon);
            }
        }

        return weapons;
    }

    /// <summary>
    /// Gets the maximum weapon range of a ship
    /// </summary>
    private double GetMaxWeaponRange(Ship ship)
    {
        var weapons = GetInstalledWeapons(ship);
        if (weapons.Count == 0)
            return 500; // Default engagement range

        return weapons.Max(w => w.Range);
    }

    /// <summary>
    /// Calculates initial engagement distance based on ship speeds
    /// </summary>
    private double GetInitialEngagementDistance(Ship player, Ship enemy)
    {
        var maxRange = Math.Max(GetMaxWeaponRange(player), GetMaxWeaponRange(enemy));
        return maxRange * (1.5 + _rng.NextDouble() * 1.0);
    }

    /// <summary>
    /// Gets the enemy fire interval based on their weapons
    /// </summary>
    private double GetEnemyFireInterval(Ship enemy)
    {
        var weapons = GetInstalledWeapons(enemy);
        if (weapons.Count == 0)
            return 3.0; // Default slow fire rate

        // Use the fastest fire rate among installed weapons
        var fastestFireRate = weapons.Max(w => w.FireRate);
        return 1.0 / Math.Max(fastestFireRate, 0.1);
    }

    /// <summary>
    /// Updates weapon reload timers for a ship
    /// </summary>
    private void UpdateWeaponReloads(Ship ship, double deltaTime)
    {
        foreach (var (_, equipmentId) in ship.InstalledEquipment)
        {
            var equipment = EquipmentRegistry.Get(equipmentId);
            if (equipment is Weapon weapon && weapon.IsReloading)
            {
                weapon.UpdateReload(deltaTime);
            }
        }
    }
}

// ─── Combat Data Types ────────────────────────────────────────────────────

/// <summary>
/// Represents an active combat encounter between player and enemy
/// </summary>
public sealed class CombatEncounter
{
    /// <summary>Unique encounter identifier</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Player's ship instance</summary>
    public Ship PlayerShip { get; set; } = null!;

    /// <summary>Enemy ship instance</summary>
    public Ship EnemyShip { get; set; } = null!;

    /// <summary>Display name of the enemy</summary>
    public string EnemyName { get; set; } = string.Empty;

    /// <summary>Location where combat is taking place</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Current combat state</summary>
    public CombatState State { get; set; } = CombatState.Approaching;

    /// <summary>Current distance between ships in meters</summary>
    public double Distance { get; set; }

    /// <summary>Result of the encounter (set when resolved)</summary>
    public CombatResult Result { get; set; }

    /// <summary>Which side is attempting to flee</summary>
    public FleeSide FleeingSide { get; set; }

    /// <summary>Time accumulator for player weapon firing</summary>
    public double TimeSinceLastPlayerShot { get; set; }

    /// <summary>Time accumulator for enemy weapon firing</summary>
    public double TimeSinceLastEnemyShot { get; set; }

    /// <summary>Number of flee attempts by player</summary>
    public int PlayerFleeAttempts { get; set; }

    /// <summary>Number of flee attempts by enemy</summary>
    public int EnemyFleeAttempts { get; set; }

    /// <summary>Timer for AI decision-making</summary>
    public double AIDecisionTimer { get; set; }
}

/// <summary>
/// Combat encounter state machine
/// </summary>
public enum CombatState
{
    /// <summary>Ships are closing distance</summary>
    Approaching,

    /// <summary>Ships are in weapon range and fighting</summary>
    Engaged,

    /// <summary>One side is fleeing, ships are separating</summary>
    Disengaging,

    /// <summary>Encounter is over</summary>
    Resolved
}

/// <summary>
/// Possible outcomes of a combat encounter
/// </summary>
public enum CombatResult
{
    /// <summary>Enemy ship destroyed</summary>
    EnemyDestroyed,

    /// <summary>Player ship destroyed</summary>
    PlayerDestroyed,

    /// <summary>Player successfully fled</summary>
    PlayerFled,

    /// <summary>Enemy successfully fled</summary>
    EnemyFled
}

/// <summary>
/// Which side is attempting to flee
/// </summary>
public enum FleeSide
{
    Player,
    Enemy
}

/// <summary>
/// Enemy AI action decisions
/// </summary>
public enum EnemyAction
{
    /// <summary>Fight aggressively, close distance</summary>
    Aggressive,

    /// <summary>Maintain distance, fight cautiously</summary>
    Defensive,

    /// <summary>Attempt to escape</summary>
    Flee
}

/// <summary>
/// Result of a combat action (fire, flee, etc.)
/// </summary>
public sealed class CombatActionResult
{
    /// <summary>Whether the action succeeded</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Error message if failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Total raw damage dealt</summary>
    public int TotalDamage { get; init; }

    /// <summary>Damage absorbed by shields</summary>
    public int ShieldDamage { get; init; }

    /// <summary>Damage that penetrated to hull</summary>
    public int HullDamage { get; init; }

    /// <summary>Whether any hit was a critical hit</summary>
    public bool IsCritical { get; init; }

    /// <summary>Number of weapon hits</summary>
    public int Hits { get; init; }

    /// <summary>Number of weapon misses</summary>
    public int Misses { get; init; }

    public static CombatActionResult Success(
        int totalDamage, int shieldDamage, int hullDamage,
        bool isCritical, int hits, int misses)
    {
        return new CombatActionResult
        {
            IsSuccess = true,
            TotalDamage = totalDamage,
            ShieldDamage = shieldDamage,
            HullDamage = hullDamage,
            IsCritical = isCritical,
            Hits = hits,
            Misses = misses
        };
    }

    public static CombatActionResult Failed(string error)
    {
        return new CombatActionResult
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Rewards earned from a combat encounter
/// </summary>
public sealed class CombatRewards
{
    /// <summary>Credits earned</summary>
    public long Credits { get; set; }

    /// <summary>Loot from enemy cargo (commodityId -> quantity)</summary>
    public Dictionary<string, int> Loot { get; } = new();

    /// <summary>Salvaged equipment ID (if any)</summary>
    public string? SalvagedEquipment { get; set; }
}

// ─── Combat Events ────────────────────────────────────────────────────────

/// <summary>
/// Event fired when a combat encounter begins
/// </summary>
public sealed record CombatEncounterStartedEvent : GameEvent
{
    public required string EncounterId { get; init; }
    public required string EnemyName { get; init; }
    public required string EnemyShipClassId { get; init; }
    public required string Location { get; init; }
}

/// <summary>
/// Event fired when ships enter engagement range
/// </summary>
public sealed record CombatEngagedEvent : GameEvent
{
    public required string EncounterId { get; init; }
    public required double Distance { get; init; }
}

/// <summary>
/// Event fired when damage is dealt in combat
/// </summary>
public sealed record CombatDamageDealtEvent : GameEvent
{
    public required string EncounterId { get; init; }
    public required string AttackerName { get; init; }
    public required string DefenderName { get; init; }
    public required int TotalDamage { get; init; }
    public required int ShieldDamage { get; init; }
    public required int HullDamage { get; init; }
    public required bool IsCritical { get; init; }
}

/// <summary>
/// Event fired when a combat encounter ends
/// </summary>
public sealed record CombatEncounterEndedEvent : GameEvent
{
    public required string EncounterId { get; init; }
    public required string Result { get; init; }
    public required string EnemyName { get; init; }
    public required long CreditsEarned { get; init; }
    public required int LootCount { get; init; }
    public required int PlayerHullRemaining { get; init; }
    public required int PlayerShieldRemaining { get; init; }
}
