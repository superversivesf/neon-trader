using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Faction definition with alignment, territory, and relations
/// </summary>
public sealed class Faction : ISaveable
{
    /// <summary>
    /// Unique faction identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description/lore
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Faction alignment/ideology
    /// </summary>
    public FactionAlignment Alignment { get; set; } = FactionAlignment.Neutral;

    /// <summary>
    /// Systems/regions this faction controls
    /// </summary>
    public HashSet<string> TerritorySystems { get; } = new();

    /// <summary>
    /// Stations/locations this faction owns
    /// </summary>
    public HashSet<string> OwnedLocations { get; } = new();

    /// <summary>
    /// Relations with other factions (-100 to 100)
    /// </summary>
    public Dictionary<string, int> FactionRelations { get; } = new();

    /// <summary>
    /// Default player reputation with this faction
    /// </summary>
    public int DefaultPlayerReputation { get; set; } = 0;

    /// <summary>
    /// Whether this faction is a major power (affects missions, events)
    /// </summary>
    public bool IsMajorFaction { get; set; } = false;

    /// <summary>
    /// Whether player can join this faction
    /// </summary>
    public bool IsJoinable { get; set; } = false;

    /// <summary>
    /// Faction color (hex) for UI
    /// </summary>
    public string ColorHex { get; set; } = "#FFFFFF";

    /// <summary>
    /// Faction icon/resource name
    /// </summary>
    public string IconResource { get; set; } = string.Empty;

    /// <summary>
    /// Preferred economy types
    /// </summary>
    public HashSet<EconomyType> PreferredEconomies { get; } = new();

    /// <summary>
    /// Commodities this faction produces/values
    /// </summary>
    public HashSet<string> FavoredCommodities { get; } = new();

    /// <summary>
    /// Commodities this faction bans/hates
    /// </summary>
    public HashSet<string> BannedCommodities { get; } = new();

    /// <summary>
    /// Starting relations with player for new games
    /// </summary>
    public int StartingReputation { get; set; } = 0;

    /// <summary>
    /// Reputation required to unlock faction missions
    /// </summary>
    public int MissionUnlockReputation { get; set; } = 25;

    /// <summary>
    /// Reputation required to unlock faction ships/equipment
    /// </summary>
    public int ShopUnlockReputation { get; set; } = 50;

    /// <summary>
    /// Whether faction offers black market access at high rep
    /// </summary>
    public bool OffersBlackMarket { get; set; } = false;

    // ISaveable implementation
    public string SaveId => $"faction_{Id}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the faction to JSON
    /// </summary>
    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["description"] = Description,
            ["alignment"] = Alignment.ToString(),
            ["territorySystems"] = JArray.FromObject(TerritorySystems),
            ["ownedLocations"] = JArray.FromObject(OwnedLocations),
            ["factionRelations"] = JObject.FromObject(FactionRelations),
            ["defaultPlayerReputation"] = DefaultPlayerReputation,
            ["isMajorFaction"] = IsMajorFaction,
            ["isJoinable"] = IsJoinable,
            ["colorHex"] = ColorHex,
            ["iconResource"] = IconResource,
            ["preferredEconomies"] = JArray.FromObject(PreferredEconomies),
            ["favoredCommodities"] = JArray.FromObject(FavoredCommodities),
            ["bannedCommodities"] = JArray.FromObject(BannedCommodities),
            ["startingReputation"] = StartingReputation,
            ["missionUnlockReputation"] = MissionUnlockReputation,
            ["shopUnlockReputation"] = ShopUnlockReputation,
            ["offersBlackMarket"] = OffersBlackMarket
        };
    }

    /// <summary>
    /// Deserialize the faction from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;
        Description = data["description"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<FactionAlignment>(data["alignment"]?.ToString(), out var align))
            Alignment = align;

        TerritorySystems.Clear();
        if (data["territorySystems"] is JArray territoryArray)
        {
            foreach (var sys in territoryArray)
            {
                var sysStr = sys?.ToString();
                if (!string.IsNullOrEmpty(sysStr))
                    TerritorySystems.Add(sysStr);
            }
        }

        OwnedLocations.Clear();
        if (data["ownedLocations"] is JArray ownedArray)
        {
            foreach (var loc in ownedArray)
            {
                var locStr = loc?.ToString();
                if (!string.IsNullOrEmpty(locStr))
                    OwnedLocations.Add(locStr);
            }
        }

        FactionRelations.Clear();
        if (data["factionRelations"] is JObject relationsObj)
        {
            foreach (var kvp in relationsObj)
            {
                var value = kvp.Value?.ToObject<int>();
                if (value.HasValue)
                    FactionRelations[kvp.Key] = value.Value;
            }
        }

        DefaultPlayerReputation = data["defaultPlayerReputation"]?.ToObject<int>() ?? 0;
        IsMajorFaction = data["isMajorFaction"]?.ToObject<bool>() ?? false;
        IsJoinable = data["isJoinable"]?.ToObject<bool>() ?? false;
        ColorHex = data["colorHex"]?.ToString() ?? "#FFFFFF";
        IconResource = data["iconResource"]?.ToString() ?? string.Empty;

        PreferredEconomies.Clear();
        if (data["preferredEconomies"] is JArray econArray)
        {
            foreach (var econ in econArray)
            {
                if (Enum.TryParse<EconomyType>(econ?.ToString(), out var econType))
                    PreferredEconomies.Add(econType);
            }
        }

        FavoredCommodities.Clear();
        if (data["favoredCommodities"] is JArray favArray)
        {
            foreach (var fav in favArray)
            {
                var favStr = fav?.ToString();
                if (!string.IsNullOrEmpty(favStr))
                    FavoredCommodities.Add(favStr);
            }
        }

        BannedCommodities.Clear();
        if (data["bannedCommodities"] is JArray banArray)
        {
            foreach (var ban in banArray)
            {
                var banStr = ban?.ToString();
                if (!string.IsNullOrEmpty(banStr))
                    BannedCommodities.Add(banStr);
            }
        }

        StartingReputation = data["startingReputation"]?.ToObject<int>() ?? 0;
        MissionUnlockReputation = data["missionUnlockReputation"]?.ToObject<int>() ?? 25;
        ShopUnlockReputation = data["shopUnlockReputation"]?.ToObject<int>() ?? 50;
        OffersBlackMarket = data["offersBlackMarket"]?.ToObject<bool>() ?? false;
    }

    /// <summary>
    /// Gets relation with another faction
    /// </summary>
    public int GetRelation(string otherFactionId)
    {
        return FactionRelations.TryGetValue(otherFactionId, out var relation) ? relation : 0;
    }

    /// <summary>
    /// Sets relation with another faction
    /// </summary>
    public void SetRelation(string otherFactionId, int value)
    {
        var clamped = Math.Clamp(value, -100, 100);
        FactionRelations[otherFactionId] = clamped;
    }

    /// <summary>
    /// Gets relation level as enum
    /// </summary>
    public FactionRelationLevel GetRelationLevel(string otherFactionId)
    {
        var relation = GetRelation(otherFactionId);
        if (relation >= 80) return FactionRelationLevel.Allied;
        if (relation >= 50) return FactionRelationLevel.Friendly;
        if (relation >= -30) return FactionRelationLevel.Neutral;
        if (relation >= -70) return FactionRelationLevel.Hostile;
        return FactionRelationLevel.AtWar;
    }

    /// <summary>
    /// Checks if faction owns a location
    /// </summary>
    public bool OwnsLocation(string locationId)
    {
        return OwnedLocations.Contains(locationId);
    }

    /// <summary>
    /// Checks if faction controls a system
    /// </summary>
    public bool ControlsSystem(string systemName)
    {
        return TerritorySystems.Contains(systemName);
    }

    /// <summary>
    /// Checks if commodity is favored
    /// </summary>
    public bool IsFavoredCommodity(string commodityId)
    {
        return FavoredCommodities.Contains(commodityId);
    }

    /// <summary>
    /// Checks if commodity is banned
    /// </summary>
    public bool IsBannedCommodity(string commodityId)
    {
        return BannedCommodities.Contains(commodityId);
    }
}

