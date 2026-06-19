using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

/// <summary>
/// Comprehensive xUnit tests for the Player model.
/// Covers credits, cargo, damage/heal, leveling, missions,
/// background bonuses, ironman mode, and serialization round-trip.
/// </summary>
[Collection("Sequential")]
public class PlayerTests
{
    // ── Serialization Round-Trip ──────────────────────────────────

    [Fact]
    public void Serialize_Deserialize_RoundTrip_PreservesAllProperties()
    {
        // Arrange: create a fully populated player
        var player = new Player
        {
            Name = "TestPilot",
            PlayerId = "test-id-123",
            PortraitResource = "portrait_01",
            Background = "merchant",
            Credits = 50000,
            Health = 80,
            MaxHealth = 120,
            CurrentFuel = 75,
            MaxFuel = 150,
            CurrentLocationId = "alpha_centauri",
            PreviousLocationId = "sol_station",
            HomeBaseId = "neon_station",
            ShipId = "explorer_mk2",
            ShipName = "Void Dancer",
            ShipHull = 90,
            ShipMaxHull = 120,
            ShipShields = 60,
            ShipMaxShields = 100,
            CargoCapacity = 200,
            Experience = 5000,
            Level = 3,
            SkillPoints = 2,
            Difficulty = GameDifficulty.Hard,
            IronmanMode = true,
            InCombat = true,
            IsDocked = false,
            LastSaveTime = new DateTime(2087, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            TotalPlayTime = TimeSpan.FromHours(42)
        };

        player.Cargo["water"] = 10;
        player.Cargo["ore"] = 5;
        player.InstalledEquipment["laser_cannon"] = "weapon_slot_1";
        player.InstalledUpgrades.Add("shield_booster");
        player.DiscoveredLocations.Add("alpha_centauri");
        player.DiscoveredLocations.Add("sol_station");
        player.KnownRoutes.Add("sol_to_alpha");
        player.ActiveMissionIds.Add("mission_001");
        player.CompletedMissionIds.Add("mission_000");
        player.FailedMissionIds.Add("mission_fail_1");

        // Act: serialize then deserialize into a new instance
        var json = player.Serialize();
        var restored = new Player();
        restored.Deserialize(json);

        // Assert: all properties match
        Assert.Equal(player.Name, restored.Name);
        Assert.Equal(player.PlayerId, restored.PlayerId);
        Assert.Equal(player.PortraitResource, restored.PortraitResource);
        Assert.Equal(player.Background, restored.Background);
        Assert.Equal(player.Credits, restored.Credits);
        Assert.Equal(player.Health, restored.Health);
        Assert.Equal(player.MaxHealth, restored.MaxHealth);
        Assert.Equal(player.CurrentFuel, restored.CurrentFuel);
        Assert.Equal(player.MaxFuel, restored.MaxFuel);
        Assert.Equal(player.CurrentLocationId, restored.CurrentLocationId);
        Assert.Equal(player.PreviousLocationId, restored.PreviousLocationId);
        Assert.Equal(player.HomeBaseId, restored.HomeBaseId);
        Assert.Equal(player.ShipId, restored.ShipId);
        Assert.Equal(player.ShipName, restored.ShipName);
        Assert.Equal(player.ShipHull, restored.ShipHull);
        Assert.Equal(player.ShipMaxHull, restored.ShipMaxHull);
        Assert.Equal(player.ShipShields, restored.ShipShields);
        Assert.Equal(player.ShipMaxShields, restored.ShipMaxShields);
        Assert.Equal(player.CargoCapacity, restored.CargoCapacity);
        Assert.Equal(player.Experience, restored.Experience);
        Assert.Equal(player.Level, restored.Level);
        Assert.Equal(player.SkillPoints, restored.SkillPoints);
        Assert.Equal(player.Difficulty, restored.Difficulty);
        Assert.Equal(player.IronmanMode, restored.IronmanMode);
        Assert.Equal(player.InCombat, restored.InCombat);
        Assert.Equal(player.IsDocked, restored.IsDocked);

        // Cargo
        Assert.Equal(2, restored.Cargo.Count);
        Assert.Equal(10, restored.Cargo["water"]);
        Assert.Equal(5, restored.Cargo["ore"]);

        // Equipment
        Assert.Single(restored.InstalledEquipment);
        Assert.Equal("weapon_slot_1", restored.InstalledEquipment["laser_cannon"]);
        Assert.Single(restored.InstalledUpgrades);
        Assert.Contains("shield_booster", restored.InstalledUpgrades);

        // Locations & routes
        Assert.Equal(2, restored.DiscoveredLocations.Count);
        Assert.Contains("alpha_centauri", restored.DiscoveredLocations);
        Assert.Single(restored.KnownRoutes);
        Assert.Contains("sol_to_alpha", restored.KnownRoutes);

        // Missions
        Assert.Single(restored.ActiveMissionIds);
        Assert.Contains("mission_001", restored.ActiveMissionIds);
        Assert.Single(restored.CompletedMissionIds);
        Assert.Contains("mission_000", restored.CompletedMissionIds);
        Assert.Single(restored.FailedMissionIds);
        Assert.Contains("mission_fail_1", restored.FailedMissionIds);
    }

    [Fact]
    public void Serialize_Deserialize_EmptyPlayer_ReturnsDefaults()
    {
        var player = new Player();
        var json = player.Serialize();
        var restored = new Player();
        restored.Deserialize(json);

        Assert.Equal("Pilot", restored.Name);
        Assert.Equal(10000, restored.Credits);
        Assert.Equal(100, restored.Health);
        Assert.Equal(100, restored.MaxHealth);
        Assert.Equal("neon_station", restored.CurrentLocationId);
        Assert.Equal("starter_ship", restored.ShipId);
        Assert.Equal(1, restored.Level);
        Assert.Equal(0, restored.Experience);
        Assert.Equal(GameDifficulty.Normal, restored.Difficulty);
        Assert.False(restored.IronmanMode);
        Assert.True(restored.IsDocked);
        Assert.Empty(restored.Cargo);
    }

    [Fact]
    public void Serialize_Deserialize_PreservesStatistics()
    {
        var player = new Player();
        player.Statistics.TotalCreditsEarned = 100000;
        player.Statistics.TotalCreditsSpent = 50000;
        player.Statistics.TradesCompleted = 42;
        player.Statistics.EnemiesDestroyed = 15;
        player.Statistics.PiratesDestroyed = 8;
        player.Statistics.DistanceTraveled = 12345.6;
        player.Statistics.MissionsCompleted = 10;
        player.Statistics.Deaths = 2;

        var json = player.Serialize();
        var restored = new Player();
        restored.Deserialize(json);

        Assert.Equal(100000, restored.Statistics.TotalCreditsEarned);
        Assert.Equal(50000, restored.Statistics.TotalCreditsSpent);
        Assert.Equal(42, restored.Statistics.TradesCompleted);
        Assert.Equal(15, restored.Statistics.EnemiesDestroyed);
        Assert.Equal(8, restored.Statistics.PiratesDestroyed);
        Assert.Equal(12345.6, restored.Statistics.DistanceTraveled);
        Assert.Equal(10, restored.Statistics.MissionsCompleted);
        Assert.Equal(2, restored.Statistics.Deaths);
    }

    [Fact]
    public void Serialize_Deserialize_PreservesSkillsAndReputation()
    {
        var player = new Player();
        SkillRegistry.InitializeDefaults();
        player.Skills.AddSkillXP("haggling", 500);
        player.Reputation.ChangeReputation("galactic_federation", 30, "Test");

        var json = player.Serialize();
        var restored = new Player();
        restored.Deserialize(json);

        Assert.True(restored.Skills.GetSkillLevel("haggling") > 0);
        Assert.Equal(30, restored.Reputation.GetReputation("galactic_federation"));
    }

    // ── Credits ──────────────────────────────────────────────────

    [Fact]
    public void AddCredits_IncreasesCreditsAndTracksEarnings()
    {
        var player = new Player { Credits = 1000 };
        player.AddCredits(500);

        Assert.Equal(1500, player.Credits);
        Assert.Equal(500, player.Statistics.TotalCreditsEarned);
    }

    [Fact]
    public void AddCredits_NegativeAmount_TracksAsSpent()
    {
        var player = new Player { Credits = 1000 };
        player.AddCredits(-300);

        Assert.Equal(700, player.Credits);
        Assert.Equal(0, player.Statistics.TotalCreditsEarned);
        Assert.Equal(300, player.Statistics.TotalCreditsSpent);
    }

    [Fact]
    public void RemoveCredits_SufficientFunds_ReturnsTrue()
    {
        var player = new Player { Credits = 1000 };
        var result = player.RemoveCredits(400);

        Assert.True(result);
        Assert.Equal(600, player.Credits);
        Assert.Equal(400, player.Statistics.TotalCreditsSpent);
    }

    [Fact]
    public void RemoveCredits_InsufficientFunds_ReturnsFalse()
    {
        var player = new Player { Credits = 100 };
        var result = player.RemoveCredits(500);

        Assert.False(result);
        Assert.Equal(100, player.Credits); // unchanged
    }

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(100, 100, true)]
    [InlineData(100, 101, false)]
    [InlineData(0, 0, true)]
    [InlineData(0, 1, false)]
    public void CanAfford_ReturnsCorrectResult(long credits, long amount, bool expected)
    {
        var player = new Player { Credits = credits };
        Assert.Equal(expected, player.CanAfford(amount));
    }

