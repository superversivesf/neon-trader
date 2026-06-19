using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

/// <summary>
/// Tests for Economy model: EconomyType enum, EconomyTradeProfile serialization,
/// EconomyRegistry profiles, modifiers, and default profile initialization.
/// </summary>
[Collection("Sequential")]
public class EconomyTests
{
    #region EconomyType Enum

    [Fact]
    public void EconomyType_HasAllExpectedValues()
    {
        var types = Enum.GetValues<EconomyType>();
        Assert.Contains(EconomyType.Industrial, types);
        Assert.Contains(EconomyType.Agricultural, types);
        Assert.Contains(EconomyType.HighTech, types);
        Assert.Contains(EconomyType.Luxury, types);
        Assert.Contains(EconomyType.Military, types);
        Assert.Contains(EconomyType.Medical, types);
        Assert.Contains(EconomyType.Criminal, types);
        Assert.Contains(EconomyType.Balanced, types);
        Assert.Contains(EconomyType.Service, types);
        Assert.Contains(EconomyType.Mining, types);
        Assert.Contains(EconomyType.Refining, types);
        Assert.Contains(EconomyType.Research, types);
        Assert.Equal(12, types.Length);
    }

    #endregion

    #region EconomyTradeProfile Construction & Defaults

    [Fact]
    public void DefaultConstructor_HasEmptyDictionaries()
    {
        var profile = new EconomyTradeProfile();

        Assert.Empty(profile.ProductionModifiers);
        Assert.Empty(profile.ConsumptionModifiers);
        Assert.Empty(profile.BaseSupply);
        Assert.Empty(profile.BaseDemand);
        Assert.Empty(profile.BuyPriceMultiplier);
        Assert.Empty(profile.SellPriceMultiplier);
        Assert.Empty(profile.SpecialProduction);
        Assert.Empty(profile.SpecialConsumption);
    }

    [Fact]
    public void DefaultConstructor_DefaultModifiers()
    {
        var profile = new EconomyTradeProfile();

        Assert.Equal(0.0, profile.IllegalTolerance);
        Assert.Equal(1.0, profile.RefreshRateModifier);
        Assert.Equal(1.0, profile.MarketSizeModifier);
    }

    #endregion

    #region EconomyTradeProfile Serialization / Deserialization

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var profile = new EconomyTradeProfile
        {
            EconomyType = EconomyType.Industrial,
            IllegalTolerance = 0.2,
            RefreshRateModifier = 1.5,
            MarketSizeModifier = 1.2
        };
        profile.ProductionModifiers[CommodityCategory.Ore] = 1.5;
        profile.ConsumptionModifiers[CommodityCategory.Tech] = 1.3;
        profile.BaseSupply[CommodityCategory.Ore] = 80;
        profile.BaseDemand[CommodityCategory.Tech] = 70;
        profile.BuyPriceMultiplier[CommodityCategory.Ore] = 0.9;
        profile.SellPriceMultiplier[CommodityCategory.Tech] = 1.2;
        profile.SpecialProduction["iron_ore"] = 1.5;
        profile.SpecialConsumption["electronics"] = 1.3;

        var json = profile.Serialize();

        Assert.Equal("Industrial", json["economyType"]?.ToString());
        Assert.Equal(0.2, json["illegalTolerance"]?.ToObject<double>());
        Assert.Equal(1.5, json["refreshRateModifier"]?.ToObject<double>());
        Assert.Equal(1.2, json["marketSizeModifier"]?.ToObject<double>());

        var prodMods = json["productionModifiers"] as JObject;
        Assert.NotNull(prodMods);
        Assert.Equal(1.5, prodMods!["Ore"]?.ToObject<double>());

        var consMods = json["consumptionModifiers"] as JObject;
        Assert.NotNull(consMods);
        Assert.Equal(1.3, consMods!["Tech"]?.ToObject<double>());

        var baseSupply = json["baseSupply"] as JObject;
        Assert.NotNull(baseSupply);
        Assert.Equal(80, baseSupply!["Ore"]?.ToObject<int>());

