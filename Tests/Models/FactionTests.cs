using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

[Collection("Sequential")]
public class FactionTests
{
    public FactionTests()
    {
        FactionRegistry.Clear();
    }

    [Fact]
    public void Faction_Serialize_Deserialize_RoundTrip()
    {
        var faction = new Faction
        {
            Id = "galactic_federation", Name = "Galactic Federation",
            Description = "The ruling government of the core systems",
            Alignment = FactionAlignment.Lawful, DefaultPlayerReputation = 10,
            IsMajorFaction = true, IsJoinable = true, ColorHex = "#4488FF",
            IconResource = "icon_gf", StartingReputation = 10,
            MissionUnlockReputation = 25, ShopUnlockReputation = 50, OffersBlackMarket = false
        };
        faction.TerritorySystems.Add("sol");
        faction.TerritorySystems.Add("alpha_centauri");
        faction.OwnedLocations.Add("earth_station");
        faction.OwnedLocations.Add("mars_colony");
        faction.FactionRelations["crimson_fleet"] = -80;
        faction.FactionRelations["shadow_syndicate"] = -50;
        faction.PreferredEconomies.Add(EconomyType.Industrial);
        faction.PreferredEconomies.Add(EconomyType.HighTech);
        faction.FavoredCommodities.Add("electronics");
        faction.FavoredCommodities.Add("medicine");
        faction.BannedCommodities.Add("narcotics");
        faction.BannedCommodities.Add("slaves");

        var json = faction.Serialize();
        var restored = new Faction();
        restored.Deserialize(json);

        Assert.Equal("galactic_federation", restored.Id);
        Assert.Equal("Galactic Federation", restored.Name);
        Assert.Equal(FactionAlignment.Lawful, restored.Alignment);
        Assert.Equal(10, restored.DefaultPlayerReputation);
        Assert.True(restored.IsMajorFaction);
        Assert.True(restored.IsJoinable);
        Assert.Equal("#4488FF", restored.ColorHex);
        Assert.Equal("icon_gf", restored.IconResource);
        Assert.Equal(10, restored.StartingReputation);
        Assert.Equal(25, restored.MissionUnlockReputation);
        Assert.Equal(50, restored.ShopUnlockReputation);
        Assert.False(restored.OffersBlackMarket);
        Assert.Equal(2, restored.TerritorySystems.Count);
        Assert.Contains("sol", restored.TerritorySystems);
        Assert.Contains("alpha_centauri", restored.TerritorySystems);
        Assert.Equal(2, restored.OwnedLocations.Count);
        Assert.Contains("earth_station", restored.OwnedLocations);
        Assert.Contains("mars_colony", restored.OwnedLocations);
        Assert.Equal(2, restored.FactionRelations.Count);
        Assert.Equal(-80, restored.FactionRelations["crimson_fleet"]);
        Assert.Equal(-50, restored.FactionRelations["shadow_syndicate"]);
        Assert.Equal(2, restored.PreferredEconomies.Count);
        Assert.Contains(EconomyType.Industrial, restored.PreferredEconomies);
        Assert.Contains(EconomyType.HighTech, restored.PreferredEconomies);
        Assert.Equal(2, restored.FavoredCommodities.Count);
        Assert.Contains("electronics", restored.FavoredCommodities);
        Assert.Contains("medicine", restored.FavoredCommodities);
        Assert.Equal(2, restored.BannedCommodities.Count);
        Assert.Contains("narcotics", restored.BannedCommodities);
        Assert.Contains("slaves", restored.BannedCommodities);
    }

    [Fact]
    public void Faction_Serialize_Deserialize_EmptyFaction()
    {
        var faction = new Faction();
        var json = faction.Serialize();
        var restored = new Faction();
        restored.Deserialize(json);
        Assert.Equal(string.Empty, restored.Id);
        Assert.Equal(string.Empty, restored.Name);
        Assert.Equal(FactionAlignment.Neutral, restored.Alignment);
        Assert.Equal(0, restored.DefaultPlayerReputation);
        Assert.False(restored.IsMajorFaction);
        Assert.False(restored.IsJoinable);
        Assert.Equal("#FFFFFF", restored.ColorHex);
        Assert.Equal(25, restored.MissionUnlockReputation);
        Assert.Equal(50, restored.ShopUnlockReputation);
        Assert.False(restored.OffersBlackMarket);
        Assert.Empty(restored.TerritorySystems);
        Assert.Empty(restored.OwnedLocations);
        Assert.Empty(restored.FactionRelations);
    }

    [Fact]
    public void GetRelation_ExistingRelation_ReturnsValue()
    {
        var faction = new Faction();
        faction.SetRelation("other_faction", 50);
        Assert.Equal(50, faction.GetRelation("other_faction"));
    }