    // ── Cargo Management ─────────────────────────────────────────

    [Fact]
    public void AddCargo_WithinCapacity_ReturnsTrue()
    {
        var player = new Player { CargoCapacity = 50 };
        var result = player.AddCargo("water", 20);

        Assert.True(result);
        Assert.Equal(20, player.GetCargoQuantity("water"));
        Assert.Equal(20, player.GetTotalCargoUsed());
    }

    [Fact]
    public void AddCargo_ExceedsCapacity_ReturnsFalse()
    {
        var player = new Player { CargoCapacity = 10 };
        var result = player.AddCargo("ore", 15);

        Assert.False(result);
        Assert.Equal(0, player.GetCargoQuantity("ore"));
    }

    [Fact]
    public void AddCargo_MultipleAdditions_Accumulates()
    {
        var player = new Player { CargoCapacity = 100 };
        player.AddCargo("water", 10);
        player.AddCargo("water", 15);

        Assert.Equal(25, player.GetCargoQuantity("water"));
    }

    [Fact]
    public void RemoveCargo_SufficientQuantity_ReturnsTrue()
    {
        var player = new Player { CargoCapacity = 50 };
        player.AddCargo("water", 30);
        var result = player.RemoveCargo("water", 10);

        Assert.True(result);
        Assert.Equal(20, player.GetCargoQuantity("water"));
    }

