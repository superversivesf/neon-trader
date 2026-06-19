using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Reputation system - tracks player standing with factions and its effects
/// </summary>
public sealed class Reputation : ISaveable
{
    /// <summary>
    /// Reputation values per faction (factionId -> reputation value, -100 to 100)
    /// </summary>
    public Dictionary<string, int> FactionReputations { get; } = new();

    /// <summary>
    /// Reputation tier thresholds
    /// </summary>
    public static readonly int[] TierThresholds = new[] { -100, -70, -30, 0, 25, 50, 75, 90, 100 };

    /// <summary>
    /// Reputation tier names
    /// </summary>
    public static readonly string[] TierNames = new[]
    {
        "Nemesis",      // -100 to -71
        "Hostile",      // -70 to -31
        "Unfriendly",   // -30 to -1
        "Neutral",      // 0 to 24
        "Friendly",     // 25 to 49
        "Respected",    // 50 to 74
        "Honored",      // 75 to 89
        "Revered",      // 90 to 100
        "Exalted"       // 100 (max)
    };

    /// <summary>
    /// Event log for reputation changes (for UI/history)
    /// </summary>
    public List<ReputationEvent> EventLog { get; } = new();

    /// <summary>
    /// Maximum event log size
    /// </summary>
    public const int MaxEventLogSize = 100;

    // ISaveable implementation
    public string SaveId => "reputation";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize reputation to JSON
    /// </summary>
    public JObject Serialize()
    {
        return new JObject
        {
            ["factionReputations"] = JObject.FromObject(FactionReputations),
            ["eventLog"] = JArray.FromObject(EventLog)
        };
    }

    /// <summary>
    /// Deserialize reputation from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        FactionReputations.Clear();
        if (data["factionReputations"] is JObject repObj)
        {
            foreach (var kvp in repObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    FactionReputations[kvp.Key] = Math.Clamp(value.Value, -100, 100);
            }
        }

