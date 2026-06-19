using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Player state - name, credits, health, location, ship, cargo, stats
/// Integrates with GameState from core
/// </summary>
public sealed class Player : ISaveable
{
    // Identity
    /// <summary>
    /// Player character name
    /// </summary>
    public string Name { get; set; } = "Pilot";

    /// <summary>
    /// Player ID (for multiplayer/save identification)
    /// </summary>
    public string PlayerId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Character portrait/icon
    /// </summary>
    public string PortraitResource { get; set; } = string.Empty;

    /// <summary>
    /// Character background/origin
    /// </summary>
    public string Background { get; set; } = string.Empty;

    // Resources
    /// <summary>
    /// Current credits (money)
    /// </summary>
    public long Credits { get; set; } = 10000;

    /// <summary>
    /// Current health
    /// </summary>
    public int Health { get; set; } = 100;

    /// <summary>
    /// Maximum health
    /// </summary>
    public int MaxHealth { get; set; } = 100;

    /// <summary>
    /// Current fuel
    /// </summary>
    public int CurrentFuel { get; set; } = 100;

    /// <summary>
    /// Maximum fuel capacity
    /// </summary>
    public int MaxFuel { get; set; } = 100;

    // Location
    /// <summary>
    /// Current location ID (planet/station)
    /// </summary>
    public string CurrentLocationId { get; set; } = "neon_station";

    /// <summary>
    /// Previous location ID (for return trips)
    /// </summary>
    public string PreviousLocationId { get; set; } = string.Empty;

    /// <summary>
    /// Home base location ID
    /// </summary>
    public string HomeBaseId { get; set; } = "neon_station";

    // Ship
    /// <summary>
    /// Current ship ID
    /// </summary>
    public string ShipId { get; set; } = "starter_ship";

    /// <summary>
    /// Ship name (customizable)
    /// </summary>
    public string ShipName { get; set; } = "Star Runner";

    /// <summary>
    /// Ship hull integrity (0-100)
    /// </summary>
    public int ShipHull { get; set; } = 100;

    /// <summary>
    /// Ship max hull
    /// </summary>
    public int ShipMaxHull { get; set; } = 100;

    /// <summary>
    /// Ship shield strength (0-100)
    /// </summary>
    public int ShipShields { get; set; } = 100;

    /// <summary>
    /// Ship max shields
    /// </summary>
    public int ShipMaxShields { get; set; } = 100;

    // Cargo
    /// <summary>
    /// Cargo contents (commodityId -> quantity)
    /// </summary>
    public ConcurrentDictionary<string, int> Cargo { get; } = new();

    /// <summary>
    /// Cargo capacity
    /// </summary>
    public int CargoCapacity { get; set; } = 50;

    // Equipment/Upgrades
    /// <summary>
    /// Installed equipment upgrades (equipmentId -> slot)
    /// </summary>
    public Dictionary<string, string> InstalledEquipment { get; } = new();

    /// <summary>
    /// Installed ship upgrades (upgrade IDs)
    /// </summary>
    public HashSet<string> InstalledUpgrades { get; } = new();

    // Progression
    /// <summary>
    /// Experience points
    /// </summary>
    public long Experience { get; set; } = 0;

    /// <summary>
    /// Player level
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Skill points available to spend
    /// </summary>
    public int SkillPoints { get; set; } = 0;

    /// <summary>
    /// Player skills system
    /// </summary>
    public Skills Skills { get; set; } = new();

    /// <summary>
    /// Reputation with factions
    /// </summary>
    public Reputation Reputation { get; set; } = new();

    // Statistics
    /// <summary>
    /// Game statistics
    /// </summary>
    public PlayerStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Player's known/discovered locations
    /// </summary>
    public HashSet<string> DiscoveredLocations { get; } = new();

    /// <summary>
    /// Player's known jump routes
    /// </summary>
    public HashSet<string> KnownRoutes { get; } = new();

    /// <summary>
    /// Active quests/missions
    /// </summary>
    public List<string> ActiveMissionIds { get; } = new();

    /// <summary>
    /// Completed mission IDs
    /// </summary>
    public HashSet<string> CompletedMissionIds { get; } = new();

    /// <summary>
    /// Failed mission IDs
    /// </summary>
    public HashSet<string> FailedMissionIds { get; } = new();