    [Fact]
    public void RemoveCargo_ExactQuantity_RemovesEntry()
    {
        var player = new Player { CargoCapacity = 50 };
        player.AddCargo("water", 10);
        var result = player.RemoveCargo("water", 10);

        Assert.True(result);
        Assert.Equal(0, player.GetCargoQuantity("water"));
        Assert.False(player.Cargo.ContainsKey("water"));
    }

    [Fact]
    public void RemoveCargo_InsufficientQuantity_ReturnsFalse()
    {
        var player = new Player { CargoCapacity = 50 };
        player.AddCargo("water", 5);
        var result = player.RemoveCargo("water", 10);

        Assert.False(result);
        Assert.Equal(5, player.GetCargoQuantity("water"));
    }

    [Fact]
    public void RemoveCargo_NonexistentCommodity_ReturnsFalse()
    {
        var player = new Player();
        var result = player.RemoveCargo("nonexistent", 1);

        Assert.False(result);
    }

    [Fact]
    public void GetAvailableCargoSpace_ReturnsCorrectValue()
    {
        var player = new Player { CargoCapacity = 100 };
        player.AddCargo("water", 30);
        player.AddCargo("ore", 20);

        Assert.Equal(50, player.GetAvailableCargoSpace());
    }

    [Fact]
    public void GetTotalCargoUsed_EmptyCargo_ReturnsZero()
    {
        var player = new Player();
        Assert.Equal(0, player.GetTotalCargoUsed());
    }

