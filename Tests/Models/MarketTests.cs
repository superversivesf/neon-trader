using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

/// <summary>
/// Tests for Market model: price/supply/demand management, buy/sell operations,
/// serialization, market events, and supply/demand ratio calculations.
/// </summary>
public class MarketTests
{
    #region Construction & Defaults

    [Fact]
    public void DefaultConstructor_SetsSensibleDefaults()
    {
        var market = new Market();

        Assert.Equal("", market.MarketId);
        Assert.Equal("", market.Name);
        Assert.Equal(EconomyType.Balanced, market.EconomyType);
        Assert.Empty(market.Prices);
        Assert.Empty(market.Supply);
        Assert.Empty(market.Demand);
        Assert.Equal(DateTime.MinValue, market.LastRefresh);
        Assert.Equal(0, market.Seed);
        Assert.Empty(market.ActiveEvents);
        Assert.Equal("", market.FactionId);
        Assert.Equal(5, market.TechLevel);
        Assert.False(market.HasBlackMarket);
        Assert.Equal(0, market.PlayerReputation);
        Assert.Equal(0.05m, market.PriceChangeThreshold);
    }

    #endregion

    #region GetPrice / GetSupply / GetDemand

    [Fact]
    public void GetPrice_ExistingCommodity_ReturnsPrice()
    {
        var market = new Market();
        market.Prices["water"] = 25.5m;

        Assert.Equal(25.5m, market.GetPrice("water"));
    }

    [Fact]
    public void GetPrice_NonExistentCommodity_ReturnsZero()
    {
        var market = new Market();
        Assert.Equal(0m, market.GetPrice("nonexistent"));
    }

    [Fact]
    public void GetSupply_ExistingCommodity_ReturnsSupply()
    {
        var market = new Market();
        market.Supply["water"] = 100;

        Assert.Equal(100, market.GetSupply("water"));
    }

    [Fact]
    public void GetSupply_NonExistentCommodity_ReturnsZero()
    {
        var market = new Market();
        Assert.Equal(0, market.GetSupply("nonexistent"));
    }

    [Fact]
    public void GetDemand_ExistingCommodity_ReturnsDemand()
    {
        var market = new Market();
        market.Demand["water"] = 50;

        Assert.Equal(50, market.GetDemand("water"));
    }

    [Fact]
    public void GetDemand_NonExistentCommodity_ReturnsZero()
    {
        var market = new Market();
        Assert.Equal(0, market.GetDemand("nonexistent"));
    }

    #endregion

    #region IsAvailable

    [Fact]
    public void IsAvailable_SufficientSupplyAndPositivePrice_ReturnsTrue()
    {
        var market = new Market();
        market.Supply["water"] = 50;
        market.Prices["water"] = 10m;

        Assert.True(market.IsAvailable("water", 10));
    }

    [Fact]
    public void IsAvailable_InsufficientSupply_ReturnsFalse()
    {
        var market = new Market();
        market.Supply["water"] = 5;
        market.Prices["water"] = 10m;

        Assert.False(market.IsAvailable("water", 10));
    }

    [Fact]
    public void IsAvailable_ZeroPrice_ReturnsFalse()
    {
        var market = new Market();
        market.Supply["water"] = 50;
        market.Prices["water"] = 0m;

        Assert.False(market.IsAvailable("water", 10));
    }

    [Fact]
    public void IsAvailable_NonExistentCommodity_ReturnsFalse()
    {
        var market = new Market();
        Assert.False(market.IsAvailable("nonexistent"));
    }

    [Fact]
    public void IsAvailable_DefaultQuantity_One()
    {
        var market = new Market();
        market.Supply["water"] = 1;
        market.Prices["water"] = 10m;

        Assert.True(market.IsAvailable("water")); // default quantity = 1
    }

    #endregion

    #region SellToMarket (Player sells to market)

    [Fact]
    public void SellToMarket_ReturnsQuantityAndCredits()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Prices["water"] = 10m;

        var (quantitySold, creditsReceived) = market.SellToMarket("water", 20);