    // Game state
    /// <summary>
    /// Game time when player was last saved
    /// </summary>
    public DateTime LastSaveTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total play time
    /// </summary>
    public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Game difficulty setting
    /// </summary>
    public GameDifficulty Difficulty { get; set; } = GameDifficulty.Normal;

    /// <summary>
    /// Ironman mode (permadeath, single save)
    /// </summary>
    public bool IronmanMode { get; set; } = false;

    /// <summary>
    /// Whether player is in combat
    /// </summary>
    public bool InCombat { get; set; } = false;

    /// <summary>
    /// Whether player is docked at station
    /// </summary>
    public bool IsDocked { get; set; } = true;

    // ISaveable implementation
    public string SaveId => $"player_{PlayerId}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize player to JSON
    /// </summary>
    public JObject Serialize()
    {
        var cargoDict = new Dictionary<string, int>();
        foreach (var kvp in Cargo)
            cargoDict[kvp.Key] = kvp.Value;

        var equipmentDict = new Dictionary<string, string>(InstalledEquipment);

        return new JObject
        {
            ["name"] = Name,
            ["playerId"] = PlayerId,
            ["portraitResource"] = PortraitResource,
            ["background"] = Background,
            ["credits"] = Credits,
            ["health"] = Health,
            ["maxHealth"] = MaxHealth,
            ["currentFuel"] = CurrentFuel,
            ["maxFuel"] = MaxFuel,
            ["currentLocationId"] = CurrentLocationId,
            ["previousLocationId"] = PreviousLocationId,
            ["homeBaseId"] = HomeBaseId,
            ["shipId"] = ShipId,
            ["shipName"] = ShipName,
            ["shipHull"] = ShipHull,
            ["shipMaxHull"] = ShipMaxHull,
            ["shipShields"] = ShipShields,
            ["shipMaxShields"] = ShipMaxShields,
            ["cargo"] = JObject.FromObject(cargoDict),
            ["cargoCapacity"] = CargoCapacity,
            ["installedEquipment"] = JObject.FromObject(equipmentDict),
            ["installedUpgrades"] = JArray.FromObject(InstalledUpgrades),
            ["experience"] = Experience,
            ["level"] = Level,
            ["skillPoints"] = SkillPoints,
            ["skills"] = Skills.Serialize(),
            ["reputation"] = Reputation.Serialize(),
            ["statistics"] = Statistics.Serialize(),
            ["discoveredLocations"] = JArray.FromObject(DiscoveredLocations),
            ["knownRoutes"] = JArray.FromObject(KnownRoutes),
            ["activeMissionIds"] = JArray.FromObject(ActiveMissionIds),
            ["completedMissionIds"] = JArray.FromObject(CompletedMissionIds),
            ["failedMissionIds"] = JArray.FromObject(FailedMissionIds),
            ["lastSaveTime"] = LastSaveTime.ToString("o"),
            ["totalPlayTime"] = TotalPlayTime.ToString(),
            ["difficulty"] = Difficulty.ToString(),
            ["ironmanMode"] = IronmanMode,
            ["inCombat"] = InCombat,
            ["isDocked"] = IsDocked
        };
    }

    /// <summary>
    /// Deserialize player from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        Name = data["name"]?.ToString() ?? "Pilot";
        PlayerId = data["playerId"]?.ToString() ?? Guid.NewGuid().ToString();
        PortraitResource = data["portraitResource"]?.ToString() ?? string.Empty;
        Background = data["background"]?.ToString() ?? string.Empty;
        Credits = data["credits"]?.ToObject<long>() ?? 10000;
        Health = data["health"]?.ToObject<int>() ?? 100;
        MaxHealth = data["maxHealth"]?.ToObject<int>() ?? 100;
        CurrentFuel = data["currentFuel"]?.ToObject<int>() ?? 100;
        MaxFuel = data["maxFuel"]?.ToObject<int>() ?? 100;
        CurrentLocationId = data["currentLocationId"]?.ToString() ?? "neon_station";
        PreviousLocationId = data["previousLocationId"]?.ToString() ?? string.Empty;
        HomeBaseId = data["homeBaseId"]?.ToString() ?? "neon_station";
        ShipId = data["shipId"]?.ToString() ?? "starter_ship";
        ShipName = data["shipName"]?.ToString() ?? "Star Runner";
        ShipHull = data["shipHull"]?.ToObject<int>() ?? 100;
        ShipMaxHull = data["shipMaxHull"]?.ToObject<int>() ?? 100;
        ShipShields = data["shipShields"]?.ToObject<int>() ?? 100;
        ShipMaxShields = data["shipMaxShields"]?.ToObject<int>() ?? 100;