    // ── Damage & Healing ─────────────────────────────────────────

    [Fact]
    public void TakeDamage_ReducesHealthAndTracksDamage()
    {
        var player = new Player { Health = 100, MaxHealth = 100 };
        player.TakeDamage(30);

        Assert.Equal(70, player.Health);
        Assert.Equal(30, player.Statistics.DamageTaken);
    }

    [Fact]
    public void TakeDamage_ExceedsHealth_ClampsToZero()
    {
        // Use IronmanMode to prevent respawn, so health stays at 0
        var player = new Player { Health = 20, MaxHealth = 100, IronmanMode = true };
        player.TakeDamage(50);

        Assert.Equal(0, player.Health);
        Assert.Equal(50, player.Statistics.DamageTaken);
    }

    [Fact]
    public void TakeDamage_Fatal_TriggersDeath()
    {
        var player = new Player
        {
            Health = 10,
            MaxHealth = 100,
            Credits = 10000,
            HomeBaseId = "neon_station",
            IronmanMode = false
        };
        player.TakeDamage(20);

        Assert.Equal(1, player.Statistics.Deaths);
        Assert.Equal(50, player.Health); // respawn at half health
        Assert.Equal("neon_station", player.CurrentLocationId);
        Assert.True(player.IsDocked);
        Assert.False(player.InCombat);
        Assert.True(player.Credits < 10000); // lost 10% credits
    }

    [Fact]
    public void Heal_RestoresHealthUpToMax()
    {
        var player = new Player { Health = 50, MaxHealth = 100 };
        player.Heal(30);

        Assert.Equal(80, player.Health);
    }

    [Fact]
    public void Heal_ExceedsMaxHealth_ClampsToMax()
    {
        var player = new Player { Health = 90, MaxHealth = 100 };
        player.Heal(50);

        Assert.Equal(100, player.Health);
    }

    [Fact]
    public void DamageHull_ReducesHullAndTracksDamage()
    {
        var player = new Player { ShipHull = 100, ShipMaxHull = 100 };
        player.DamageHull(25);

        Assert.Equal(75, player.ShipHull);
        Assert.Equal(25, player.Statistics.HullDamageTaken);
    }

    [Fact]
    public void DamageHull_Fatal_TriggersShipDestroyed()
    {
        var player = new Player
        {
            ShipHull = 10,
            ShipMaxHull = 100,
            HomeBaseId = "neon_station",
            IronmanMode = false
        };
        player.Cargo["water"] = 50;
        player.DamageHull(20);

        Assert.Equal(1, player.Statistics.ShipsLost);
        Assert.Equal(25, player.ShipHull); // respawn at quarter hull
        Assert.Equal(0, player.ShipShields);
        Assert.Equal("neon_station", player.CurrentLocationId);
        Assert.True(player.IsDocked);
        Assert.False(player.InCombat);
        Assert.Empty(player.Cargo); // cargo lost
    }

    [Fact]
    public void RepairHull_RestoresHullUpToMax()
    {
        var player = new Player { ShipHull = 40, ShipMaxHull = 100 };
        player.RepairHull(30);

        Assert.Equal(70, player.ShipHull);
    }

    [Fact]
    public void DamageShields_ReducesShields()
    {
        var player = new Player { ShipShields = 100, ShipMaxShields = 100 };
        player.DamageShields(40);

        Assert.Equal(60, player.ShipShields);
    }