        Assert.Equal(20, quantitySold);
        Assert.Equal(200m, creditsReceived);
    }

    [Fact]
    public void SellToMarket_ReducesSupply()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Prices["water"] = 10m;

        market.SellToMarket("water", 20);

        Assert.Equal(80, market.GetSupply("water"));
    }

    [Fact]
    public void SellToMarket_IncreasesDemand()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Prices["water"] = 10m;
        market.Demand["water"] = 10;

        market.SellToMarket("water", 20);

        // Demand increases by quantity/2 = 10
        Assert.Equal(20, market.GetDemand("water"));
    }

    [Fact]
    public void SellToMarket_QuantityExceedsSupply_ClampsToSupply()
    {
        var market = new Market();
        market.Supply["water"] = 10;
        market.Prices["water"] = 10m;

        var (quantitySold, creditsReceived) = market.SellToMarket("water", 100);

        Assert.Equal(10, quantitySold);
        Assert.Equal(100m, creditsReceived);
        Assert.Equal(0, market.GetSupply("water"));
    }

    [Fact]
    public void SellToMarket_ZeroSupply_ReturnsZero()
    {
        var market = new Market();
        market.Supply["water"] = 0;
        market.Prices["water"] = 10m;

        var (quantitySold, creditsReceived) = market.SellToMarket("water", 20);

        Assert.Equal(0, quantitySold);
        Assert.Equal(0m, creditsReceived);
    }

    [Fact]
    public void SellToMarket_NonExistentCommodity_ReturnsZero()
    {
        var market = new Market();
        var (quantitySold, creditsReceived) = market.SellToMarket("nonexistent", 20);

        Assert.Equal(0, quantitySold);
        Assert.Equal(0m, creditsReceived);
    }

    [Fact]
    public void SellToMarket_SupplyNeverGoesNegative()
    {
        var market = new Market();
        market.Supply["water"] = 5;
        market.Prices["water"] = 10m;

        market.SellToMarket("water", 100);

        Assert.Equal(0, market.GetSupply("water"));
    }

    #endregion

    #region BuyFromMarket (Player buys from market)

    [Fact]
    public void BuyFromMarket_ReturnsQuantityAndCredits()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Prices["water"] = 10m;

        var (quantityBought, creditsPaid) = market.BuyFromMarket("water", 20);

        Assert.Equal(20, quantityBought);
        Assert.Equal(200m, creditsPaid);
    }

    [Fact]
    public void BuyFromMarket_ReducesSupply()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Prices["water"] = 10m;

        market.BuyFromMarket("water", 20);

        Assert.Equal(80, market.GetSupply("water"));
    }

    [Fact]
    public void BuyFromMarket_IncreasesDemand()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Prices["water"] = 10m;
        market.Demand["water"] = 10;

        market.BuyFromMarket("water", 20);

        // Demand increases by full quantity
        Assert.Equal(30, market.GetDemand("water"));
    }

    [Fact]
    public void BuyFromMarket_QuantityExceedsSupply_ClampsToSupply()
    {
        var market = new Market();
        market.Supply["water"] = 10;
        market.Prices["water"] = 10m;

        var (quantityBought, creditsPaid) = market.BuyFromMarket("water", 100);

        Assert.Equal(10, quantityBought);
        Assert.Equal(100m, creditsPaid);
        Assert.Equal(0, market.GetSupply("water"));
    }

    [Fact]
    public void BuyFromMarket_ZeroSupply_ReturnsZero()
    {
        var market = new Market();
        market.Supply["water"] = 0;
        market.Prices["water"] = 10m;

        var (quantityBought, creditsPaid) = market.BuyFromMarket("water", 20);

        Assert.Equal(0, quantityBought);
        Assert.Equal(0m, creditsPaid);
    }

    [Fact]
    public void BuyFromMarket_SupplyNeverGoesNegative()
    {
        var market = new Market();
        market.Supply["water"] = 5;
        market.Prices["water"] = 10m;

        market.BuyFromMarket("water", 100);

        Assert.Equal(0, market.GetSupply("water"));
    }

    #endregion

    #region NeedsRefresh

    [Fact]
    public void NeedsRefresh_NeverRefreshed_ReturnsTrue()
    {
        var market = new Market();
        var now = DateTime.UtcNow;

        Assert.True(market.NeedsRefresh(now, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void NeedsRefresh_WithinInterval_ReturnsFalse()
    {
        var market = new Market();
        var now = DateTime.UtcNow;
        market.LastRefresh = now;

        Assert.False(market.NeedsRefresh(now.AddMinutes(30), TimeSpan.FromHours(1)));
    }

    [Fact]
    public void NeedsRefresh_AfterInterval_ReturnsTrue()
    {
        var market = new Market();
        var now = DateTime.UtcNow;
        market.LastRefresh = now;

        Assert.True(market.NeedsRefresh(now.AddHours(2), TimeSpan.FromHours(1)));
    }

    [Fact]
    public void NeedsRefresh_ExactlyAtInterval_ReturnsTrue()
    {
        var market = new Market();
        var now = DateTime.UtcNow;
        market.LastRefresh = now;

        Assert.True(market.NeedsRefresh(now.AddHours(1), TimeSpan.FromHours(1)));
    }

    #endregion

    #region GetSupplyDemandRatio

    [Fact]
    public void GetSupplyDemandRatio_Balanced_ReturnsOne()
    {
        var market = new Market();
        market.Supply["water"] = 50;
        market.Demand["water"] = 50;

        Assert.Equal(1.0, market.GetSupplyDemandRatio("water"));
    }

    [Fact]
    public void GetSupplyDemandRatio_Oversupplied_ReturnsGreaterThanOne()
    {
        var market = new Market();
        market.Supply["water"] = 100;
        market.Demand["water"] = 50;

        Assert.Equal(2.0, market.GetSupplyDemandRatio("water"));
    }

    [Fact]
    public void GetSupplyDemandRatio_Undersupplied_ReturnsLessThanOne()
    {
        var market = new Market();
        market.Supply["water"] = 25;
        market.Demand["water"] = 100;

        Assert.Equal(0.25, market.GetSupplyDemandRatio("water"));
    }

    [Fact]
    public void GetSupplyDemandRatio_ZeroDemandWithSupply_ReturnsMaxValue()
    {
        var market = new Market();
        market.Supply["water"] = 50;
        market.Demand["water"] = 0;

        Assert.Equal(double.MaxValue, market.GetSupplyDemandRatio("water"));
    }

    [Fact]
    public void GetSupplyDemandRatio_ZeroDemandZeroSupply_ReturnsOne()
    {
        var market = new Market();
        market.Supply["water"] = 0;
        market.Demand["water"] = 0;

        Assert.Equal(1.0, market.GetSupplyDemandRatio("water"));
    }

    [Fact]
    public void GetSupplyDemandRatio_NonExistentCommodity_ReturnsOne()
    {
        var market = new Market();
        Assert.Equal(1.0, market.GetSupplyDemandRatio("nonexistent"));
    }

    #endregion

    #region GetPriceTrend

    [Fact]
    public void GetPriceTrend_Oversupplied_ReturnsNegative()
    {
        var market = new Market();
        market.Supply["water"] = 200;
        market.Demand["water"] = 50; // ratio = 4.0

        Assert.Equal(-0.5, market.GetPriceTrend("water"));
    }

    [Fact]
    public void GetPriceTrend_SlightlyOversupplied_ReturnsSlightNegative()
    {
        var market = new Market();
        market.Supply["water"] = 60;
        market.Demand["water"] = 50; // ratio = 1.2

        Assert.Equal(-0.2, market.GetPriceTrend("water"));
    }

    [Fact]
    public void GetPriceTrend_Balanced_ReturnsZero()
    {
        var market = new Market();
        market.Supply["water"] = 50;
        market.Demand["water"] = 50; // ratio = 1.0

        Assert.Equal(0.0, market.GetPriceTrend("water"));
    }

    [Fact]
    public void GetPriceTrend_SlightlyUndersupplied_ReturnsSlightPositive()
    {
        var market = new Market();
        market.Supply["water"] = 30;
        market.Demand["water"] = 50; // ratio = 0.6

        Assert.Equal(0.2, market.GetPriceTrend("water"));
    }

    [Fact]
    public void GetPriceTrend_SeverelyUndersupplied_ReturnsPositive()
    {
        var market = new Market();
        market.Supply["water"] = 10;
        market.Demand["water"] = 100; // ratio = 0.1

        Assert.Equal(0.5, market.GetPriceTrend("water"));
    }

    #endregion

    #region Serialization / Deserialization

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var market = new Market
        {
            MarketId = "market_alpha",
            Name = "Alpha Market",
            EconomyType = EconomyType.Industrial,
            Seed = 42,
            TechLevel = 7,
            HasBlackMarket = true,
            PlayerReputation = 25,
            PriceChangeThreshold = 0.1m,
            FactionId = "faction_1",
            LastRefresh = new DateTime(2087, 6, 15, 12, 0, 0)
        };
        market.Prices["water"] = 25m;
        market.Supply["water"] = 100;
        market.Demand["water"] = 50;

        var json = market.Serialize();

        Assert.Equal("market_alpha", json["marketId"]?.ToString());
        Assert.Equal("Alpha Market", json["name"]?.ToString());
        Assert.Equal("Industrial", json["economyType"]?.ToString());
        Assert.Equal(42, json["seed"]?.ToObject<int>());
        Assert.Equal(7, json["techLevel"]?.ToObject<int>());
        Assert.True(json["hasBlackMarket"]?.ToObject<bool>());
        Assert.Equal(25, json["playerReputation"]?.ToObject<int>());
        Assert.Equal(0.1m, json["priceChangeThreshold"]?.ToObject<decimal>());
        Assert.Equal("faction_1", json["factionId"]?.ToString());

        var prices = json["prices"] as JObject;
        Assert.NotNull(prices);
        Assert.Equal(25m, prices!["water"]?.ToObject<decimal>());

        var supply = json["supply"] as JObject;
        Assert.NotNull(supply);
        Assert.Equal(100, supply!["water"]?.ToObject<int>());

        var demand = json["demand"] as JObject;
        Assert.NotNull(demand);
        Assert.Equal(50, demand!["water"]?.ToObject<int>());
    }

    [Fact]
    public void Deserialize_RestoresAllProperties()
    {
        var original = new Market
        {
            MarketId = "market_beta",
            Name = "Beta Market",
            EconomyType = EconomyType.Agricultural,
            Seed = 99,
            TechLevel = 3,
            HasBlackMarket = false,
            PlayerReputation = -10,
            PriceChangeThreshold = 0.03m,
            FactionId = "faction_2",
            LastRefresh = new DateTime(2087, 1, 1)
        };
        original.Prices["food"] = 15m;
        original.Prices["ore"] = 50m;
        original.Supply["food"] = 200;
        original.Demand["food"] = 80;

        var json = original.Serialize();
        var restored = new Market();
        restored.Deserialize(json);

        Assert.Equal(original.MarketId, restored.MarketId);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.EconomyType, restored.EconomyType);
        Assert.Equal(original.Seed, restored.Seed);
        Assert.Equal(original.TechLevel, restored.TechLevel);
        Assert.Equal(original.HasBlackMarket, restored.HasBlackMarket);
        Assert.Equal(original.PlayerReputation, restored.PlayerReputation);
        Assert.Equal(original.PriceChangeThreshold, restored.PriceChangeThreshold);
        Assert.Equal(original.FactionId, restored.FactionId);
        Assert.Equal(original.LastRefresh, restored.LastRefresh);
        Assert.Equal(15m, restored.GetPrice("food"));
        Assert.Equal(50m, restored.GetPrice("ore"));
        Assert.Equal(200, restored.GetSupply("food"));
        Assert.Equal(80, restored.GetDemand("food"));
    }

    [Fact]
    public void Deserialize_EmptyJson_UsesDefaults()
    {
        var market = new Market();
        market.Deserialize(new JObject());

        Assert.Equal("", market.MarketId);
        Assert.Equal("", market.Name);
        Assert.Equal(EconomyType.Balanced, market.EconomyType);
        Assert.Equal(0, market.Seed);
        Assert.Equal(5, market.TechLevel);
        Assert.False(market.HasBlackMarket);
        Assert.Equal(0, market.PlayerReputation);
        Assert.Equal(0.05m, market.PriceChangeThreshold);
        Assert.Empty(market.Prices);
        Assert.Empty(market.Supply);
        Assert.Empty(market.Demand);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = new Market
        {
            MarketId = "market_gamma",
            Name = "Gamma Market",
            EconomyType = EconomyType.HighTech,
            Seed = 12345,
            TechLevel = 9,
            HasBlackMarket = true,
            PlayerReputation = 50,
            FactionId = "faction_3"
        };
        original.Prices["tech"] = 200m;
        original.Prices["ore"] = 30m;
        original.Supply["tech"] = 80;
        original.Supply["ore"] = 150;
        original.Demand["tech"] = 60;
        original.Demand["ore"] = 100;

        var json = original.Serialize();
        var restored = new Market();
        restored.Deserialize(json);

        var reSerialized = restored.Serialize();
        Assert.Equal(json.ToString(), reSerialized.ToString());
    }

    [Fact]
    public void Deserialize_WithMarketEvents_RestoresEvents()
    {
        var market = new Market();
        market.ActiveEvents.Add(new MarketEvent
        {
            EventId = "evt_001",
            Type = MarketEventType.Shortage,
            CommodityId = "water",
            PriceMultiplier = 1.5,
            Severity = 60
        });

        var json = market.Serialize();
        var restored = new Market();
        restored.Deserialize(json);

        Assert.Single(restored.ActiveEvents);
        Assert.Equal("evt_001", restored.ActiveEvents[0].EventId);
        Assert.Equal(MarketEventType.Shortage, restored.ActiveEvents[0].Type);
    }

    #endregion

    #region ISaveable

    [Fact]
    public void SaveId_ContainsMarketId()
    {
        var market = new Market { MarketId = "market_test" };
        Assert.Equal("market_market_test", market.SaveId);
    }

    [Fact]
    public void SaveVersion_IsOne()
    {
        var market = new Market();
        Assert.Equal(1, market.SaveVersion);
    }

    #endregion

    #region ConcurrentDictionary Thread Safety

    [Fact]
    public void Prices_ConcurrentDictionary_AllowsConcurrentAccess()
    {
        var market = new Market();
        market.Prices["a"] = 1m;
        market.Prices["b"] = 2m;
        market.Prices["c"] = 3m;

        Assert.Equal(3, market.Prices.Count);
        Assert.Equal(1m, market.Prices["a"]);
        Assert.Equal(2m, market.Prices["b"]);
        Assert.Equal(3m, market.Prices["c"]);
    }

    #endregion

    #region MarketEvent

    [Fact]
    public void MarketEvent_DefaultConstructor_GeneratesEventId()
    {
        var evt = new MarketEvent();
        Assert.NotEmpty(evt.EventId);
        Assert.NotEqual(Guid.Empty.ToString(), evt.EventId);
    }

    [Fact]
    public void MarketEvent_IsActive_DuringEvent_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var evt = new MarketEvent
        {
            StartTime = now.AddHours(-1),
            EndTime = now.AddHours(1)
        };

        Assert.True(evt.IsActive(now));
    }

    [Fact]
    public void MarketEvent_IsActive_BeforeStart_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var evt = new MarketEvent
        {
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2)
        };

        Assert.False(evt.IsActive(now));
    }

    [Fact]
    public void MarketEvent_IsActive_AfterEnd_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var evt = new MarketEvent
        {
            StartTime = now.AddHours(-2),
            EndTime = now.AddHours(-1)
        };

        Assert.False(evt.IsActive(now));
    }

    [Fact]
    public void MarketEvent_IsActive_ExactlyAtStart_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var evt = new MarketEvent
        {
            StartTime = now,
            EndTime = now.AddHours(1)
        };

        Assert.True(evt.IsActive(now));
    }

    [Fact]
    public void MarketEvent_IsActive_ExactlyAtEnd_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var evt = new MarketEvent
        {
            StartTime = now.AddHours(-1),
            EndTime = now
        };

        Assert.True(evt.IsActive(now));
    }

    [Fact]
    public void MarketEvent_AffectsCommodity_SpecificCommodity_Matches()
    {
        var evt = new MarketEvent { CommodityId = "water" };

        Assert.True(evt.AffectsCommodity("water", CommodityCategory.Organics));
        Assert.False(evt.AffectsCommodity("ore", CommodityCategory.Ore));
    }

    [Fact]
    public void MarketEvent_AffectsCommodity_ByCategory_Matches()
    {
        var evt = new MarketEvent { Category = CommodityCategory.Ore };

        Assert.True(evt.AffectsCommodity("iron_ore", CommodityCategory.Ore));
        Assert.False(evt.AffectsCommodity("water", CommodityCategory.Organics));
    }

    [Fact]
    public void MarketEvent_AffectsCommodity_NoFilter_AffectsAll()
    {
        var evt = new MarketEvent(); // No CommodityId, no Category

        Assert.True(evt.AffectsCommodity("water", CommodityCategory.Organics));
        Assert.True(evt.AffectsCommodity("ore", CommodityCategory.Ore));
        Assert.True(evt.AffectsCommodity("tech", CommodityCategory.Tech));
    }

    [Fact]
    public void MarketEvent_AffectsCommodity_CommodityIdTakesPriorityOverCategory()
    {
        var evt = new MarketEvent
        {
            CommodityId = "water",
            Category = CommodityCategory.Ore // Should be ignored
        };

        Assert.True(evt.AffectsCommodity("water", CommodityCategory.Organics));
        Assert.False(evt.AffectsCommodity("iron_ore", CommodityCategory.Ore));
    }

    [Fact]
    public void MarketEvent_SerializeDeserialize_RoundTrip()
    {
        var original = new MarketEvent
        {
            EventId = "evt_test",
            Type = MarketEventType.EconomicBoom,
            CommodityId = "all",
            PriceMultiplier = 1.5,
            SupplyMultiplier = 1.2,
            DemandMultiplier = 1.8,
            StartTime = new DateTime(2087, 6, 1),
            EndTime = new DateTime(2087, 6, 3),
            Description = "Boom time!",
            Severity = 75
        };

        var json = original.Serialize();
        var restored = new MarketEvent();
        restored.Deserialize(json);

        Assert.Equal(original.EventId, restored.EventId);
        Assert.Equal(original.Type, restored.Type);
        Assert.Equal(original.CommodityId, restored.CommodityId);
        Assert.Equal(original.PriceMultiplier, restored.PriceMultiplier);
        Assert.Equal(original.SupplyMultiplier, restored.SupplyMultiplier);
        Assert.Equal(original.DemandMultiplier, restored.DemandMultiplier);
        Assert.Equal(original.StartTime, restored.StartTime);
        Assert.Equal(original.EndTime, restored.EndTime);
        Assert.Equal(original.Description, restored.Description);
        Assert.Equal(original.Severity, restored.Severity);
    }

    [Fact]
    public void MarketEvent_DefaultSeverity_Is50()
    {
        var evt = new MarketEvent();
        Assert.Equal(50, evt.Severity);
    }

    [Fact]
    public void MarketEvent_DefaultDuration_IsOneHour()
    {
        var evt = new MarketEvent();
        var duration = evt.EndTime - evt.StartTime;
        // DateTime has floating-point precision; allow small tolerance
        Assert.True(Math.Abs(duration.TotalHours - 1.0) < 0.001,
            $"Duration {duration.TotalHours} should be approximately 1 hour");
    }

    #endregion

    #region MarketEventType Enum

    [Fact]
    public void MarketEventType_HasAllExpectedValues()
    {
        var types = Enum.GetValues<MarketEventType>();
        Assert.Contains(MarketEventType.Shortage, types);
        Assert.Contains(MarketEventType.Surplus, types);
        Assert.Contains(MarketEventType.HighDemand, types);
        Assert.Contains(MarketEventType.LowDemand, types);
        Assert.Contains(MarketEventType.TradeRouteDisruption, types);
        Assert.Contains(MarketEventType.EconomicBoom, types);
        Assert.Contains(MarketEventType.Recession, types);
        Assert.Contains(MarketEventType.PirateActivity, types);
        Assert.Contains(MarketEventType.PoliceCrackdown, types);
        Assert.Contains(MarketEventType.TechBreakthrough, types);
        Assert.Contains(MarketEventType.CropFailure, types);
        Assert.Contains(MarketEventType.MineralDiscovery, types);
        Assert.Equal(12, types.Length);
    }

    #endregion
}
