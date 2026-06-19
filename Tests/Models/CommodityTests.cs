using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

/// <summary>
/// Tests for Commodity model: validation, cloning, serialization, registry, and category/legality enums.
/// </summary>
[Collection("Sequential")]
public class CommodityTests
{
    #region Validation

    [Fact]
    public void Validate_ValidCommodity_ReturnsTrue()
    {
        var commodity = CreateValidCommodity();
        var result = commodity.Validate(out var error);
        Assert.True(result);
        Assert.Empty(error);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.Id = "";
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("ID", error);
    }

    [Fact]
    public void Validate_WhitespaceId_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.Id = "   ";
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("ID", error);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.Name = "";
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("name", error);
    }

    [Fact]
    public void Validate_WhitespaceName_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.Name = "  \t  ";
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("name", error);
    }

    [Fact]
    public void Validate_ZeroBasePrice_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.BasePrice = 0m;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Base price", error);
    }

    [Fact]
    public void Validate_NegativeBasePrice_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.BasePrice = -50m;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Base price", error);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Validate_VolatilityOutOfRange_ReturnsFalse(double volatility)
    {
        var commodity = CreateValidCommodity();
        commodity.Volatility = volatility;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Volatility", error);
    }

    [Fact]
    public void Validate_VolatilityAtBoundaries_ReturnsTrue()
    {
        var commodity = CreateValidCommodity();
        commodity.Volatility = 0.0;
        Assert.True(commodity.Validate(out _));

        commodity.Volatility = 1.0;
        Assert.True(commodity.Validate(out _));
    }

    [Fact]
    public void Validate_NegativeBaseVolume_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.BaseVolume = -1;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Base volume", error);
    }

    [Fact]
    public void Validate_ZeroBaseVolume_ReturnsTrue()
    {
        var commodity = CreateValidCommodity();
        commodity.BaseVolume = 0;
        Assert.True(commodity.Validate(out _));
    }

    [Fact]
    public void Validate_ZeroMassPerUnit_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.MassPerUnit = 0;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Mass per unit", error);
    }

    [Fact]
    public void Validate_NegativeMassPerUnit_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.MassPerUnit = -5.0;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Mass per unit", error);
    }

    [Fact]
    public void Validate_MinPriceZero_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.MinPrice = 0m;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Min price", error);
    }

    [Fact]
    public void Validate_MinPriceGreaterThanMaxPrice_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.MinPrice = 500m;
        commodity.MaxPrice = 100m;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Min price", error);
    }

    [Fact]
    public void Validate_MinPriceEqualsMaxPrice_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.MinPrice = 100m;
        commodity.MaxPrice = 100m;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Min price", error);
    }

    [Fact]
    public void Validate_MaxPriceZero_ReturnsFalse()
    {
        var commodity = CreateValidCommodity();
        commodity.MaxPrice = 0m;
        var result = commodity.Validate(out var error);
        Assert.False(result);
        Assert.Contains("Min price", error);
    }

    #endregion

    #region Clone

    [Fact]
    public void Clone_ReturnsDeepCopy()
    {
        var original = CreateValidCommodity();
        original.Tags.Add("perishable");
        original.Tags.Add("radioactive");

        var clone = original.Clone();

        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Category, clone.Category);
        Assert.Equal(original.BasePrice, clone.BasePrice);
        Assert.Equal(original.Volatility, clone.Volatility);
        Assert.Equal(original.Legality, clone.Legality);
        Assert.Equal(original.BaseVolume, clone.BaseVolume);
        Assert.Equal(original.MassPerUnit, clone.MassPerUnit);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.MinPrice, clone.MinPrice);
        Assert.Equal(original.MaxPrice, clone.MaxPrice);
    }

    [Fact]
    public void Clone_CopiesTags()
    {
        var original = CreateValidCommodity();
        original.Tags.Add("perishable");
        original.Tags.Add("radioactive");

        var clone = original.Clone();

        Assert.Equal(2, clone.Tags.Count);
        Assert.Contains("perishable", clone.Tags);
        Assert.Contains("radioactive", clone.Tags);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var original = CreateValidCommodity();
        original.Tags.Add("perishable");

        var clone = original.Clone();
        clone.Name = "Modified";
        clone.BasePrice = 999m;
        clone.Tags.Add("fragile");

        Assert.NotEqual("Modified", original.Name);
        Assert.NotEqual(999m, original.BasePrice);
        Assert.DoesNotContain("fragile", original.Tags);
    }

    [Fact]
    public void Clone_ModifyingOriginalAfterCloneDoesNotAffectClone()
    {
        var original = CreateValidCommodity();
        var clone = original.Clone();

        original.Tags.Add("perishable");

        Assert.DoesNotContain("perishable", clone.Tags);
    }

    #endregion

    #region Serialization / Deserialization

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var commodity = CreateValidCommodity();
        commodity.Tags.Add("perishable");

        var json = commodity.Serialize();

        Assert.Equal("test_ore", json["id"]?.ToString());
        Assert.Equal("Test Ore", json["name"]?.ToString());
        Assert.Equal("Ore", json["category"]?.ToString());
        Assert.Equal(100m, json["basePrice"]?.ToObject<decimal>());
        Assert.Equal(0.2, json["volatility"]?.ToObject<double>());
        Assert.Equal("Legal", json["legality"]?.ToString());
        Assert.Equal(200, json["baseVolume"]?.ToObject<int>());
        Assert.Equal(2.5, json["massPerUnit"]?.ToObject<double>());
        Assert.Equal("A test commodity", json["description"]?.ToString());
        Assert.Equal(10m, json["minPrice"]?.ToObject<decimal>());
        Assert.Equal(5000m, json["maxPrice"]?.ToObject<decimal>());

        var tags = json["tags"] as JArray;
        Assert.NotNull(tags);
        Assert.Contains("perishable", tags!.Select(t => t.ToString()));
    }

    [Fact]
    public void Deserialize_RestoresAllProperties()
    {
        var original = CreateValidCommodity();
        original.Tags.Add("perishable");
        original.Tags.Add("radioactive");
        var json = original.Serialize();

        var restored = new Commodity();
        restored.Deserialize(json);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Category, restored.Category);
        Assert.Equal(original.BasePrice, restored.BasePrice);
        Assert.Equal(original.Volatility, restored.Volatility);
        Assert.Equal(original.Legality, restored.Legality);
        Assert.Equal(original.BaseVolume, restored.BaseVolume);
        Assert.Equal(original.MassPerUnit, restored.MassPerUnit);
        Assert.Equal(original.Description, restored.Description);
        Assert.Equal(original.MinPrice, restored.MinPrice);
        Assert.Equal(original.MaxPrice, restored.MaxPrice);
        Assert.Equal(2, restored.Tags.Count);
        Assert.Contains("perishable", restored.Tags);
        Assert.Contains("radioactive", restored.Tags);
    }

    [Fact]
    public void Deserialize_EmptyJson_UsesDefaults()
    {
        var commodity = new Commodity();
        commodity.Deserialize(new JObject());

        Assert.Equal("", commodity.Id);
        Assert.Equal("", commodity.Name);
        Assert.Equal(CommodityCategory.Ore, commodity.Category);
        Assert.Equal(100m, commodity.BasePrice);
        Assert.Equal(0.1, commodity.Volatility);
        Assert.Equal(CommodityLegality.Legal, commodity.Legality);
        Assert.Equal(100, commodity.BaseVolume);
        Assert.Equal(1.0, commodity.MassPerUnit);
        Assert.Equal("", commodity.Description);
        Assert.Equal(10m, commodity.MinPrice);
        Assert.Equal(10000m, commodity.MaxPrice);
        Assert.Empty(commodity.Tags);
    }

    [Fact]
    public void Deserialize_InvalidEnumValues_UsesDefaults()
    {
        var json = new JObject
        {
            ["category"] = "InvalidCategory",
            ["legality"] = "NotALegality"
        };

        var commodity = new Commodity();
        commodity.Deserialize(json);

        Assert.Equal(CommodityCategory.Ore, commodity.Category);
        Assert.Equal(CommodityLegality.Legal, commodity.Legality);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = CreateValidCommodity();
        original.Tags.Add("perishable");
        original.Tags.Add("radioactive");
        original.Tags.Add("fragile");

        var json = original.Serialize();
        var restored = new Commodity();
        restored.Deserialize(json);

        var reSerialized = restored.Serialize();
        Assert.Equal(json.ToString(), reSerialized.ToString());
    }

    #endregion

    #region ISaveable

    [Fact]
    public void SaveId_ContainsCommodityId()
    {
        var commodity = CreateValidCommodity();
        Assert.Equal("commodity_test_ore", commodity.SaveId);
    }

    [Fact]
    public void SaveVersion_IsOne()
    {
        var commodity = new Commodity();
        Assert.Equal(1, commodity.SaveVersion);
    }

    #endregion

    #region Default Values

    [Fact]
    public void DefaultConstructor_SetsSensibleDefaults()
    {
        var commodity = new Commodity();

        Assert.Equal("", commodity.Id);
        Assert.Equal("", commodity.Name);
        Assert.Equal(CommodityCategory.Ore, commodity.Category);
        Assert.Equal(100m, commodity.BasePrice);
        Assert.Equal(0.1, commodity.Volatility);
        Assert.Equal(CommodityLegality.Legal, commodity.Legality);
        Assert.Equal(100, commodity.BaseVolume);
        Assert.Equal(1.0, commodity.MassPerUnit);
        Assert.Equal("", commodity.Description);
        Assert.Equal(10m, commodity.MinPrice);
        Assert.Equal(10000m, commodity.MaxPrice);
        Assert.Empty(commodity.Tags);
    }

    #endregion

    #region CommodityCategory Enum

    [Fact]
    public void CommodityCategory_HasAllExpectedValues()
    {
        var categories = Enum.GetValues<CommodityCategory>();
        Assert.Contains(CommodityCategory.Ore, categories);
        Assert.Contains(CommodityCategory.Organics, categories);
        Assert.Contains(CommodityCategory.Tech, categories);
        Assert.Contains(CommodityCategory.Luxury, categories);
        Assert.Contains(CommodityCategory.Weapons, categories);
        Assert.Contains(CommodityCategory.Medical, categories);
        Assert.Contains(CommodityCategory.Illegal, categories);
        Assert.Equal(7, categories.Length);
    }

    #endregion

    #region CommodityLegality Enum

    [Fact]
    public void CommodityLegality_HasAllExpectedValues()
    {
        var legalities = Enum.GetValues<CommodityLegality>();
        Assert.Contains(CommodityLegality.Legal, legalities);
        Assert.Contains(CommodityLegality.Restricted, legalities);
        Assert.Contains(CommodityLegality.Illegal, legalities);
        Assert.Equal(3, legalities.Length);
    }

    #endregion

    #region CommodityRegistry

    [Fact]
    public void Registry_Register_AddsCommodity()
    {
        CommodityRegistry.Clear();
        var commodity = CreateValidCommodity();

        CommodityRegistry.Register(commodity);

        var retrieved = CommodityRegistry.Get("test_ore");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Ore", retrieved!.Name);
    }

    [Fact]
    public void Registry_Register_InvalidCommodity_Throws()
    {
        CommodityRegistry.Clear();
        var commodity = new Commodity(); // Invalid: empty ID

        var ex = Assert.Throws<ArgumentException>(() => CommodityRegistry.Register(commodity));
        Assert.Contains("Invalid commodity", ex.Message);
    }

    [Fact]
    public void Registry_Get_NonExistent_ReturnsNull()
    {
        CommodityRegistry.Clear();
        var result = CommodityRegistry.Get("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void Registry_All_ReturnsAllRegistered()
    {
        CommodityRegistry.Clear();
        var c1 = CreateValidCommodity();
        var c2 = CreateValidCommodity("test_food", "Test Food", CommodityCategory.Organics);

        CommodityRegistry.Register(c1);
        CommodityRegistry.Register(c2);

        // Materialize to a list to avoid concurrent modification issues
        var all = CommodityRegistry.All.ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, c => c.Id == "test_ore");
        Assert.Contains(all, c => c.Id == "test_food");
    }

    [Fact]
    public void Registry_GetByCategory_ReturnsCorrectCommodities()
    {
        CommodityRegistry.Clear();
        var ore = CreateValidCommodity("test_ore", "Test Ore", CommodityCategory.Ore);
        var food = CreateValidCommodity("test_food", "Test Food", CommodityCategory.Organics);
        var tech = CreateValidCommodity("test_tech", "Test Tech", CommodityCategory.Tech);

        CommodityRegistry.Register(ore);
        CommodityRegistry.Register(food);
        CommodityRegistry.Register(tech);

        var ores = CommodityRegistry.GetByCategory(CommodityCategory.Ore);
        Assert.Single(ores);
        Assert.Equal("test_ore", ores[0].Id);

        var organics = CommodityRegistry.GetByCategory(CommodityCategory.Organics);
        Assert.Single(organics);
        Assert.Equal("test_food", organics[0].Id);
    }

    [Fact]
    public void Registry_GetByCategory_EmptyCategory_ReturnsEmptyList()
    {
        CommodityRegistry.Clear();
        var result = CommodityRegistry.GetByCategory(CommodityCategory.Weapons);
        Assert.Empty(result);
    }

    [Fact]
    public void Registry_Clear_RemovesAll()
    {
        CommodityRegistry.Clear();
        CommodityRegistry.Register(CreateValidCommodity());

        CommodityRegistry.Clear();

        Assert.Empty(CommodityRegistry.All);
        Assert.Null(CommodityRegistry.Get("test_ore"));
    }

    [Fact]
    public void Registry_LoadFromJson_PopulatesRegistry()
    {
        CommodityRegistry.Clear();
        var json = @"[
            {
                ""id"": ""water"",
                ""name"": ""Water"",
                ""category"": ""Organics"",
                ""basePrice"": 10,
                ""volatility"": 0.05,
                ""legality"": ""Legal"",
                ""baseVolume"": 500,
                ""massPerUnit"": 1.0,
                ""description"": ""Fresh water"",
                ""minPrice"": 5,
                ""maxPrice"": 50
            },
            {
                ""id"": ""iron_ore"",
                ""name"": ""Iron Ore"",
                ""category"": ""Ore"",
                ""basePrice"": 50,
                ""volatility"": 0.15,
                ""legality"": ""Legal"",
                ""baseVolume"": 300,
                ""massPerUnit"": 3.0,
                ""description"": ""Raw iron ore"",
                ""minPrice"": 20,
                ""maxPrice"": 200
            }
        ]";

        CommodityRegistry.LoadFromJson(json);

        Assert.Equal(2, CommodityRegistry.All.Count);

        var water = CommodityRegistry.Get("water");
        Assert.NotNull(water);
        Assert.Equal("Water", water!.Name);
        Assert.Equal(CommodityCategory.Organics, water.Category);
        Assert.Equal(10m, water.BasePrice);

        var iron = CommodityRegistry.Get("iron_ore");
        Assert.NotNull(iron);
        Assert.Equal("Iron Ore", iron!.Name);
        Assert.Equal(CommodityCategory.Ore, iron.Category);
        Assert.Equal(50m, iron.BasePrice);
    }

    [Fact]
    public void Registry_LoadFromJson_ClearsPreviousData()
    {
        CommodityRegistry.Clear();
        CommodityRegistry.Register(CreateValidCommodity());

        CommodityRegistry.LoadFromJson(@"[{""id"":""water"",""name"":""Water"",""category"":""Organics"",""basePrice"":10,""volatility"":0.05,""legality"":""Legal"",""baseVolume"":500,""massPerUnit"":1.0,""minPrice"":5,""maxPrice"":50}]");

        Assert.Single(CommodityRegistry.All);
        Assert.Null(CommodityRegistry.Get("test_ore"));
        Assert.NotNull(CommodityRegistry.Get("water"));
    }

    [Fact]
    public void Registry_LoadFromJson_InvalidCommodity_Throws()
    {
        CommodityRegistry.Clear();
        // Missing required fields will cause validation failure
        var json = @"[{""id"":"""",""name"":""""}]";

        Assert.Throws<ArgumentException>(() => CommodityRegistry.LoadFromJson(json));
    }

    #endregion

    #region Helpers

    private static Commodity CreateValidCommodity(
        string id = "test_ore",
        string name = "Test Ore",
        CommodityCategory category = CommodityCategory.Ore)
    {
        return new Commodity
        {
            Id = id,
            Name = name,
            Category = category,
            BasePrice = 100m,
            Volatility = 0.2,
            Legality = CommodityLegality.Legal,
            BaseVolume = 200,
            MassPerUnit = 2.5,
            Description = "A test commodity",
            MinPrice = 10m,
            MaxPrice = 5000m
        };
    }

    #endregion
}