    [Fact]
    public void RechargeShields_RestoresShieldsUpToMax()
    {
        var player = new Player { ShipShields = 30, ShipMaxShields = 100 };
        player.RechargeShields(50);

        Assert.Equal(80, player.ShipShields);
    }

    // ── Fuel ─────────────────────────────────────────────────────

    [Fact]
    public void ConsumeFuel_SufficientFuel_ReturnsTrue()
    {
        var player = new Player { CurrentFuel = 100, MaxFuel = 100 };
        var result = player.ConsumeFuel(30);

        Assert.True(result);
        Assert.Equal(70, player.CurrentFuel);
        Assert.Equal(30, player.Statistics.FuelConsumed);
    }

    [Fact]
    public void ConsumeFuel_InsufficientFuel_ReturnsFalse()
    {
        var player = new Player { CurrentFuel = 10, MaxFuel = 100 };
        var result = player.ConsumeFuel(50);

        Assert.False(result);
        Assert.Equal(10, player.CurrentFuel);
    }

    [Fact]
    public void Refuel_RestoresFuelUpToMax()
    {
        var player = new Player { CurrentFuel = 20, MaxFuel = 100 };
        player.Refuel(60);

        Assert.Equal(80, player.CurrentFuel);
    }

    // ── Leveling & Experience ────────────────────────────────────

    [Fact]
    public void GetXPForLevel_ReturnsCorrectValues()
    {
        Assert.Equal(1000, Player.GetXPForLevel(1));   // 1000 * 1^2
        Assert.Equal(4000, Player.GetXPForLevel(2));   // 1000 * 2^2
        Assert.Equal(9000, Player.GetXPForLevel(3));   // 1000 * 3^2
        Assert.Equal(16000, Player.GetXPForLevel(4));  // 1000 * 4^2
    }

    [Fact]
    public void GetTotalXPForLevel_ReturnsCumulativeXP()
    {
        // Level 1: 0 XP needed (starting level)
        // Level 2: 1000 XP
        // Level 3: 1000 + 4000 = 5000 XP
        // Level 4: 1000 + 4000 + 9000 = 14000 XP
        Assert.Equal(0, Player.GetTotalXPForLevel(1));
        Assert.Equal(1000, Player.GetTotalXPForLevel(2));
        Assert.Equal(5000, Player.GetTotalXPForLevel(3));
        Assert.Equal(14000, Player.GetTotalXPForLevel(4));
    }

    [Fact]
    public void AddExperience_LevelsUp_WhenThresholdReached()
    {
        var player = new Player { Level = 1, Experience = 0, SkillPoints = 0 };
        // GetXPForLevel(2) = 1000 * 2^2 = 4000 XP needed for level 2
        player.AddExperience(4000);

        Assert.Equal(2, player.Level);
        Assert.Equal(1, player.SkillPoints); // gained 1 skill point
        Assert.Equal(0, player.Experience); // remainder consumed
    }

    [Fact]
    public void AddExperience_MultipleLevels_LevelsUpCorrectly()
    {
        var player = new Player { Level = 1, Experience = 0, SkillPoints = 0 };
        // L2: 4000, L3: 9000 = 13000 total
        player.AddExperience(13000);

        Assert.Equal(3, player.Level);
        Assert.Equal(2, player.SkillPoints);
        Assert.Equal(0, player.Experience);
    }

    [Fact]
    public void AddExperience_Overflow_KeepsRemainder()
    {
        var player = new Player { Level = 1, Experience = 0, SkillPoints = 0 };
        // Need 4000 for L2, give 5000 -> level up to 2 with 1000 remainder
        player.AddExperience(5000);

        Assert.Equal(2, player.Level);
        Assert.Equal(1, player.SkillPoints);
        Assert.Equal(1000, player.Experience);
    }

    [Fact]
    public void AddExperience_LevelCap_DoesNotExceed100()
    {
        var player = new Player { Level = 99, Experience = 0, SkillPoints = 0 };
        // Give massive XP
        player.AddExperience(100000000);

        Assert.Equal(100, player.Level);
    }