        Cargo.Clear();
        if (data["cargo"] is JObject cargoObj)
        {
            foreach (var kvp in cargoObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    Cargo[kvp.Key] = value.Value;
            }
        }

        CargoCapacity = data["cargoCapacity"]?.ToObject<int>() ?? 50;

        InstalledEquipment.Clear();
        if (data["installedEquipment"] is JObject equipObj)
        {
            foreach (var kvp in equipObj)
            {
                var value = kvp.Value?.ToString();
                if (!string.IsNullOrEmpty(value))
                    InstalledEquipment[kvp.Key] = value;
            }
        }

        InstalledUpgrades.Clear();
        if (data["installedUpgrades"] is JArray upgradesArray)
        {
            foreach (var upgrade in upgradesArray)
            {
                var upgradeStr = upgrade?.ToString();
                if (!string.IsNullOrEmpty(upgradeStr))
                    InstalledUpgrades.Add(upgradeStr);
            }
        }

        Experience = data["experience"]?.ToObject<long>() ?? 0;
        Level = data["level"]?.ToObject<int>() ?? 1;
        SkillPoints = data["skillPoints"]?.ToObject<int>() ?? 0;

        if (data["skills"] is JObject skillsObj)
        {
            Skills = new Skills();
            Skills.Deserialize(skillsObj);
        }

        if (data["reputation"] is JObject repObj)
        {
            Reputation = new Reputation();
            Reputation.Deserialize(repObj);
        }

        if (data["statistics"] is JObject statsObj)
        {
            Statistics = new PlayerStatistics();
            Statistics.Deserialize(statsObj);
        }

        DiscoveredLocations.Clear();
        if (data["discoveredLocations"] is JArray discArray)
        {
            foreach (var loc in discArray)
            {
                var locStr = loc?.ToString();
                if (!string.IsNullOrEmpty(locStr))
                    DiscoveredLocations.Add(locStr);
            }
        }

        KnownRoutes.Clear();
        if (data["knownRoutes"] is JArray routeArray)
        {
            foreach (var route in routeArray)
            {
                var routeStr = route?.ToString();
                if (!string.IsNullOrEmpty(routeStr))
                    KnownRoutes.Add(routeStr);
            }
        }

        ActiveMissionIds.Clear();
        if (data["activeMissionIds"] is JArray activeArray)
        {
            foreach (var mission in activeArray)
            {
                var missionStr = mission?.ToString();
                if (!string.IsNullOrEmpty(missionStr))
                    ActiveMissionIds.Add(missionStr);
            }
        }

        CompletedMissionIds.Clear();
        if (data["completedMissionIds"] is JArray compArray)
        {
            foreach (var mission in compArray)
            {
                var missionStr = mission?.ToString();
                if (!string.IsNullOrEmpty(missionStr))
                    CompletedMissionIds.Add(missionStr);
            }
        }

        FailedMissionIds.Clear();
        if (data["failedMissionIds"] is JArray failArray)
        {
            foreach (var mission in failArray)
            {
                var missionStr = mission?.ToString();
                if (!string.IsNullOrEmpty(missionStr))
                    FailedMissionIds.Add(missionStr);
            }
        }

        if (data["lastSaveTime"] != null)
            LastSaveTime = DateTime.Parse(data["lastSaveTime"]!.ToString());

        if (data["totalPlayTime"] != null)
            TotalPlayTime = TimeSpan.Parse(data["totalPlayTime"]!.ToString());

        if (Enum.TryParse<GameDifficulty>(data["difficulty"]?.ToString(), out var diff))
            Difficulty = diff;