    [Fact]
    public void GetRelation_NonexistentRelation_ReturnsZero()
    {
        Assert.Equal(0, new Faction().GetRelation("unknown_faction"));
    }

    [Fact]
    public void SetRelation_ClampsToRange()
    {
        var faction = new Faction();
        faction.SetRelation("other_faction", 150);
        Assert.Equal(100, faction.GetRelation("other_faction"));
        faction.SetRelation("other_faction", -200);
        Assert.Equal(-100, faction.GetRelation("other_faction"));
    }

    [Theory]
    [InlineData(90, FactionRelationLevel.Allied)]
    [InlineData(80, FactionRelationLevel.Allied)]
    [InlineData(60, FactionRelationLevel.Friendly)]
    [InlineData(50, FactionRelationLevel.Friendly)]
    [InlineData(10, FactionRelationLevel.Neutral)]
    [InlineData(0, FactionRelationLevel.Neutral)]
    [InlineData(-30, FactionRelationLevel.Neutral)]
    [InlineData(-50, FactionRelationLevel.Hostile)]
    [InlineData(-70, FactionRelationLevel.Hostile)]
    [InlineData(-80, FactionRelationLevel.AtWar)]
    [InlineData(-100, FactionRelationLevel.AtWar)]
    public void GetRelationLevel_ReturnsCorrectLevel(int relation, FactionRelationLevel expected)
    {
        var faction = new Faction();
        faction.SetRelation("other_faction", relation);
        Assert.Equal(expected, faction.GetRelationLevel("other_faction"));
    }

    [Fact]
    public void OwnsLocation_OwnedLocation_ReturnsTrue()
    {
        var faction = new Faction();
        faction.OwnedLocations.Add("earth_station");
        Assert.True(faction.OwnsLocation("earth_station"));
    }

    [Fact]
    public void OwnsLocation_NotOwned_ReturnsFalse()
    {
        Assert.False(new Faction().OwnsLocation("mars_colony"));
    }

    [Fact]
    public void ControlsSystem_ControlledSystem_ReturnsTrue()
    {
        var faction = new Faction();
        faction.TerritorySystems.Add("sol");
        Assert.True(faction.ControlsSystem("sol"));
    }

    [Fact]
    public void ControlsSystem_NotControlled_ReturnsFalse()
    {
        Assert.False(new Faction().ControlsSystem("vega"));
    }

    [Fact]
    public void IsFavoredCommodity_Favored_ReturnsTrue()
    {
        var faction = new Faction();
        faction.FavoredCommodities.Add("electronics");
        Assert.True(faction.IsFavoredCommodity("electronics"));
    }

    [Fact]
    public void IsFavoredCommodity_NotFavored_ReturnsFalse()
    {
        Assert.False(new Faction().IsFavoredCommodity("ore"));
    }

    [Fact]
    public void IsBannedCommodity_Banned_ReturnsTrue()
    {
        var faction = new Faction();
        faction.BannedCommodities.Add("narcotics");
        Assert.True(faction.IsBannedCommodity("narcotics"));
    }

    [Fact]
    public void IsBannedCommodity_NotBanned_ReturnsFalse()
    {
        Assert.False(new Faction().IsBannedCommodity("water"));
    }

    [Fact]
    public void FactionRegistry_Register_AddsFaction()
    {
        var faction = new Faction { Id = "test_faction", Name = "Test Faction", Alignment = FactionAlignment.Mercantile };
        FactionRegistry.Register(faction);
        var retrieved = FactionRegistry.Get("test_faction");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Faction", retrieved!.Name);
    }

    [Fact]
    public void FactionRegistry_Get_Nonexistent_ReturnsNull()
    {
        Assert.Null(FactionRegistry.Get("nonexistent"));
    }

    [Fact]
    public void FactionRegistry_All_ReturnsAllRegistered()
    {
        FactionRegistry.Clear();
        FactionRegistry.Register(new Faction { Id = "faction_a", Name = "A" });
        FactionRegistry.Register(new Faction { Id = "faction_b", Name = "B" });
        Assert.Equal(2, FactionRegistry.All.Count);
    }

    [Fact]
    public void FactionRegistry_GetMajorFactions_ReturnsOnlyMajor()
    {
        FactionRegistry.Clear();
        FactionRegistry.Register(new Faction { Id = "major_a", Name = "Major A", IsMajorFaction = true });
        FactionRegistry.Register(new Faction { Id = "minor_a", Name = "Minor A", IsMajorFaction = false });
        FactionRegistry.Register(new Faction { Id = "major_b", Name = "Major B", IsMajorFaction = true });
        var majors = FactionRegistry.GetMajorFactions().ToList();
        Assert.Equal(2, majors.Count);
        Assert.All(majors, f => Assert.True(f.IsMajorFaction));
    }