    // ── Missions ─────────────────────────────────────────────────

    [Fact]
    public void StartMission_AddsToActiveAndTracks()
    {
        var player = new Player();
        player.StartMission("mission_001");

        Assert.Contains("mission_001", player.ActiveMissionIds);
        Assert.Equal(1, player.Statistics.MissionsStarted);
    }

    [Fact]
    public void CompleteMission_MovesToCompletedAndTracksReward()
    {
        var player = new Player();
        player.StartMission("mission_001");
        player.CompleteMission("mission_001", 5000);

        Assert.DoesNotContain("mission_001", player.ActiveMissionIds);
        Assert.Contains("mission_001", player.CompletedMissionIds);
        Assert.Equal(1, player.Statistics.MissionsCompleted);
        Assert.Equal(5000, player.Statistics.TotalMissionRewards);
    }

    [Fact]
    public void FailMission_MovesToFailedAndTracks()
    {
        var player = new Player();
        player.StartMission("mission_001");
        player.FailMission("mission_001");

        Assert.DoesNotContain("mission_001", player.ActiveMissionIds);
        Assert.Contains("mission_001", player.FailedMissionIds);
        Assert.Equal(1, player.Statistics.MissionsFailed);
    }

    // ── Discovery & Travel ───────────────────────────────────────

    [Fact]
    public void DiscoverLocation_NewLocation_AddsAndTracks()
    {
        var player = new Player();
        player.DiscoverLocation("new_system");

        Assert.Contains("new_system", player.DiscoveredLocations);
        Assert.Equal(1, player.Statistics.LocationsDiscovered);
    }

    [Fact]
    public void DiscoverLocation_Duplicate_DoesNotDoubleCount()
    {
        var player = new Player();
        player.DiscoverLocation("alpha_centauri");
        player.DiscoverLocation("alpha_centauri");

        Assert.Equal(1, player.Statistics.LocationsDiscovered);
    }

    [Fact]
    public void LearnRoute_AddsRoute()
    {
        var player = new Player();
        player.LearnRoute("sol_to_vega");

        Assert.Contains("sol_to_vega", player.KnownRoutes);
    }

    [Fact]
    public void RecordTravel_TracksDistance()
    {
        var player = new Player();
        player.RecordTravel(42.5);

        Assert.Equal(42.5, player.Statistics.DistanceTraveled);
    }

    // ── Trade Recording ──────────────────────────────────────────

    [Fact]
    public void RecordTrade_TracksProfit()
    {
        var player = new Player();
        player.RecordTrade(1500);

        Assert.Equal(1, player.Statistics.TradesCompleted);
        Assert.Equal(1500, player.Statistics.TotalTradeProfit);
    }

    // ── Combat Recording ─────────────────────────────────────────

    [Theory]
    [InlineData("pirate_scout", 1, 1, 0, 0)]
    [InlineData("police_cruiser", 1, 0, 1, 0)]
    [InlineData("alien_drone", 1, 0, 0, 1)]
    [InlineData("generic_enemy", 1, 0, 0, 0)]
    public void RecordKill_TracksCorrectCategory(string enemyType, int total, int pirates, int police, int aliens)
    {
        var player = new Player();
        player.RecordKill(enemyType);

        Assert.Equal(total, player.Statistics.EnemiesDestroyed);
        Assert.Equal(pirates, player.Statistics.PiratesDestroyed);
        Assert.Equal(police, player.Statistics.PoliceDestroyed);
        Assert.Equal(aliens, player.Statistics.AliensDestroyed);
    }

    // ── Background Bonuses ───────────────────────────────────────

    [Fact]
    public void CreateNew_Merchant_GetsBonusCreditsAndSkills()
    {
        SkillRegistry.InitializeDefaults();
        var player = Player.CreateNew("TraderJoe", "merchant");

        Assert.Equal(15000, player.Credits); // 10000 + 5000
        Assert.True(player.Skills.GetSkillLevel("haggling") > 0);
        Assert.True(player.Skills.GetSkillLevel("market_analysis") > 0);
    }