/// <summary>
/// Faction alignment/ideology
/// </summary>
public enum FactionAlignment
{
    /// <summary>Lawful, structured, corporate</summary>
    Lawful,
    
    /// <summary>Freedom-focused, anti-authority</summary>
    Libertarian,
    
    /// <summary>Profit-driven, pragmatic</summary>
    Mercantile,
    
    /// <summary>Military/hierarchical</summary>
    Militaristic,
    
    /// <summary>Scientific/knowledge-seeking</summary>
    Scientific,
    
    /// <summary>Religious/ideological</summary>
    Ideological,
    
    /// <summary>Criminal/outside the law</summary>
    Criminal,
    
    /// <summary>Neutral/pragmatic</summary>
    Neutral,
    
    /// <summary>Isolationist/xenophobic</summary>
    Isolationist,
    
    /// <summary>Expansionist/imperialist</summary>
    Expansionist
}

/// <summary>
/// Faction relation levels
/// </summary>
public enum FactionRelationLevel
{
    Allied,
    Friendly,
    Neutral,
    Hostile,
    AtWar
}

/// <summary>
/// Static registry of all factions
/// </summary>
public static class FactionRegistry
{
    private static readonly Dictionary<string, Faction> _factions = new();

    /// <summary>
    /// Gets a faction by ID
    /// </summary>
    public static Faction? Get(string id)
    {
        _factions.TryGetValue(id, out var faction);
        return faction;
    }

    /// <summary>
    /// Gets all factions
    /// </summary>
    public static IReadOnlyCollection<Faction> All => _factions.Values;

    /// <summary>
    /// Gets major factions
    /// </summary>
    public static IEnumerable<Faction> GetMajorFactions()
    {
        return _factions.Values.Where(f => f.IsMajorFaction);
    }

    /// <summary>
    /// Gets joinable factions
    /// </summary>
    public static IEnumerable<Faction> GetJoinableFactions()
    {
        return _factions.Values.Where(f => f.IsJoinable);
    }

    /// <summary>
    /// Gets factions by alignment
    /// </summary>
    public static IEnumerable<Faction> GetByAlignment(FactionAlignment alignment)
    {
        return _factions.Values.Where(f => f.Alignment == alignment);
    }

    /// <summary>
    /// Gets factions controlling a system
    /// </summary>
    public static IEnumerable<Faction> GetControllingSystem(string systemName)
    {
        return _factions.Values.Where(f => f.ControlsSystem(systemName));
    }

    /// <summary>
    /// Registers a faction
    /// </summary>
    public static void Register(Faction faction)
    {
        _factions[faction.Id] = faction;
    }

    /// <summary>
    /// Clears the registry
    /// </summary>
    public static void Clear()
    {
        _factions.Clear();
    }

    /// <summary>
    /// Loads factions from JSON data
    /// </summary>
    public static void LoadFromJson(string json)
    {
        Clear();
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var faction = new Faction();
            faction.Deserialize((JObject)item);
            Register(faction);
        }
    }
}