    [Fact]
    public void FactionRegistry_GetJoinableFactions_ReturnsOnlyJoinable()
    {
        FactionRegistry.Register(new Faction { Id = "join_a", Name = "Join A", IsJoinable = true });
        FactionRegistry.Register(new Faction { Id = "nojoin_a", Name = "No Join A", IsJoinable = false });
        var joinable = FactionRegistry.GetJoinableFactions().ToList();
        Assert.Single(joinable);
        Assert.Equal("join_a", joinable[0].Id);
    }

    [Fact]
    public void FactionRegistry_GetByAlignment_ReturnsCorrectAlignment()
    {
        FactionRegistry.Register(new Faction { Id = "lawful_a", Name = "Lawful A", Alignment = FactionAlignment.Lawful });
        FactionRegistry.Register(new Faction { Id = "criminal_a", Name = "Criminal A", Alignment = FactionAlignment.Criminal });
        FactionRegistry.Register(new Faction { Id = "lawful_b", Name = "Lawful B", Alignment = FactionAlignment.Lawful });
        var lawful = FactionRegistry.GetByAlignment(FactionAlignment.Lawful).ToList();
        Assert.Equal(2, lawful.Count);
        Assert.All(lawful, f => Assert.Equal(FactionAlignment.Lawful, f.Alignment));
    }

    [Fact]
    public void FactionRegistry_GetControllingSystem_ReturnsCorrectFactions()
    {
        var faction = new Faction { Id = "sol_controller", Name = "Sol Controller" };
        faction.TerritorySystems.Add("sol");
        FactionRegistry.Register(faction);
        FactionRegistry.Register(new Faction { Id = "vega_controller", Name = "Vega Controller" });
        var controllers = FactionRegistry.GetControllingSystem("sol").ToList();
        Assert.Single(controllers);
        Assert.Equal("sol_controller", controllers[0].Id);
    }

    [Fact]
    public void FactionRegistry_Clear_RemovesAllFactions()
    {
        FactionRegistry.Register(new Faction { Id = "test", Name = "Test" });
        FactionRegistry.Clear();
        Assert.Empty(FactionRegistry.All);
    }

    [Fact]
    public void FactionRegistry_LoadFromJson_PopulatesRegistry()
    {
        var json = @"[{""id"":""json_faction"",""name"":""JSON Faction"",""description"":""Loaded from JSON"",""alignment"":""Militaristic"",""territorySystems"":[""war_zone""],""ownedLocations"":[""fortress_alpha""],""factionRelations"":{""enemy"":-100},""defaultPlayerReputation"":-20,""isMajorFaction"":true,""isJoinable"":false,""colorHex"":""#FF0000"",""iconResource"":""icon_war"",""preferredEconomies"":[""Military""],""favoredCommodities"":[""weapons""],""bannedCommodities"":[""luxury_goods""],""startingReputation"":-20,""missionUnlockReputation"":10,""shopUnlockReputation"":30,""offersBlackMarket"":true}]";
        FactionRegistry.LoadFromJson(json);
        var faction = FactionRegistry.Get("json_faction");
        Assert.NotNull(faction);
        Assert.Equal("JSON Faction", faction!.Name);
        Assert.Equal(FactionAlignment.Militaristic, faction.Alignment);
        Assert.Contains("war_zone", faction.TerritorySystems);
        Assert.Contains("fortress_alpha", faction.OwnedLocations);
        Assert.Equal(-100, faction.GetRelation("enemy"));
        Assert.Equal(-20, faction.DefaultPlayerReputation);
        Assert.True(faction.IsMajorFaction);
        Assert.False(faction.IsJoinable);
        Assert.Equal("#FF0000", faction.ColorHex);
        Assert.Contains(EconomyType.Military, faction.PreferredEconomies);
        Assert.Contains("weapons", faction.FavoredCommodities);
        Assert.Contains("luxury_goods", faction.BannedCommodities);
        Assert.Equal(-20, faction.StartingReputation);
        Assert.Equal(10, faction.MissionUnlockReputation);
        Assert.Equal(30, faction.ShopUnlockReputation);
        Assert.True(faction.OffersBlackMarket);
    }

    [Fact]
    public void SaveId_ReturnsCorrectFormat()
    {
        Assert.Equal("faction_galactic_federation", new Faction { Id = "galactic_federation" }.SaveId);
    }

    [Fact]
    public void SaveVersion_IsOne()
    {
        Assert.Equal(1, new Faction().SaveVersion);
    }
}