    [Fact]
    public void CreateNew_Pilot_GetsBonusSkillsAndFullFuel()
    {
        SkillRegistry.InitializeDefaults();
        var player = Player.CreateNew("AcePilot", "pilot");

        Assert.True(player.Skills.GetSkillLevel("navigation") > 0);
        Assert.True(player.Skills.GetSkillLevel("evasion") > 0);
        Assert.Equal(player.MaxFuel, player.CurrentFuel);
    }

    [Fact]
    public void CreateNew_Combat_GetsBonusSkillsAndFullShip()
    {
        SkillRegistry.InitializeDefaults();
        var player = Player.CreateNew("Fighter", "combat");

        Assert.True(player.Skills.GetSkillLevel("gunnery") > 0);
        Assert.True(player.Skills.GetSkillLevel("shields") > 0);
        Assert.Equal(player.ShipMaxHull, player.ShipHull);
        Assert.Equal(player.ShipMaxShields, player.ShipShields);
    }

    [Fact]
    public void CreateNew_Engineer_GetsBonusSkillsAndUpgrade()
    {
        SkillRegistry.InitializeDefaults();
        var player = Player.CreateNew("Engineer", "engineer");

        Assert.True(player.Skills.GetSkillLevel("repair") > 0);
        Assert.True(player.Skills.GetSkillLevel("systems_engineering") > 0);
        Assert.Contains("basic_repair_drone", player.InstalledUpgrades);
    }

    [Fact]
    public void CreateNew_Smuggler_GetsBonusCreditsSkillsAndReputation()
    {
        SkillRegistry.InitializeDefaults();
        var player = Player.CreateNew("SneakyPete", "smuggler");

        Assert.Equal(12000, player.Credits); // 10000 + 2000
        Assert.True(player.Skills.GetSkillLevel("evasion") > 0);
        Assert.True(player.Skills.GetSkillLevel("haggling") > 0);
        Assert.Contains("shielded_cargo_hold", player.InstalledUpgrades);
        Assert.Equal(10, player.Reputation.GetReputation("crimson_fleet"));
        Assert.Equal(10, player.Reputation.GetReputation("shadow_syndicate"));
    }

    [Fact]
    public void CreateNew_DefaultBackground_IsMerchant()
    {
        SkillRegistry.InitializeDefaults();
        var player = Player.CreateNew("DefaultJoe");

        Assert.Equal("merchant", player.Background);
        Assert.Equal(15000, player.Credits);
    }

    // ── Ironman Mode ─────────────────────────────────────────────

    [Fact]
    public void IronmanMode_Death_DoesNotRespawn()
    {
        var player = new Player
        {
            Health = 10,
            MaxHealth = 100,
            IronmanMode = true,
            HomeBaseId = "neon_station"
        };
        player.TakeDamage(20);

        Assert.Equal(0, player.Health);
        Assert.Equal(1, player.Statistics.Deaths);
        // In ironman mode, OnDeath does not respawn
        // Health stays at 0 (game over handled by game manager)
    }

    [Fact]
    public void IronmanMode_ShipDestroyed_DoesNotRespawn()
    {
        var player = new Player
        {
            ShipHull = 10,
            ShipMaxHull = 100,
            IronmanMode = true,
            HomeBaseId = "neon_station"
        };
        player.Cargo["water"] = 50;
        player.DamageHull(20);

        Assert.Equal(0, player.ShipHull);
        Assert.Equal(1, player.Statistics.ShipsLost);
        // In ironman mode, OnShipDestroyed does not respawn
    }

    // ── PlayerStatistics Serialization ───────────────────────────