        var baseDemand = json["baseDemand"] as JObject;
        Assert.NotNull(baseDemand);
        Assert.Equal(70, baseDemand!["Tech"]?.ToObject<int>());

        var buyMult = json["buyPriceMultiplier"] as JObject;
        Assert.NotNull(buyMult);
        Assert.Equal(0.9, buyMult!["Ore"]?.ToObject<double>());

        var sellMult = json["sellPriceMultiplier"] as JObject;
        Assert.NotNull(sellMult);
        Assert.Equal(1.2, sellMult!["Tech"]?.ToObject<double>());

        var specialProd = json["specialProduction"] as JObject;
        Assert.NotNull(specialProd);
        Assert.Equal(1.5, specialProd!["iron_ore"]?.ToObject<double>());

        var specialCons = json["specialConsumption"] as JObject;
        Assert.NotNull(specialCons);
        Assert.Equal(1.3, specialCons!["electronics"]?.ToObject<double>());
    }

    [Fact]
    public void Deserialize_RestoresAllProperties()
    {
        var original = new EconomyTradeProfile
        {
            EconomyType = EconomyType.Military,
            IllegalTolerance = 0.1,
            RefreshRateModifier = 0.8,
            MarketSizeModifier = 1.1
        };
        original.ProductionModifiers[CommodityCategory.Weapons] = 1.5;
        original.ConsumptionModifiers[CommodityCategory.Ore] = 1.4;
        original.BaseSupply[CommodityCategory.Weapons] = 70;
        original.BaseDemand[CommodityCategory.Ore] = 80;
        original.BuyPriceMultiplier[CommodityCategory.Weapons] = 0.9;
        original.SellPriceMultiplier[CommodityCategory.Ore] = 1.25;
        original.SpecialProduction["plasma_rifle"] = 1.2;
        original.SpecialConsumption["titanium"] = 1.5;

        var json = original.Serialize();
        var restored = new EconomyTradeProfile();
        restored.Deserialize(json);

        Assert.Equal(original.EconomyType, restored.EconomyType);
        Assert.Equal(original.IllegalTolerance, restored.IllegalTolerance);
        Assert.Equal(original.RefreshRateModifier, restored.RefreshRateModifier);
        Assert.Equal(original.MarketSizeModifier, restored.MarketSizeModifier);
        Assert.Equal(1.5, restored.ProductionModifiers[CommodityCategory.Weapons]);
        Assert.Equal(1.4, restored.ConsumptionModifiers[CommodityCategory.Ore]);
        Assert.Equal(70, restored.BaseSupply[CommodityCategory.Weapons]);
        Assert.Equal(80, restored.BaseDemand[CommodityCategory.Ore]);
        Assert.Equal(0.9, restored.BuyPriceMultiplier[CommodityCategory.Weapons]);
        Assert.Equal(1.25, restored.SellPriceMultiplier[CommodityCategory.Ore]);
        Assert.Equal(1.2, restored.SpecialProduction["plasma_rifle"]);
        Assert.Equal(1.5, restored.SpecialConsumption["titanium"]);
    }

    [Fact]
    public void Deserialize_EmptyJson_UsesDefaults()
    {
        var profile = new EconomyTradeProfile();
        profile.Deserialize(new JObject());

        Assert.Equal(0.0, profile.IllegalTolerance);
        Assert.Equal(1.0, profile.RefreshRateModifier);
        Assert.Equal(1.0, profile.MarketSizeModifier);
        Assert.Empty(profile.ProductionModifiers);
        Assert.Empty(profile.ConsumptionModifiers);
        Assert.Empty(profile.BaseSupply);
        Assert.Empty(profile.BaseDemand);
        Assert.Empty(profile.BuyPriceMultiplier);
        Assert.Empty(profile.SellPriceMultiplier);
        Assert.Empty(profile.SpecialProduction);
        Assert.Empty(profile.SpecialConsumption);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = new EconomyTradeProfile
        {
            EconomyType = EconomyType.HighTech,
            IllegalTolerance = 0.05,
            RefreshRateModifier = 1.2,
            MarketSizeModifier = 1.1
        };
        original.ProductionModifiers[CommodityCategory.Tech] = 1.6;
        original.ProductionModifiers[CommodityCategory.Medical] = 1.2;
        original.ConsumptionModifiers[CommodityCategory.Ore] = 1.4;
        original.BaseSupply[CommodityCategory.Tech] = 80;
        original.BaseDemand[CommodityCategory.Ore] = 75;
        original.BuyPriceMultiplier[CommodityCategory.Tech] = 0.9;
        original.SellPriceMultiplier[CommodityCategory.Ore] = 1.3;

        var json = original.Serialize();
        var restored = new EconomyTradeProfile();
        restored.Deserialize(json);

        var reSerialized = restored.Serialize();
        Assert.Equal(json.ToString(), reSerialized.ToString());
    }

    #endregion

    #region ISaveable

    [Fact]
    public void SaveId_ContainsEconomyType()
    {
        var profile = new EconomyTradeProfile { EconomyType = EconomyType.Industrial };
        Assert.Equal("economy_profile_Industrial", profile.SaveId);
    }

    [Fact]
    public void SaveVersion_IsOne()
    {
        var profile = new EconomyTradeProfile();
        Assert.Equal(1, profile.SaveVersion);
    }

    #endregion

    #region EconomyRegistry - Default Profiles

    [Fact]
    public void Registry_GetProfile_ReturnsProfileForAllTypes()
    {
        foreach (EconomyType type in Enum.GetValues<EconomyType>())
        {
            var profile = EconomyRegistry.GetProfile(type);
            Assert.NotNull(profile);
            Assert.Equal(type, profile.EconomyType);
        }
    }

    [Fact]
    public void Registry_GetProfile_UnknownType_ReturnsBalanced()
    {
        // Cast an invalid value
        var invalidType = (EconomyType)999;
        var profile = EconomyRegistry.GetProfile(invalidType);
        Assert.NotNull(profile);
        Assert.Equal(EconomyType.Balanced, profile.EconomyType);
    }

    [Fact]
    public void Registry_AllProfiles_ContainsAllTypes()
    {
        var all = EconomyRegistry.AllProfiles;
        Assert.Equal(12, all.Count);
        foreach (EconomyType type in Enum.GetValues<EconomyType>())
        {
            Assert.True(all.ContainsKey(type));
        }
    }

    #endregion

    #region EconomyRegistry - Industrial Profile

    [Fact]
    public void IndustrialProfile_ProducesOre()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Industrial);

        Assert.True(profile.ProductionModifiers[CommodityCategory.Ore] > 1.0);
        Assert.Equal(1.5, profile.ProductionModifiers[CommodityCategory.Ore]);
    }

    [Fact]
    public void IndustrialProfile_ConsumesTech()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Industrial);

        Assert.True(profile.ConsumptionModifiers[CommodityCategory.Tech] > 1.0);
        Assert.Equal(1.3, profile.ConsumptionModifiers[CommodityCategory.Tech]);
    }

    [Fact]
    public void IndustrialProfile_BuysOreCheaper()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Industrial);

        Assert.True(profile.BuyPriceMultiplier[CommodityCategory.Ore] < 1.0);
        Assert.Equal(0.9, profile.BuyPriceMultiplier[CommodityCategory.Ore]);
    }

    [Fact]
    public void IndustrialProfile_SellsTechHigher()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Industrial);

        Assert.True(profile.SellPriceMultiplier[CommodityCategory.Tech] > 1.0);
        Assert.Equal(1.2, profile.SellPriceMultiplier[CommodityCategory.Tech]);
    }

    [Fact]
    public void IndustrialProfile_HasLargerMarket()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Industrial);
        Assert.Equal(1.2, profile.MarketSizeModifier);
    }

    #endregion

    #region EconomyRegistry - Criminal Profile

    [Fact]
    public void CriminalProfile_ProducesIllegal()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Criminal);

        Assert.True(profile.ProductionModifiers[CommodityCategory.Illegal] > 1.0);
        Assert.Equal(1.5, profile.ProductionModifiers[CommodityCategory.Illegal]);
    }

    [Fact]
    public void CriminalProfile_FullIllegalTolerance()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Criminal);
        Assert.Equal(1.0, profile.IllegalTolerance);
    }

    [Fact]
    public void CriminalProfile_BuysIllegalCheaper()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Criminal);
        Assert.Equal(0.8, profile.BuyPriceMultiplier[CommodityCategory.Illegal]);
    }

    [Fact]
    public void CriminalProfile_SmallerMarket()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Criminal);
        Assert.Equal(0.8, profile.MarketSizeModifier);
    }

    #endregion

    #region EconomyRegistry - Mining Profile

    [Fact]
    public void MiningProfile_HeavyOreProduction()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Mining);

        Assert.Equal(2.0, profile.ProductionModifiers[CommodityCategory.Ore]);
        Assert.Equal(95, profile.BaseSupply[CommodityCategory.Ore]);
    }

    [Fact]
    public void MiningProfile_VeryCheapOre()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Mining);
        Assert.Equal(0.7, profile.BuyPriceMultiplier[CommodityCategory.Ore]);
    }

    [Fact]
    public void MiningProfile_LargestMarket()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Mining);
        Assert.Equal(1.3, profile.MarketSizeModifier);
    }

    #endregion

    #region EconomyRegistry - Balanced Profile

    [Fact]
    public void BalancedProfile_AllModifiersAreOne()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Balanced);

        foreach (CommodityCategory cat in Enum.GetValues<CommodityCategory>())
        {
            Assert.Equal(1.0, profile.ProductionModifiers[cat]);
            Assert.Equal(1.0, profile.ConsumptionModifiers[cat]);
            Assert.Equal(50, profile.BaseSupply[cat]);
            Assert.Equal(50, profile.BaseDemand[cat]);
            Assert.Equal(1.0, profile.BuyPriceMultiplier[cat]);
            Assert.Equal(1.0, profile.SellPriceMultiplier[cat]);
        }
    }

    [Fact]
    public void BalancedProfile_StandardMarketSize()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Balanced);
        Assert.Equal(1.0, profile.MarketSizeModifier);
    }

    [Fact]
    public void BalancedProfile_NoIllegalTolerance()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Balanced);
        Assert.Equal(0.0, profile.IllegalTolerance);
    }

    #endregion

    #region EconomyRegistry - Service Profile

    [Fact]
    public void ServiceProfile_ProducesLittle()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Service);

        foreach (CommodityCategory cat in Enum.GetValues<CommodityCategory>())
        {
            Assert.True(profile.ProductionModifiers[cat] < 1.0);
            Assert.Equal(0.5, profile.ProductionModifiers[cat]);
        }
    }

    [Fact]
    public void ServiceProfile_ConsumesEverything()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Service);

        foreach (CommodityCategory cat in Enum.GetValues<CommodityCategory>())
        {
            Assert.True(profile.ConsumptionModifiers[cat] > 1.0);
            Assert.Equal(1.3, profile.ConsumptionModifiers[cat]);
        }
    }

    [Fact]
    public void ServiceProfile_BuysHigherSellsLower()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Service);

        foreach (CommodityCategory cat in Enum.GetValues<CommodityCategory>())
        {
            Assert.True(profile.BuyPriceMultiplier[cat] > 1.0); // Buy higher
            Assert.True(profile.SellPriceMultiplier[cat] < 1.0); // Sell lower
        }
    }

    #endregion

    #region EconomyRegistry - Research Profile

    [Fact]
    public void ResearchProfile_HeavyTechProduction()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Research);

        Assert.Equal(1.8, profile.ProductionModifiers[CommodityCategory.Tech]);
        Assert.Equal(1.3, profile.ProductionModifiers[CommodityCategory.Medical]);
        Assert.Equal(85, profile.BaseSupply[CommodityCategory.Tech]);
    }

    [Fact]
    public void ResearchProfile_CheapTech()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Research);
        Assert.Equal(0.85, profile.BuyPriceMultiplier[CommodityCategory.Tech]);
        Assert.Equal(0.9, profile.BuyPriceMultiplier[CommodityCategory.Medical]);
    }

    [Fact]
    public void ResearchProfile_SmallMarket()
    {
        var profile = EconomyRegistry.GetProfile(EconomyType.Research);
        Assert.Equal(0.8, profile.MarketSizeModifier);
    }

    #endregion

    #region EconomyRegistry - Register Custom Profile

    [Fact]
    public void RegisterProfile_OverridesExisting()
    {
        // Save original profile to restore after test
        var originalProfile = EconomyRegistry.GetProfile(EconomyType.Industrial);
        var originalMarketSize = originalProfile.MarketSizeModifier;
        var originalIllegalTolerance = originalProfile.IllegalTolerance;
        var originalOreProduction = originalProfile.ProductionModifiers.GetValueOrDefault(CommodityCategory.Ore, 1.0);

        try
        {
            var customProfile = new EconomyTradeProfile
            {
                EconomyType = EconomyType.Industrial,
                MarketSizeModifier = 5.0,
                IllegalTolerance = 0.9
            };
            customProfile.ProductionModifiers[CommodityCategory.Ore] = 3.0;

            EconomyRegistry.RegisterProfile(customProfile);

            var retrieved = EconomyRegistry.GetProfile(EconomyType.Industrial);
            Assert.Equal(5.0, retrieved.MarketSizeModifier);
            Assert.Equal(0.9, retrieved.IllegalTolerance);
            Assert.Equal(3.0, retrieved.ProductionModifiers[CommodityCategory.Ore]);
        }
        finally
        {
            // Restore original profile so other tests aren't affected
            EconomyRegistry.RegisterProfile(originalProfile);
        }
    }

    [Fact]
    public void RegisterProfile_AddsNewType()
    {
        // Save original profile to restore after test
        var originalProfile = EconomyRegistry.GetProfile(EconomyType.Agricultural);
        var originalMarketSize = originalProfile.MarketSizeModifier;

        try
        {
            var customProfile = new EconomyTradeProfile
            {
                EconomyType = EconomyType.Agricultural,
                MarketSizeModifier = 2.0
            };

            EconomyRegistry.RegisterProfile(customProfile);

            var retrieved = EconomyRegistry.GetProfile(EconomyType.Agricultural);
            Assert.Equal(2.0, retrieved.MarketSizeModifier);
        }
        finally
        {
            // Restore original profile
            EconomyRegistry.RegisterProfile(originalProfile);
        }
    }

    #endregion

    #region EconomyRegistry - Profile Consistency

    [Fact]
    public void AllProfiles_HaveValidModifierRanges()
    {
        foreach (var (type, profile) in EconomyRegistry.AllProfiles)
        {
            Assert.True(profile.IllegalTolerance >= 0.0 && profile.IllegalTolerance <= 1.0,
                $"{type}: IllegalTolerance {profile.IllegalTolerance} out of [0,1]");
            Assert.True(profile.RefreshRateModifier > 0,
                $"{type}: RefreshRateModifier {profile.RefreshRateModifier} must be positive");
            Assert.True(profile.MarketSizeModifier > 0,
                $"{type}: MarketSizeModifier {profile.MarketSizeModifier} must be positive");
        }
    }

    [Fact]
    public void AllProfiles_HaveBaseSupplyAndDemand()
    {
        foreach (var (type, profile) in EconomyRegistry.AllProfiles)
        {
            Assert.NotEmpty(profile.BaseSupply);
            Assert.NotEmpty(profile.BaseDemand);

            foreach (var (cat, supply) in profile.BaseSupply)
            {
                Assert.True(supply >= 0 && supply <= 100,
                    $"{type}: BaseSupply[{cat}] = {supply} out of [0,100]");
            }

            foreach (var (cat, demand) in profile.BaseDemand)
            {
                Assert.True(demand >= 0 && demand <= 100,
                    $"{type}: BaseDemand[{cat}] = {demand} out of [0,100]");
            }
        }
    }

    #endregion
}