        IronmanMode = data["ironmanMode"]?.ToObject<bool>() ?? false;
        InCombat = data["inCombat"]?.ToObject<bool>() ?? false;
        IsDocked = data["isDocked"]?.ToObject<bool>() ?? true;
    }

    /// <summary>
    /// Adds credits to player
    /// </summary>
    public void AddCredits(long amount)
    {
        Credits += amount;
        if (amount > 0)
            Statistics.TotalCreditsEarned += amount;
        else
            Statistics.TotalCreditsSpent += Math.Abs(amount);
    }

    /// <summary>
    /// Removes credits from player (returns false if insufficient)
    /// </summary>
    public bool RemoveCredits(long amount)
    {
        if (Credits < amount)
            return false;
        
        Credits -= amount;
        Statistics.TotalCreditsSpent += amount;
        return true;
    }

    /// <summary>
    /// Checks if player can afford amount
    /// </summary>
    public bool CanAfford(long amount)
    {
        return Credits >= amount;
    }

    /// <summary>
    /// Adds cargo to player's hold
    /// </summary>
    public bool AddCargo(string commodityId, int quantity)
    {
        var currentTotal = Cargo.Values.Sum();
        if (currentTotal + quantity > CargoCapacity)
            return false;

        Cargo.AddOrUpdate(commodityId, quantity, (_, existing) => existing + quantity);
        return true;
    }

    /// <summary>
    /// Removes cargo from player's hold
    /// </summary>
    public bool RemoveCargo(string commodityId, int quantity)
    {
        if (!Cargo.TryGetValue(commodityId, out var current) || current < quantity)
            return false;

        var newQuantity = current - quantity;
        if (newQuantity <= 0)
            Cargo.TryRemove(commodityId, out _);
        else
            Cargo[commodityId] = newQuantity;
        
        return true;
    }

    /// <summary>
    /// Gets cargo quantity
    /// </summary>
    public int GetCargoQuantity(string commodityId)
    {
        return Cargo.TryGetValue(commodityId, out var qty) ? qty : 0;
    }

    /// <summary>
    /// Gets total cargo used
    /// </summary>
    public int GetTotalCargoUsed()
    {
        return Cargo.Values.Sum();
    }

    /// <summary>
    /// Gets available cargo space
    /// </summary>
    public int GetAvailableCargoSpace()
    {
        return CargoCapacity - GetTotalCargoUsed();
    }

    /// <summary>
    /// Damages player
    /// </summary>
    public void TakeDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
        Statistics.DamageTaken += amount;
        
        if (Health <= 0)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// Heals player
    /// </summary>
    public void Heal(int amount)
    {
        Health = Math.Min(MaxHealth, Health + amount);
    }

    /// <summary>
    /// Damages ship hull
    /// </summary>
    public void DamageHull(int amount)
    {
        ShipHull = Math.Max(0, ShipHull - amount);
        Statistics.HullDamageTaken += amount;
        
        if (ShipHull <= 0)
        {
            OnShipDestroyed();
        }
    }

    /// <summary>
    /// Repairs ship hull
    /// </summary>
    public void RepairHull(int amount)
    {
        ShipHull = Math.Min(ShipMaxHull, ShipHull + amount);
    }

    /// <summary>
    /// Damages ship shields
    /// </summary>
    public void DamageShields(int amount)
    {
        ShipShields = Math.Max(0, ShipShields - amount);
    }

    /// <summary>
    /// Recharges ship shields
    /// </summary>
    public void RechargeShields(int amount)
    {
        ShipShields = Math.Min(ShipMaxShields, ShipShields + amount);
    }

    /// <summary>
    /// Consumes fuel
    /// </summary>
    public bool ConsumeFuel(int amount)
    {
        if (CurrentFuel < amount)
            return false;
        
        CurrentFuel -= amount;
        Statistics.FuelConsumed += amount;
        return true;
    }

    /// <summary>
    /// Refuels ship
    /// </summary>
    public void Refuel(int amount)
    {
        CurrentFuel = Math.Min(MaxFuel, CurrentFuel + amount);
    }

    /// <summary>
    /// Adds experience
    /// </summary>
    public void AddExperience(long amount)
    {
        Experience += amount;
        
        // Level up check (simple formula: 1000 * level^2)
        var xpForNext = GetXPForLevel(Level + 1);
        while (Experience >= xpForNext && Level < 100)
        {
            Experience -= xpForNext;
            Level++;
            SkillPoints++;
            xpForNext = GetXPForLevel(Level + 1);
        }
    }

    /// <summary>
    /// Gets XP required for a level
    /// </summary>
    public static long GetXPForLevel(int level)
    {
        return 1000L * level * level;
    }

    /// <summary>
    /// Gets total XP for a level
    /// </summary>
    public static long GetTotalXPForLevel(int level)
    {
        long total = 0;
        for (int i = 1; i < level; i++)
        {
            total += GetXPForLevel(i);
        }
        return total;
    }

    /// <summary>
    /// Discovers a location
    /// </summary>
    public void DiscoverLocation(string locationId)
    {
        if (DiscoveredLocations.Add(locationId))
        {
            Statistics.LocationsDiscovered++;
        }
    }

    /// <summary>
    /// Learns a route
    /// </summary>
    public void LearnRoute(string routeId)
    {
        KnownRoutes.Add(routeId);
    }

    /// <summary>
    /// Starts a mission
    /// </summary>
    public void StartMission(string missionId)
    {
        ActiveMissionIds.Add(missionId);
        Statistics.MissionsStarted++;
    }

    /// <summary>
    /// Completes a mission
    /// </summary>
    public void CompleteMission(string missionId, long reward)
    {
        ActiveMissionIds.Remove(missionId);
        CompletedMissionIds.Add(missionId);
        Statistics.MissionsCompleted++;
        Statistics.TotalMissionRewards += reward;
    }

    /// <summary>
    /// Fails a mission
    /// </summary>
    public void FailMission(string missionId)
    {
        ActiveMissionIds.Remove(missionId);
        FailedMissionIds.Add(missionId);
        Statistics.MissionsFailed++;
    }

    /// <summary>
    /// Records a trade
    /// </summary>
    public void RecordTrade(long profit)
    {
        Statistics.TradesCompleted++;
        Statistics.TotalTradeProfit += profit;
    }

    /// <summary>
    /// Records travel
    /// </summary>
    public void RecordTravel(double distance)
    {
        Statistics.DistanceTraveled += distance;
    }

    /// <summary>
    /// Records combat kill
    /// </summary>
    public void RecordKill(string enemyType)
    {
        Statistics.EnemiesDestroyed++;
        if (enemyType.Contains("pirate")) Statistics.PiratesDestroyed++;
        if (enemyType.Contains("police")) Statistics.PoliceDestroyed++;
        if (enemyType.Contains("alien")) Statistics.AliensDestroyed++;
    }

    /// <summary>
    /// Called when player dies
    /// </summary>
    private void OnDeath()
    {
        Statistics.Deaths++;
        
        if (IronmanMode)
        {
            // Game over - handled by game manager
        }
        else
        {
            // Respawn at home base with penalties
            Health = MaxHealth / 2;
            CurrentLocationId = HomeBaseId;
            IsDocked = true;
            InCombat = false;
            
            // Lose some credits
            var loss = Credits / 10;
            Credits = Math.Max(0, Credits - loss);
            Statistics.TotalCreditsLost += loss;
        }
    }

    /// <summary>
    /// Called when ship is destroyed
    /// </summary>
    private void OnShipDestroyed()
    {
        Statistics.ShipsLost++;
        
        if (IronmanMode)
        {
            // Game over
        }
        else
        {
            // Emergency escape pod to home base
            ShipHull = ShipMaxHull / 4;
            ShipShields = 0;
            CurrentLocationId = HomeBaseId;
            IsDocked = true;
            InCombat = false;
            
            // Lose cargo
            Cargo.Clear();
            Statistics.TotalCargoLost += GetTotalCargoUsed();
        }
    }

    /// <summary>
    /// Creates a new player with defaults
    /// </summary>
    public static Player CreateNew(string name, string background = "merchant")
    {
        var player = new Player
        {
            Name = name,
            Background = background,
            PlayerId = Guid.NewGuid().ToString(),
            Credits = 10000,
            Health = 100,
            MaxHealth = 100,
            CurrentFuel = 100,
            MaxFuel = 100,
            CurrentLocationId = "neon_station",
            HomeBaseId = "neon_station",
            ShipId = "starter_ship",
            ShipName = "Star Runner",
            ShipHull = 100,
            ShipMaxHull = 100,
            ShipShields = 100,
            ShipMaxShields = 100,
            CargoCapacity = 50,
            Level = 1,
            Experience = 0,
            SkillPoints = 0,
            Difficulty = GameDifficulty.Normal,
            IronmanMode = false,
            IsDocked = true,
            LastSaveTime = DateTime.UtcNow
        };

        // Initialize skills and reputation for new game
        player.Skills.InitializeNewGame();
        player.Reputation.InitializeNewGame();
        player.DiscoverLocation("neon_station");

        // Background bonuses
        ApplyBackgroundBonuses(player, background);

        return player;
    }

    /// <summary>
    /// Applies starting bonuses based on background
    /// </summary>
    private static void ApplyBackgroundBonuses(Player player, string background)
    {
        switch (background.ToLower())
        {
            case "merchant":
                player.Credits += 5000;
                player.Skills.AddSkillXP("haggling", 500);
                player.Skills.AddSkillXP("market_analysis", 300);
                break;
            case "pilot":
                player.Skills.AddSkillXP("navigation", 500);
                player.Skills.AddSkillXP("evasion", 300);
                player.CurrentFuel = player.MaxFuel;
                break;
            case "combat":
            case "mercenary":
                player.Skills.AddSkillXP("gunnery", 500);
                player.Skills.AddSkillXP("shields", 300);
                player.ShipHull = player.ShipMaxHull;
                player.ShipShields = player.ShipMaxShields;
                break;
            case "engineer":
                player.Skills.AddSkillXP("repair", 500);
                player.Skills.AddSkillXP("systems_engineering", 300);
                player.InstalledUpgrades.Add("basic_repair_drone");
                break;
            case "explorer":
            break;
            case "smuggler":
                player.Credits += 2000;
                player.Skills.AddSkillXP("evasion", 400);
                player.Skills.AddSkillXP("haggling", 200);
                player.InstalledUpgrades.Add("shielded_cargo_hold");
                player.Reputation.ChangeReputation("crimson_fleet", 10, "Smuggler background");
                player.Reputation.ChangeReputation("shadow_syndicate", 10, "Smuggler background");
                break;
        }
    }
}