    [Fact]
    public void PlayerStatistics_Serialize_Deserialize_RoundTrip()
    {
        var stats = new PlayerStatistics
        {
            TotalCreditsEarned = 500000,
            TotalCreditsSpent = 300000,
            TotalCreditsLost = 10000,
            TotalTradeProfit = 200000,
            TradesCompleted = 150,
            BestSingleTrade = 50000,
            LargestCargoHaul = 200,
            EnemiesDestroyed = 75,
            PiratesDestroyed = 30,
            PoliceDestroyed = 5,
            AliensDestroyed = 10,
            DamageDealt = 50000,
            DamageTaken = 25000,
            HullDamageTaken = 15000,
            ShipsDestroyed = 20,
            ShipsLost = 2,
            DistanceTraveled = 50000.0,
            SystemsVisited = 25,
            LocationsDiscovered = 40,
            FuelConsumed = 10000,
            JumpsMade = 200,
            MissionsStarted = 50,
            MissionsCompleted = 45,
            MissionsFailed = 5,
            TotalMissionRewards = 250000,
            BestMissionReward = 50000,
            Deaths = 3,
            TotalCargoLost = 100,
            GameStartTime = new DateTime(2087, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TotalPlayTime = TimeSpan.FromHours(100)
        };

        var json = stats.Serialize();
        var restored = new PlayerStatistics();
        restored.Deserialize(json);

        Assert.Equal(stats.TotalCreditsEarned, restored.TotalCreditsEarned);
        Assert.Equal(stats.TotalCreditsSpent, restored.TotalCreditsSpent);
        Assert.Equal(stats.TotalCreditsLost, restored.TotalCreditsLost);
        Assert.Equal(stats.TotalTradeProfit, restored.TotalTradeProfit);
        Assert.Equal(stats.TradesCompleted, restored.TradesCompleted);
        Assert.Equal(stats.BestSingleTrade, restored.BestSingleTrade);
        Assert.Equal(stats.LargestCargoHaul, restored.LargestCargoHaul);
        Assert.Equal(stats.EnemiesDestroyed, restored.EnemiesDestroyed);
        Assert.Equal(stats.PiratesDestroyed, restored.PiratesDestroyed);
        Assert.Equal(stats.PoliceDestroyed, restored.PoliceDestroyed);
        Assert.Equal(stats.AliensDestroyed, restored.AliensDestroyed);
        Assert.Equal(stats.DamageDealt, restored.DamageDealt);
        Assert.Equal(stats.DamageTaken, restored.DamageTaken);
        Assert.Equal(stats.HullDamageTaken, restored.HullDamageTaken);
        Assert.Equal(stats.ShipsDestroyed, restored.ShipsDestroyed);
        Assert.Equal(stats.ShipsLost, restored.ShipsLost);
        Assert.Equal(stats.DistanceTraveled, restored.DistanceTraveled);
        Assert.Equal(stats.SystemsVisited, restored.SystemsVisited);
        Assert.Equal(stats.LocationsDiscovered, restored.LocationsDiscovered);
        Assert.Equal(stats.FuelConsumed, restored.FuelConsumed);
        Assert.Equal(stats.JumpsMade, restored.JumpsMade);
        Assert.Equal(stats.MissionsStarted, restored.MissionsStarted);
        Assert.Equal(stats.MissionsCompleted, restored.MissionsCompleted);
        Assert.Equal(stats.MissionsFailed, restored.MissionsFailed);
        Assert.Equal(stats.TotalMissionRewards, restored.TotalMissionRewards);
        Assert.Equal(stats.BestMissionReward, restored.BestMissionReward);
        Assert.Equal(stats.Deaths, restored.Deaths);
        Assert.Equal(stats.TotalCargoLost, restored.TotalCargoLost);
        Assert.Equal(stats.TotalPlayTime, restored.TotalPlayTime);
    }

    // ── ISaveable Interface ──────────────────────────────────────

    [Fact]
    public void SaveId_ReturnsCorrectFormat()
    {
        var player = new Player { PlayerId = "abc-123" };
        Assert.Equal("player_abc-123", player.SaveId);
    }

    [Fact]
    public void SaveVersion_IsOne()
    {
        var player = new Player();
        Assert.Equal(1, player.SaveVersion);
    }
}