        EventLog.Clear();
        if (data["eventLog"] is JArray logArray)
        {
            EventLog.AddRange(logArray.ToObject<List<ReputationEvent>>() ?? new());
        }
    }

    /// <summary>
    /// Gets reputation with a faction
    /// </summary>
    public int GetReputation(string factionId)
    {
        return FactionReputations.TryGetValue(factionId, out var rep) ? rep : 0;
    }

    /// <summary>
    /// Sets reputation with a faction (clamped to -100..100)
    /// </summary>
    public void SetReputation(string factionId, int value)
    {
        var clamped = Math.Clamp(value, -100, 100);
        FactionReputations[factionId] = clamped;
    }

    /// <summary>
    /// Changes reputation with a faction by delta amount
    /// </summary>
    public int ChangeReputation(string factionId, int delta, string reason = "")
    {
        var current = GetReputation(factionId);
        var newValue = Math.Clamp(current + delta, -100, 100);
        var actualDelta = newValue - current;
        
        if (actualDelta != 0)
        {
            FactionReputations[factionId] = newValue;
            AddEvent(factionId, current, newValue, actualDelta, reason);
        }
        
        return newValue;
    }

    /// <summary>
    /// Gets reputation tier for a faction
    /// </summary>
    public ReputationTier GetTier(string factionId)
    {
        var rep = GetReputation(factionId);
        return GetTierFromValue(rep);
    }

    /// <summary>
    /// Gets reputation tier from a numeric value
    /// </summary>
    public static ReputationTier GetTierFromValue(int reputation)
    {
        if (reputation >= 90) return ReputationTier.Exalted;
        if (reputation >= 75) return ReputationTier.Revered;
        if (reputation >= 50) return ReputationTier.Honored;
        if (reputation >= 25) return ReputationTier.Respected;
        if (reputation >= 0) return ReputationTier.Friendly;
        if (reputation >= -30) return ReputationTier.Neutral;
        if (reputation >= -70) return ReputationTier.Unfriendly;
        return ReputationTier.Hostile;
    }

    /// <summary>
    /// Gets reputation tier name from value
    /// </summary>
    public static string GetTierName(int reputation)
    {
        if (reputation >= 90) return "Exalted";
        if (reputation >= 75) return "Revered";
        if (reputation >= 50) return "Honored";
        if (reputation >= 25) return "Respected";
        if (reputation >= 0) return "Friendly";
        if (reputation >= -30) return "Neutral";
        if (reputation >= -70) return "Unfriendly";
        return "Hostile";
    }

    /// <summary>
    /// Gets price modifier for a faction based on reputation
    /// Positive = discount (player buys cheaper), negative = markup (player pays more)
    /// </summary>
    public decimal GetPriceModifier(string factionId)
    {
        var rep = GetReputation(factionId);
        
        // Reputation price modifier: -20% at +100 rep, +20% at -100 rep
        // Linear interpolation: modifier = -rep / 500
        // At +100: -0.2 (20% discount), at -100: +0.2 (20% markup)
        var modifier = -(decimal)rep / 500m;
        
        // Clamp to reasonable range
        return Math.Clamp(modifier, -0.25m, 0.25m);
    }

    /// <summary>
    /// Gets mission availability modifier based on reputation
    /// </summary>
    public float GetMissionAvailabilityModifier(string factionId)
    {
        var tier = GetTier(factionId);
        return tier switch
        {
            ReputationTier.Exalted => 1.5f,
            ReputationTier.Revered => 1.3f,
            ReputationTier.Honored => 1.2f,
            ReputationTier.Respected => 1.1f,
            ReputationTier.Friendly => 1.0f,
            ReputationTier.Neutral => 0.8f,
            ReputationTier.Unfriendly => 0.5f,
            ReputationTier.Hostile => 0.2f,
            ReputationTier.Nemesis => 0.0f,
            _ => 0.8f
        };
    }

    /// <summary>
    /// Gets mission reward modifier based on reputation
    /// </summary>
    public float GetMissionRewardModifier(string factionId)
    {
        var rep = GetReputation(factionId);
        
        // Reward bonus: +50% at +100, -30% at -100
        var modifier = 1.0f + (rep / 200f);
        return Math.Clamp(modifier, 0.7f, 1.5f);
    }

    /// <summary>
    /// Checks if player can access faction's black market
    /// </summary>
    public bool CanAccessBlackMarket(string factionId)
    {
        var faction = FactionRegistry.Get(factionId);
        if (faction == null || !faction.OffersBlackMarket)
            return false;
        
        var rep = GetReputation(factionId);
        return rep >= 50; // Respected or higher
    }

    /// <summary>
    /// Checks if player can access faction shop
    /// </summary>
    public bool CanAccessShop(string factionId)
    {
        var faction = FactionRegistry.Get(factionId);
        if (faction == null)
            return false;
        
        var rep = GetReputation(factionId);
        return rep >= faction.ShopUnlockReputation;
    }

    /// <summary>
    /// Checks if player can get faction missions
    /// </summary>
    public bool CanGetMissions(string factionId)
    {
        var faction = FactionRegistry.Get(factionId);
        if (faction == null)
            return false;
        
        var rep = GetReputation(factionId);
        return rep >= faction.MissionUnlockReputation;
    }

    /// <summary>
    /// Adds a reputation event to the log
    /// </summary>
    private void AddEvent(string factionId, int oldValue, int newValue, int delta, string reason)
    {
        var evt = new ReputationEvent
        {
            FactionId = factionId,
            Timestamp = DateTime.UtcNow,
            OldValue = oldValue,
            NewValue = newValue,
            Delta = delta,
            Reason = reason
        };

        EventLog.Add(evt);

        // Trim log if too large
        if (EventLog.Count > MaxEventLogSize)
        {
            EventLog.RemoveRange(0, EventLog.Count - MaxEventLogSize);
        }
    }

    /// <summary>
    /// Gets recent reputation events for a faction
    /// </summary>
    public List<ReputationEvent> GetEventsForFaction(string factionId, int count = 10)
    {
        return EventLog
            .Where(e => e.FactionId == factionId)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets all factions with reputation above a threshold
    /// </summary>
    public IEnumerable<string> GetFactionsAboveThreshold(int threshold)
    {
        return FactionReputations
            .Where(kvp => kvp.Value >= threshold)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets all factions with reputation below a threshold
    /// </summary>
    public IEnumerable<string> GetFactionsBelowThreshold(int threshold)
    {
        return FactionReputations
            .Where(kvp => kvp.Value <= threshold)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Applies reputation decay over time (call periodically)
    /// </summary>
    public void ApplyDecay(TimeSpan timeSinceLastDecay, int decayPerDay = 1)
    {
        var days = (int)timeSinceLastDecay.TotalDays;
        if (days <= 0) return;

        var decayAmount = decayPerDay * days;
        
        var factionsToUpdate = FactionReputations.Keys.ToList();
        foreach (var factionId in factionsToUpdate)
        {
            var current = FactionReputations[factionId];
            
            // Decay towards neutral (0)
            int newValue;
            if (current > 0)
            {
                newValue = Math.Max(0, current - decayAmount);
            }
            else if (current < 0)
            {
                newValue = Math.Min(0, current + decayAmount);
            }
            else
            {
                continue; // Already neutral
            }

            if (newValue != current)
            {
                FactionReputations[factionId] = newValue;
                AddEvent(factionId, current, newValue, newValue - current, "Reputation decay over time");
            }
        }
    }

    /// <summary>
    /// Initializes reputation for a new game with starting values
    /// </summary>
    public void InitializeNewGame()
    {
        FactionReputations.Clear();
        EventLog.Clear();

        foreach (var faction in FactionRegistry.All)
        {
            if (faction.StartingReputation != 0)
            {
                FactionReputations[faction.Id] = Math.Clamp(faction.StartingReputation, -100, 100);
            }
        }
    }
}

/// <summary>
/// Reputation tier levels
/// </summary>
public enum ReputationTier
{
    Nemesis,      // -100 to -71
    Hostile,      // -70 to -31
    Unfriendly,   // -30 to -1
    Neutral,      // 0 to 24
    Friendly,     // 25 to 49
    Respected,    // 50 to 74
    Honored,      // 75 to 89
    Revered,      // 90 to 99
    Exalted       // 100
}

/// <summary>
/// Reputation change event for history/UI
/// </summary>
public sealed class ReputationEvent : ISaveable
{
    public string FactionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int OldValue { get; set; }
    public int NewValue { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;

    public string SaveId => $"rep_event_{Guid.NewGuid()}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["factionId"] = FactionId,
            ["timestamp"] = Timestamp.ToString("o"),
            ["oldValue"] = OldValue,
            ["newValue"] = NewValue,
            ["delta"] = Delta,
            ["reason"] = Reason
        };
    }

    public void Deserialize(JObject data)
    {
        FactionId = data["factionId"]?.ToString() ?? string.Empty;
        Timestamp = DateTime.Parse(data["timestamp"]?.ToString() ?? DateTime.UtcNow.ToString("o"));
        OldValue = data["oldValue"]?.ToObject<int>() ?? 0;
        NewValue = data["newValue"]?.ToObject<int>() ?? 0;
        Delta = data["delta"]?.ToObject<int>() ?? 0;
        Reason = data["reason"]?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Reputation modifier definitions for various actions
/// </summary>
public static class ReputationModifiers
{
    // Trading
    public const int TradeProfitSmall = 1;      // Small profitable trade
    public const int TradeProfitMedium = 2;     // Medium profitable trade
    public const int TradeProfitLarge = 5;      // Large profitable trade
    public const int TradeLoss = -1;            // Lost money on trade
    public const int TradeFavoredCommodity = 2; // Traded faction's favored commodity
    public const int TradeBannedCommodity = -10; // Traded faction's banned commodity

    // Missions
    public const int MissionSuccess = 5;
    public const int MissionSuccessBonus = 3;   // Bonus objectives
    public const int MissionFailure = -10;
    public const int MissionAbandoned = -5;

    // Combat
    public const int DestroyedFactionShip = -15;
    public const int DestroyedEnemyOfFaction = 3;
    public const int AssistedFactionShip = 5;

    // Exploration
    public const int DiscoveredLocation = 2;
    public const int SurveyedSystem = 3;

    // Special
    public const int BribeOfficial = 10;        // One-time large boost
    public const int SmugglingCaught = -20;
    public const int RescueFactionAgent = 15;
    public const int BetrayFaction = -50;
}