/// <summary>
/// Game difficulty levels
/// </summary>
public enum GameDifficulty
{
    Easy,
    Normal,
    Hard,
    Expert,
    Ironman
}

/// <summary>
/// Player statistics tracking
/// </summary>
public sealed class PlayerStatistics : ISaveable
{
    // Economic
    public long TotalCreditsEarned { get; set; }
    public long TotalCreditsSpent { get; set; }
    public long TotalCreditsLost { get; set; }
    public long TotalTradeProfit { get; set; }
    public int TradesCompleted { get; set; }
    public int BestSingleTrade { get; set; }
    public long LargestCargoHaul { get; set; }

    // Combat
    public int EnemiesDestroyed { get; set; }
    public int PiratesDestroyed { get; set; }
    public int PoliceDestroyed { get; set; }
    public int AliensDestroyed { get; set; }
    public long DamageDealt { get; set; }
    public long DamageTaken { get; set; }
    public long HullDamageTaken { get; set; }
    public int ShipsDestroyed { get; set; }
    public int ShipsLost { get; set; }

    // Travel
    public double DistanceTraveled { get; set; }
    public int SystemsVisited { get; set; }
    public int LocationsDiscovered { get; set; }
    public long FuelConsumed { get; set; }
    public int JumpsMade { get; set; }

