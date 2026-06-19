using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

[Collection("Sequential")]
public class ReputationTests
{
    [Fact]
    public void Reputation_Serialize_Deserialize_RoundTrip()
    {
        var rep = new Reputation();
        rep.ChangeReputation("galactic_federation", 50, "Completed mission");
        rep.ChangeReputation("crimson_fleet", -30, "Attacked their ships");
        rep.ChangeReputation("shadow_syndicate", 75, "Trade deal");
        var json = rep.Serialize();
        var restored = new Reputation();
        restored.Deserialize(json);
        Assert.Equal(50, restored.GetReputation("galactic_federation"));
        Assert.Equal(-30, restored.GetReputation("crimson_fleet"));
        Assert.Equal(75, restored.GetReputation("shadow_syndicate"));
        Assert.Equal(3, restored.EventLog.Count);
    }

    [Fact]
    public void Reputation_Serialize_Deserialize_EmptyReputation()
    {
        var rep = new Reputation();
        var json = rep.Serialize();
        var restored = new Reputation();
        restored.Deserialize(json);
        Assert.Empty(restored.FactionReputations);
        Assert.Empty(restored.EventLog);
    }

    [Fact]
    public void Reputation_Deserialize_ClampsValues()
    {
        var json = new JObject { ["factionReputations"] = new JObject { ["test_faction"] = 150 }, ["eventLog"] = new JArray() };
        var rep = new Reputation();
        rep.Deserialize(json);
        Assert.Equal(100, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void ReputationEvent_Serialize_Deserialize_RoundTrip()
    {
        var evt = new ReputationEvent { FactionId = "galactic_federation", Timestamp = new DateTime(2087, 6, 15, 12, 0, 0, DateTimeKind.Utc), OldValue = 10, NewValue = 25, Delta = 15, Reason = "Completed mission" };
        var json = evt.Serialize();
        var restored = new ReputationEvent();
        restored.Deserialize(json);
        Assert.Equal("galactic_federation", restored.FactionId);
        Assert.Equal(10, restored.OldValue);
        Assert.Equal(25, restored.NewValue);
        Assert.Equal(15, restored.Delta);
        Assert.Equal("Completed mission", restored.Reason);
    }

    [Fact]
    public void GetReputation_ExistingFaction_ReturnsValue()
    {
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 30);
        Assert.Equal(30, rep.GetReputation("galactic_federation"));
    }

    [Fact]
    public void GetReputation_NonexistentFaction_ReturnsZero()
    {
        Assert.Equal(0, new Reputation().GetReputation("unknown_faction"));
    }

    [Fact]
    public void SetReputation_ClampsToRange()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", 150);
        Assert.Equal(100, rep.GetReputation("test_faction"));
        rep.SetReputation("test_faction", -200);
        Assert.Equal(-100, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void ChangeReputation_PositiveDelta_IncreasesReputation()
    {
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 10);
        var newValue = rep.ChangeReputation("galactic_federation", 15, "Mission reward");
        Assert.Equal(25, newValue);
        Assert.Equal(25, rep.GetReputation("galactic_federation"));
    }

    [Fact]
    public void ChangeReputation_NegativeDelta_DecreasesReputation()
    {
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 10);
        var newValue = rep.ChangeReputation("galactic_federation", -20, "Attacked ships");
        Assert.Equal(-10, newValue);
    }

    [Fact]
    public void ChangeReputation_ClampsAtBoundaries()
    {
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 95);
        var newValue = rep.ChangeReputation("galactic_federation", 20, "Big reward");
        Assert.Equal(100, newValue);
    }

    [Fact]
    public void ChangeReputation_LogsEvent()
    {
        var rep = new Reputation();
        rep.ChangeReputation("galactic_federation", 10, "Test reason");
        Assert.Single(rep.EventLog);
        var evt = rep.EventLog[0];
        Assert.Equal("galactic_federation", evt.FactionId);
        Assert.Equal(0, evt.OldValue);
        Assert.Equal(10, evt.NewValue);
        Assert.Equal(10, evt.Delta);
        Assert.Equal("Test reason", evt.Reason);
    }

    [Fact]
    public void ChangeReputation_ZeroDelta_DoesNotLog()
    {
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 100);
        var eventCount = rep.EventLog.Count;
        rep.ChangeReputation("galactic_federation", 10, "Should not change");
        Assert.Equal(eventCount, rep.EventLog.Count);
    }

    [Theory]
    [InlineData(100, ReputationTier.Exalted)]
    [InlineData(95, ReputationTier.Exalted)]
    [InlineData(90, ReputationTier.Exalted)]
    [InlineData(80, ReputationTier.Revered)]
    [InlineData(75, ReputationTier.Revered)]
    [InlineData(60, ReputationTier.Honored)]
    [InlineData(50, ReputationTier.Honored)]
    [InlineData(30, ReputationTier.Respected)]
    [InlineData(25, ReputationTier.Respected)]
    [InlineData(10, ReputationTier.Friendly)]
    [InlineData(0, ReputationTier.Friendly)]
    [InlineData(-10, ReputationTier.Neutral)]
    [InlineData(-30, ReputationTier.Neutral)]
    [InlineData(-50, ReputationTier.Unfriendly)]
    [InlineData(-70, ReputationTier.Unfriendly)]
    [InlineData(-80, ReputationTier.Hostile)]
    [InlineData(-100, ReputationTier.Hostile)]
    public void GetTierFromValue_ReturnsCorrectTier(int reputation, ReputationTier expected)
    {
        Assert.Equal(expected, Reputation.GetTierFromValue(reputation));
    }

    [Fact]
    public void GetTier_ReturnsCorrectTierForFaction()
    {
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 60);
        Assert.Equal(ReputationTier.Honored, rep.GetTier("galactic_federation"));
    }

    [Fact]
    public void GetTier_NonexistentFaction_ReturnsFriendly()
    {
        Assert.Equal(ReputationTier.Friendly, new Reputation().GetTier("unknown_faction"));
    }

    [Theory]
    [InlineData(100, "Exalted")]
    [InlineData(90, "Exalted")]
    [InlineData(80, "Revered")]
    [InlineData(75, "Revered")]
    [InlineData(60, "Honored")]
    [InlineData(50, "Honored")]
    [InlineData(30, "Respected")]
    [InlineData(25, "Respected")]
    [InlineData(10, "Friendly")]
    [InlineData(0, "Friendly")]
    [InlineData(-10, "Neutral")]
    [InlineData(-30, "Neutral")]
    [InlineData(-50, "Unfriendly")]
    [InlineData(-70, "Unfriendly")]
    [InlineData(-80, "Hostile")]
    [InlineData(-100, "Hostile")]
    public void GetTierName_ReturnsCorrectName(int reputation, string expected)
    {
        Assert.Equal(expected, Reputation.GetTierName(reputation));
    }

    [Theory]
    [InlineData(100, -0.2)]
    [InlineData(50, -0.1)]
    [InlineData(0, 0)]
    [InlineData(-50, 0.1)]
    [InlineData(-100, 0.2)]
    public void GetPriceModifier_ReturnsCorrectModifier(int reputation, decimal expected)
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", reputation);
        Assert.Equal(expected, rep.GetPriceModifier("test_faction"));
    }

    [Fact]
    public void GetPriceModifier_ClampsToReasonableRange()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", 200);
        var modifier = rep.GetPriceModifier("test_faction");
        Assert.True(modifier >= -0.25m && modifier <= 0.25m);
    }

    [Theory]
    [InlineData(100, 1.5f)]
    [InlineData(80, 1.3f)]
    [InlineData(60, 1.2f)]
    [InlineData(30, 1.1f)]
    [InlineData(10, 1.0f)]
    [InlineData(-10, 0.8f)]
    [InlineData(-50, 0.5f)]
    [InlineData(-80, 0.2f)]
    [InlineData(-100, 0.2f)] // Hostile tier (GetTierFromValue returns Hostile for -100)
    public void GetMissionAvailabilityModifier_ReturnsCorrectValue(int reputation, float expected)
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", reputation);
        Assert.Equal(expected, rep.GetMissionAvailabilityModifier("test_faction"));
    }

    [Theory]
    [InlineData(100, 1.5f)]
    [InlineData(0, 1.0f)]
    [InlineData(-100, 0.7f)]
    public void GetMissionRewardModifier_ReturnsCorrectValue(int reputation, float expected)
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", reputation);
        Assert.Equal(expected, rep.GetMissionRewardModifier("test_faction"));
    }

    [Fact]
    public void CanAccessBlackMarket_FactionOffersAndHighRep_ReturnsTrue()
    {
        FactionRegistry.Register(new Faction { Id = "shadow_syndicate", Name = "Shadow Syndicate", OffersBlackMarket = true });
        var rep = new Reputation();
        rep.SetReputation("shadow_syndicate", 60);
        Assert.True(rep.CanAccessBlackMarket("shadow_syndicate"));
    }

    [Fact]
    public void CanAccessBlackMarket_LowRep_ReturnsFalse()
    {
        FactionRegistry.Register(new Faction { Id = "shadow_syndicate", Name = "Shadow Syndicate", OffersBlackMarket = true });
        var rep = new Reputation();
        rep.SetReputation("shadow_syndicate", 30);
        Assert.False(rep.CanAccessBlackMarket("shadow_syndicate"));
    }

    [Fact]
    public void CanAccessBlackMarket_FactionDoesNotOffer_ReturnsFalse()
    {
        FactionRegistry.Register(new Faction { Id = "honest_traders", Name = "Honest Traders", OffersBlackMarket = false });
        var rep = new Reputation();
        rep.SetReputation("honest_traders", 80);
        Assert.False(rep.CanAccessBlackMarket("honest_traders"));
    }

    [Fact]
    public void CanAccessBlackMarket_NonexistentFaction_ReturnsFalse()
    {
        Assert.False(new Reputation().CanAccessBlackMarket("nonexistent"));
    }

    [Fact]
    public void CanAccessShop_ReputationAboveThreshold_ReturnsTrue()
    {
        FactionRegistry.Register(new Faction { Id = "galactic_federation", Name = "Galactic Federation", ShopUnlockReputation = 50 });
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 60);
        Assert.True(rep.CanAccessShop("galactic_federation"));
    }

    [Fact]
    public void CanAccessShop_ReputationBelowThreshold_ReturnsFalse()
    {
        FactionRegistry.Register(new Faction { Id = "galactic_federation", Name = "Galactic Federation", ShopUnlockReputation = 50 });
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 30);
        Assert.False(rep.CanAccessShop("galactic_federation"));
    }

    [Fact]
    public void CanGetMissions_ReputationAboveThreshold_ReturnsTrue()
    {
        FactionRegistry.Register(new Faction { Id = "galactic_federation", Name = "Galactic Federation", MissionUnlockReputation = 25 });
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 30);
        Assert.True(rep.CanGetMissions("galactic_federation"));
    }

    [Fact]
    public void CanGetMissions_ReputationBelowThreshold_ReturnsFalse()
    {
        FactionRegistry.Register(new Faction { Id = "galactic_federation", Name = "Galactic Federation", MissionUnlockReputation = 25 });
        var rep = new Reputation();
        rep.SetReputation("galactic_federation", 10);
        Assert.False(rep.CanGetMissions("galactic_federation"));
    }

    [Fact]
    public void EventLog_TrimsAtMaxSize()
    {
        var rep = new Reputation();
        for (int i = 0; i < 150; i++)
            rep.ChangeReputation("test_faction", 1, $"Event {i}");
        Assert.True(rep.EventLog.Count <= Reputation.MaxEventLogSize);
    }

    [Fact]
    public void GetEventsForFaction_ReturnsFilteredEvents()
    {
        var rep = new Reputation();
        rep.ChangeReputation("faction_a", 10, "Event A1");
        rep.ChangeReputation("faction_b", 10, "Event B1");
        rep.ChangeReputation("faction_a", 10, "Event A2");
        var events = rep.GetEventsForFaction("faction_a");
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("faction_a", e.FactionId));
    }

    [Fact]
    public void GetEventsForFaction_NoEvents_ReturnsEmpty()
    {
        Assert.Empty(new Reputation().GetEventsForFaction("unknown_faction"));
    }

    [Fact]
    public void GetFactionsAboveThreshold_ReturnsCorrectFactions()
    {
        var rep = new Reputation();
        rep.SetReputation("faction_a", 60);
        rep.SetReputation("faction_b", 30);
        rep.SetReputation("faction_c", 80);
        var above = rep.GetFactionsAboveThreshold(50);
        Assert.Equal(2, above.Count());
        Assert.Contains("faction_a", above);
        Assert.Contains("faction_c", above);
        Assert.DoesNotContain("faction_b", above);
    }

    [Fact]
    public void GetFactionsBelowThreshold_ReturnsCorrectFactions()
    {
        var rep = new Reputation();
        rep.SetReputation("faction_a", -50);
        rep.SetReputation("faction_b", 10);
        rep.SetReputation("faction_c", -80);
        var below = rep.GetFactionsBelowThreshold(-30);
        Assert.Equal(2, below.Count());
        Assert.Contains("faction_a", below);
        Assert.Contains("faction_c", below);
        Assert.DoesNotContain("faction_b", below);
    }

    [Fact]
    public void ApplyDecay_PositiveReputation_DecaysTowardZero()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", 50);
        rep.ApplyDecay(TimeSpan.FromDays(10), decayPerDay: 2);
        Assert.Equal(30, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void ApplyDecay_NegativeReputation_DecaysTowardZero()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", -50);
        rep.ApplyDecay(TimeSpan.FromDays(10), decayPerDay: 2);
        Assert.Equal(-30, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void ApplyDecay_StopsAtZero()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", 5);
        rep.ApplyDecay(TimeSpan.FromDays(10), decayPerDay: 2);
        Assert.Equal(0, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void ApplyDecay_NeutralReputation_StaysNeutral()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", 0);
        rep.ApplyDecay(TimeSpan.FromDays(10), decayPerDay: 2);
        Assert.Equal(0, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void ApplyDecay_ZeroTime_DoesNothing()
    {
        var rep = new Reputation();
        rep.SetReputation("test_faction", 50);
        rep.ApplyDecay(TimeSpan.Zero, decayPerDay: 2);
        Assert.Equal(50, rep.GetReputation("test_faction"));
    }

    [Fact]
    public void InitializeNewGame_SetsStartingReputations()
    {
        FactionRegistry.Clear();
        FactionRegistry.Register(new Faction { Id = "galactic_federation", Name = "Galactic Federation", StartingReputation = 20 });
        FactionRegistry.Register(new Faction { Id = "crimson_fleet", Name = "Crimson Fleet", StartingReputation = -30 });
        var rep = new Reputation();
        rep.InitializeNewGame();
        Assert.Equal(20, rep.GetReputation("galactic_federation"));
        Assert.Equal(-30, rep.GetReputation("crimson_fleet"));
        Assert.Empty(rep.EventLog);
    }

    [Fact]
    public void ReputationModifiers_HaveExpectedValues()
    {
        Assert.Equal(1, ReputationModifiers.TradeProfitSmall);
        Assert.Equal(2, ReputationModifiers.TradeProfitMedium);
        Assert.Equal(5, ReputationModifiers.TradeProfitLarge);
        Assert.Equal(-1, ReputationModifiers.TradeLoss);
        Assert.Equal(2, ReputationModifiers.TradeFavoredCommodity);
        Assert.Equal(-10, ReputationModifiers.TradeBannedCommodity);
        Assert.Equal(5, ReputationModifiers.MissionSuccess);
        Assert.Equal(-10, ReputationModifiers.MissionFailure);
        Assert.Equal(-15, ReputationModifiers.DestroyedFactionShip);
        Assert.Equal(-50, ReputationModifiers.BetrayFaction);
    }

    [Fact]
    public void SaveId_IsCorrect() => Assert.Equal("reputation", new Reputation().SaveId);
    [Fact]
    public void SaveVersion_IsOne() => Assert.Equal(1, new Reputation().SaveVersion);
}