    // Missions
    public int MissionsStarted { get; set; }
    public int MissionsCompleted { get; set; }
    public int MissionsFailed { get; set; }
    public long TotalMissionRewards { get; set; }
    public long BestMissionReward { get; set; }

    // General
    public int Deaths { get; set; }
    public int TotalCargoLost { get; set; }
    public DateTime GameStartTime { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;

    public string SaveId => "player_statistics";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["totalCreditsEarned"] = TotalCreditsEarned,
            ["totalCreditsSpent"] = TotalCreditsSpent,
            ["totalCreditsLost"] = TotalCreditsLost,
            ["totalTradeProfit"] = TotalTradeProfit,
            ["tradesCompleted"] = TradesCompleted,
            ["bestSingleTrade"] = BestSingleTrade,
            ["largestCargoHaul"] = LargestCargoHaul,
            ["enemiesDestroyed"] = EnemiesDestroyed,
            ["piratesDestroyed"] = PiratesDestroyed,
            ["policeDestroyed"] = PoliceDestroyed,
            ["aliensDestroyed"] = AliensDestroyed,
            ["damageDealt"] = DamageDealt,
            ["damageTaken"] = DamageTaken,
            ["hullDamageTaken"] = HullDamageTaken,
            ["shipsDestroyed"] = ShipsDestroyed,
            ["shipsLost"] = ShipsLost,
            ["distanceTraveled"] = DistanceTraveled,
            ["systemsVisited"] = SystemsVisited,
            ["locationsDiscovered"] = LocationsDiscovered,
            ["fuelConsumed"] = FuelConsumed,
            ["jumpsMade"] = JumpsMade,
            ["missionsStarted"] = MissionsStarted,
            ["missionsCompleted"] = MissionsCompleted,
            ["missionsFailed"] = MissionsFailed,
            ["totalMissionRewards"] = TotalMissionRewards,
            ["bestMissionReward"] = BestMissionReward,
            ["deaths"] = Deaths,
            ["totalCargoLost"] = TotalCargoLost,
            ["gameStartTime"] = GameStartTime.ToString("o"),
            ["totalPlayTime"] = TotalPlayTime.ToString()
        };
    }

    public void Deserialize(JObject data)
    {
        TotalCreditsEarned = data["totalCreditsEarned"]?.ToObject<long>() ?? 0;
        TotalCreditsSpent = data["totalCreditsSpent"]?.ToObject<long>() ?? 0;
        TotalCreditsLost = data["totalCreditsLost"]?.ToObject<long>() ?? 0;
        TotalTradeProfit = data["totalTradeProfit"]?.ToObject<long>() ?? 0;
        TradesCompleted = data["tradesCompleted"]?.ToObject<int>() ?? 0;
        BestSingleTrade = data["bestSingleTrade"]?.ToObject<int>() ?? 0;
        LargestCargoHaul = data["largestCargoHaul"]?.ToObject<long>() ?? 0;
        EnemiesDestroyed = data["enemiesDestroyed"]?.ToObject<int>() ?? 0;
        PiratesDestroyed = data["piratesDestroyed"]?.ToObject<int>() ?? 0;
        PoliceDestroyed = data["policeDestroyed"]?.ToObject<int>() ?? 0;
        AliensDestroyed = data["aliensDestroyed"]?.ToObject<int>() ?? 0;
        DamageDealt = data["damageDealt"]?.ToObject<long>() ?? 0;
        DamageTaken = data["damageTaken"]?.ToObject<long>() ?? 0;
        HullDamageTaken = data["hullDamageTaken"]?.ToObject<long>() ?? 0;
        ShipsDestroyed = data["shipsDestroyed"]?.ToObject<int>() ?? 0;
        ShipsLost = data["shipsLost"]?.ToObject<int>() ?? 0;
        DistanceTraveled = data["distanceTraveled"]?.ToObject<double>() ?? 0;
        SystemsVisited = data["systemsVisited"]?.ToObject<int>() ?? 0;
        LocationsDiscovered = data["locationsDiscovered"]?.ToObject<int>() ?? 0;
        FuelConsumed = data["fuelConsumed"]?.ToObject<long>() ?? 0;
        JumpsMade = data["jumpsMade"]?.ToObject<int>() ?? 0;
        MissionsStarted = data["missionsStarted"]?.ToObject<int>() ?? 0;
        MissionsCompleted = data["missionsCompleted"]?.ToObject<int>() ?? 0;
        MissionsFailed = data["missionsFailed"]?.ToObject<int>() ?? 0;
        TotalMissionRewards = data["totalMissionRewards"]?.ToObject<long>() ?? 0;
        BestMissionReward = data["bestMissionReward"]?.ToObject<long>() ?? 0;
        Deaths = data["deaths"]?.ToObject<int>() ?? 0;
        TotalCargoLost = data["totalCargoLost"]?.ToObject<int>() ?? 0;

        if (data["gameStartTime"] != null)
            GameStartTime = DateTime.Parse(data["gameStartTime"]!.ToString());

        if (data["totalPlayTime"] != null)
            TotalPlayTime = TimeSpan.Parse(data["totalPlayTime"]!.ToString());
    }
}