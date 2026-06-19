using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

/// <summary>
/// Tests for Planet model: connections/jump routes, cloning, serialization,
/// registry, services, and planet type enum.
/// </summary>
[Collection("Sequential")]
public class PlanetTests
{
    #region Construction & Defaults

    [Fact]
    public void DefaultConstructor_SetsSensibleDefaults()
    {
        var planet = new Planet();

        Assert.Equal("", planet.Id);
        Assert.Equal("", planet.Name);
        Assert.Equal(PlanetType.Terrestrial, planet.Type);
        Assert.Equal("", planet.SystemName);
        Assert.Equal(0, planet.OrbitIndex);
        Assert.Equal("", planet.FactionId);
        Assert.Equal(FactionAlignment.Neutral, planet.PlayerAlignment);
        Assert.Equal(EconomyType.Balanced, planet.EconomyType);
        Assert.Equal(5, planet.TechLevel);
        Assert.Equal(1, planet.Population);
        Assert.Equal(5, planet.SecurityLevel);
        Assert.NotNull(planet.Market);
        Assert.Empty(planet.ConnectedLocations);
        Assert.Empty(planet.Distances);
        Assert.Empty(planet.FuelCosts);
        Assert.Equal(10, planet.TravelDanger);
        Assert.False(planet.HasShipyard);
        Assert.False(planet.HasOutfitter);
        Assert.True(planet.HasMissionBoard);
        Assert.True(planet.HasCommodityExchange);
        Assert.False(planet.HasBlackMarket);
        Assert.Empty(planet.Features);
        Assert.Equal("", planet.Description);
        Assert.Equal("", planet.ImageResource);
        Assert.False(planet.IsDiscovered);
        Assert.False(planet.IsHomeBase);
    }

    #endregion

    #region Connections / Jump Routes

    [Fact]
    public void AddConnection_AddsToAllCollections()
    {
        var planet = new Planet();

        planet.AddConnection("alpha_station", 5.2, 50);

        Assert.Contains("alpha_station", planet.ConnectedLocations);
        Assert.Equal(5.2, planet.Distances["alpha_station"]);
        Assert.Equal(50, planet.FuelCosts["alpha_station"]);
    }

    [Fact]
    public void AddConnection_DuplicateLocation_UpdatesValues()
    {
        var planet = new Planet();

        planet.AddConnection("alpha_station", 5.2, 50);
        planet.AddConnection("alpha_station", 3.0, 30);

        // Should not duplicate in ConnectedLocations
        Assert.Single(planet.ConnectedLocations);
        // But should update distance and fuel cost
        Assert.Equal(3.0, planet.Distances["alpha_station"]);
        Assert.Equal(30, planet.FuelCosts["alpha_station"]);
    }

    [Fact]
    public void AddConnection_MultipleLocations()
    {
        var planet = new Planet();

        planet.AddConnection("alpha", 5.0, 50);
        planet.AddConnection("beta", 10.0, 100);
        planet.AddConnection("gamma", 15.0, 150);

        Assert.Equal(3, planet.ConnectedLocations.Count);
        Assert.Equal(3, planet.Distances.Count);
        Assert.Equal(3, planet.FuelCosts.Count);
    }

    [Fact]
    public void RemoveConnection_RemovesFromAllCollections()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.0, 50);
        planet.AddConnection("beta", 10.0, 100);

        planet.RemoveConnection("alpha");

        Assert.DoesNotContain("alpha", planet.ConnectedLocations);
        Assert.False(planet.Distances.ContainsKey("alpha"));
        Assert.False(planet.FuelCosts.ContainsKey("alpha"));
        Assert.Contains("beta", planet.ConnectedLocations);
    }

    [Fact]
    public void RemoveConnection_NonExistent_DoesNotThrow()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.0, 50);

        planet.RemoveConnection("nonexistent");

        Assert.Single(planet.ConnectedLocations);
    }

    [Fact]
    public void IsConnectedTo_ExistingConnection_ReturnsTrue()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.0, 50);

        Assert.True(planet.IsConnectedTo("alpha"));
    }

    [Fact]
    public void IsConnectedTo_NonExistentConnection_ReturnsFalse()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.0, 50);

        Assert.False(planet.IsConnectedTo("beta"));
    }

    [Fact]
    public void IsConnectedTo_AfterRemoval_ReturnsFalse()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.0, 50);
        planet.RemoveConnection("alpha");

        Assert.False(planet.IsConnectedTo("alpha"));
    }

    [Fact]
    public void GetDistanceTo_ExistingConnection_ReturnsDistance()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.2, 50);

        Assert.Equal(5.2, planet.GetDistanceTo("alpha"));
    }

    [Fact]
    public void GetDistanceTo_NonExistentConnection_ReturnsMaxValue()
    {
        var planet = new Planet();

        Assert.Equal(double.MaxValue, planet.GetDistanceTo("nonexistent"));
    }

    [Fact]
    public void GetFuelCostTo_ExistingConnection_ReturnsCost()
    {
        var planet = new Planet();
        planet.AddConnection("alpha", 5.2, 50);

        Assert.Equal(50, planet.GetFuelCostTo("alpha"));
    }

    [Fact]
    public void GetFuelCostTo_NonExistentConnection_ReturnsMaxValue()
    {
        var planet = new Planet();

        Assert.Equal(int.MaxValue, planet.GetFuelCostTo("nonexistent"));
    }

    #endregion

    #region GetAvailableServices

    [Fact]
    public void GetAvailableServices_DefaultPlanet_ReturnsCommodityExchangeAndMissionBoard()
    {
        var planet = new Planet();

        var services = planet.GetAvailableServices();

        Assert.Contains("Commodity Exchange", services);
        Assert.Contains("Mission Board", services);
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void GetAvailableServices_AllServices_ReturnsAll()
    {
        var planet = new Planet
        {
            HasCommodityExchange = true,
            HasShipyard = true,
            HasOutfitter = true,
            HasMissionBoard = true,
            HasBlackMarket = true
        };

        var services = planet.GetAvailableServices();

        Assert.Equal(5, services.Count);
        Assert.Contains("Commodity Exchange", services);
        Assert.Contains("Shipyard", services);
        Assert.Contains("Outfitter", services);
        Assert.Contains("Mission Board", services);
        Assert.Contains("Black Market", services);
    }

    [Fact]
    public void GetAvailableServices_NoServices_ReturnsEmpty()
    {
        var planet = new Planet
        {
            HasCommodityExchange = false,
            HasShipyard = false,
            HasOutfitter = false,
            HasMissionBoard = false,
            HasBlackMarket = false
        };

        var services = planet.GetAvailableServices();

        Assert.Empty(services);
    }

    #endregion

    #region Clone

    [Fact]
    public void Clone_ReturnsDeepCopy()
    {
        var original = CreateTestPlanet();
        original.AddConnection("alpha", 5.0, 50);
        original.AddConnection("beta", 10.0, 100);
        original.Features.Add("trading_hub");
        original.Features.Add("pirate_haven");
        original.Market.Prices["water"] = 25m;

        var clone = original.Clone();

        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.SystemName, clone.SystemName);
        Assert.Equal(original.OrbitIndex, clone.OrbitIndex);
        Assert.Equal(original.FactionId, clone.FactionId);
        Assert.Equal(original.PlayerAlignment, clone.PlayerAlignment);
        Assert.Equal(original.EconomyType, clone.EconomyType);
        Assert.Equal(original.TechLevel, clone.TechLevel);
        Assert.Equal(original.Population, clone.Population);
        Assert.Equal(original.SecurityLevel, clone.SecurityLevel);
        Assert.Equal(original.TravelDanger, clone.TravelDanger);
        Assert.Equal(original.HasShipyard, clone.HasShipyard);
        Assert.Equal(original.HasOutfitter, clone.HasOutfitter);
        Assert.Equal(original.HasMissionBoard, clone.HasMissionBoard);
        Assert.Equal(original.HasCommodityExchange, clone.HasCommodityExchange);
        Assert.Equal(original.HasBlackMarket, clone.HasBlackMarket);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.ImageResource, clone.ImageResource);
        Assert.Equal(original.IsDiscovered, clone.IsDiscovered);
        Assert.Equal(original.IsHomeBase, clone.IsHomeBase);
    }

    [Fact]
    public void Clone_CopiesConnections()
    {
        var original = CreateTestPlanet();
        original.AddConnection("alpha", 5.0, 50);
        original.AddConnection("beta", 10.0, 100);

        var clone = original.Clone();

        Assert.Equal(2, clone.ConnectedLocations.Count);
        Assert.Contains("alpha", clone.ConnectedLocations);
        Assert.Contains("beta", clone.ConnectedLocations);
        Assert.Equal(5.0, clone.GetDistanceTo("alpha"));
        Assert.Equal(50, clone.GetFuelCostTo("alpha"));
    }

    [Fact]
    public void Clone_CopiesFeatures()
    {
        var original = CreateTestPlanet();
        original.Features.Add("trading_hub");
        original.Features.Add("pirate_haven");

        var clone = original.Clone();

        Assert.Equal(2, clone.Features.Count);
        Assert.Contains("trading_hub", clone.Features);
        Assert.Contains("pirate_haven", clone.Features);
    }

    [Fact]
    public void Clone_CopiesMarket()
    {
        var original = CreateTestPlanet();
        original.Market.Prices["water"] = 25m;
        original.Market.Supply["water"] = 100;
        original.Market.Demand["water"] = 50;

        var clone = original.Clone();

        Assert.NotNull(clone.Market);
        Assert.Equal(25m, clone.Market.GetPrice("water"));
        Assert.Equal(100, clone.Market.GetSupply("water"));
        Assert.Equal(50, clone.Market.GetDemand("water"));
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var original = CreateTestPlanet();
        original.AddConnection("alpha", 5.0, 50);
        original.Features.Add("trading_hub");

        var clone = original.Clone();
        clone.Name = "Modified Planet";
        clone.AddConnection("gamma", 20.0, 200);
        clone.Features.Add("new_feature");
        clone.Market.Prices["new_item"] = 100m;

        Assert.NotEqual("Modified Planet", original.Name);
        Assert.DoesNotContain("gamma", original.ConnectedLocations);
        Assert.DoesNotContain("new_feature", original.Features);
        Assert.False(original.Market.Prices.ContainsKey("new_item"));
    }

    [Fact]
    public void Clone_ModifyingOriginalAfterCloneDoesNotAffectClone()
    {
        var original = CreateTestPlanet();
        var clone = original.Clone();

        original.AddConnection("alpha", 5.0, 50);
        original.Features.Add("trading_hub");

        Assert.DoesNotContain("alpha", clone.ConnectedLocations);
        Assert.DoesNotContain("trading_hub", clone.Features);
    }

    [Fact]
    public void Clone_MarketIsIndependent()
    {
        var original = CreateTestPlanet();
        original.Market.Prices["water"] = 25m;

        var clone = original.Clone();
        clone.Market.Prices["water"] = 50m;

        Assert.Equal(25m, original.Market.GetPrice("water"));
        Assert.Equal(50m, clone.Market.GetPrice("water"));
    }

    #endregion

    #region Serialization / Deserialization

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var planet = CreateTestPlanet();
        planet.AddConnection("alpha", 5.2, 50);
        planet.Features.Add("trading_hub");
        planet.Market.Prices["water"] = 25m;

        var json = planet.Serialize();

        Assert.Equal("test_planet", json["id"]?.ToString());
        Assert.Equal("Test Planet", json["name"]?.ToString());
        Assert.Equal("Terrestrial", json["type"]?.ToString());
        Assert.Equal("Sol", json["systemName"]?.ToString());
        Assert.Equal(3, json["orbitIndex"]?.ToObject<int>());
        Assert.Equal("faction_test", json["factionId"]?.ToString());
        Assert.Equal("Neutral", json["playerAlignment"]?.ToString());
        Assert.Equal("Industrial", json["economyType"]?.ToString());
        Assert.Equal(7, json["techLevel"]?.ToObject<int>());
        Assert.Equal(5000000L, json["population"]?.ToObject<long>());
        Assert.Equal(6, json["securityLevel"]?.ToObject<int>());
        Assert.Equal(15, json["travelDanger"]?.ToObject<int>());
        Assert.True(json["hasShipyard"]?.ToObject<bool>());
        Assert.False(json["hasOutfitter"]?.ToObject<bool>());
        Assert.True(json["hasMissionBoard"]?.ToObject<bool>());
        Assert.True(json["hasCommodityExchange"]?.ToObject<bool>());
        Assert.False(json["hasBlackMarket"]?.ToObject<bool>());
        Assert.Equal("A test planet", json["description"]?.ToString());
        Assert.Equal("planet_icon", json["imageResource"]?.ToString());
        Assert.True(json["isDiscovered"]?.ToObject<bool>());
        Assert.False(json["isHomeBase"]?.ToObject<bool>());

        var connArray = json["connectedLocations"] as JArray;
        Assert.NotNull(connArray);
        Assert.Contains("alpha", connArray!.Select(c => c.ToString()));

        var distances = json["distances"] as JObject;
        Assert.NotNull(distances);
        Assert.Equal(5.2, distances!["alpha"]?.ToObject<double>());

        var fuelCosts = json["fuelCosts"] as JObject;
        Assert.NotNull(fuelCosts);
        Assert.Equal(50, fuelCosts!["alpha"]?.ToObject<int>());

        var features = json["features"] as JArray;
        Assert.NotNull(features);
        Assert.Contains("trading_hub", features!.Select(f => f.ToString()));

        var market = json["market"] as JObject;
        Assert.NotNull(market);
        Assert.Equal("market_test_planet", market!["marketId"]?.ToString());
    }

    [Fact]
    public void Deserialize_RestoresAllProperties()
    {
        var original = CreateTestPlanet();
        original.AddConnection("alpha", 5.2, 50);
        original.AddConnection("beta", 10.0, 100);
        original.Features.Add("trading_hub");
        original.Features.Add("pirate_haven");
        original.Market.Prices["water"] = 25m;
        original.Market.Supply["water"] = 100;

        var json = original.Serialize();
        var restored = new Planet();
        restored.Deserialize(json);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Type, restored.Type);
        Assert.Equal(original.SystemName, restored.SystemName);
        Assert.Equal(original.OrbitIndex, restored.OrbitIndex);
        Assert.Equal(original.FactionId, restored.FactionId);
        Assert.Equal(original.PlayerAlignment, restored.PlayerAlignment);
        Assert.Equal(original.EconomyType, restored.EconomyType);
        Assert.Equal(original.TechLevel, restored.TechLevel);
        Assert.Equal(original.Population, restored.Population);
        Assert.Equal(original.SecurityLevel, restored.SecurityLevel);
        Assert.Equal(original.TravelDanger, restored.TravelDanger);
        Assert.Equal(original.HasShipyard, restored.HasShipyard);
        Assert.Equal(original.HasOutfitter, restored.HasOutfitter);
        Assert.Equal(original.HasMissionBoard, restored.HasMissionBoard);
        Assert.Equal(original.HasCommodityExchange, restored.HasCommodityExchange);
        Assert.Equal(original.HasBlackMarket, restored.HasBlackMarket);
        Assert.Equal(original.Description, restored.Description);
        Assert.Equal(original.ImageResource, restored.ImageResource);
        Assert.Equal(original.IsDiscovered, restored.IsDiscovered);
        Assert.Equal(original.IsHomeBase, restored.IsHomeBase);

        Assert.Equal(2, restored.ConnectedLocations.Count);
        Assert.Contains("alpha", restored.ConnectedLocations);
        Assert.Contains("beta", restored.ConnectedLocations);
        Assert.Equal(5.2, restored.GetDistanceTo("alpha"));
        Assert.Equal(50, restored.GetFuelCostTo("alpha"));

        Assert.Equal(2, restored.Features.Count);
        Assert.Contains("trading_hub", restored.Features);
        Assert.Contains("pirate_haven", restored.Features);

        Assert.NotNull(restored.Market);
        Assert.Equal(25m, restored.Market.GetPrice("water"));
        Assert.Equal(100, restored.Market.GetSupply("water"));
    }

    [Fact]
    public void Deserialize_EmptyJson_UsesDefaults()
    {
        var planet = new Planet();
        planet.Deserialize(new JObject());

        Assert.Equal("", planet.Id);
        Assert.Equal("", planet.Name);
        Assert.Equal(PlanetType.Terrestrial, planet.Type);
        Assert.Equal("", planet.SystemName);
        Assert.Equal(0, planet.OrbitIndex);
        Assert.Equal("", planet.FactionId);
        Assert.Equal(FactionAlignment.Neutral, planet.PlayerAlignment);
        Assert.Equal(EconomyType.Balanced, planet.EconomyType);
        Assert.Equal(5, planet.TechLevel);
        Assert.Equal(1, planet.Population);
        Assert.Equal(5, planet.SecurityLevel);
        Assert.Equal(10, planet.TravelDanger);
        Assert.False(planet.HasShipyard);
        Assert.False(planet.HasOutfitter);
        Assert.True(planet.HasMissionBoard);
        Assert.True(planet.HasCommodityExchange);
        Assert.False(planet.HasBlackMarket);
        Assert.Empty(planet.ConnectedLocations);
        Assert.Empty(planet.Distances);
        Assert.Empty(planet.FuelCosts);
        Assert.Empty(planet.Features);
        Assert.Equal("", planet.Description);
        Assert.Equal("", planet.ImageResource);
        Assert.False(planet.IsDiscovered);
        Assert.False(planet.IsHomeBase);
    }

    [Fact]
    public void Deserialize_InvalidEnumValues_UsesDefaults()
    {
        var json = new JObject
        {
            ["type"] = "InvalidType",
            ["playerAlignment"] = "NotAnAlignment",
            ["economyType"] = "NotAnEconomy"
        };

        var planet = new Planet();
        planet.Deserialize(json);

        Assert.Equal(PlanetType.Terrestrial, planet.Type);
        Assert.Equal(FactionAlignment.Neutral, planet.PlayerAlignment);
        Assert.Equal(EconomyType.Balanced, planet.EconomyType);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = CreateTestPlanet();
        original.AddConnection("alpha", 5.2, 50);
        original.AddConnection("beta", 10.0, 100);
        original.Features.Add("trading_hub");
        original.Features.Add("pirate_haven");
        original.Market.Prices["water"] = 25m;
        original.Market.Supply["water"] = 100;
        original.Market.Demand["water"] = 50;

        var json = original.Serialize();
        var restored = new Planet();
        restored.Deserialize(json);

        var reSerialized = restored.Serialize();
        Assert.Equal(json.ToString(), reSerialized.ToString());
    }

    [Fact]
    public void Deserialize_WithoutMarket_CreatesDefaultMarket()
    {
        var json = new JObject
        {
            ["id"] = "test",
            ["name"] = "Test"
        };

        var planet = new Planet();
        planet.Deserialize(json);

        Assert.NotNull(planet.Market);
        Assert.Equal("", planet.Market.MarketId);
    }

    #endregion

    #region ISaveable

    [Fact]
    public void SaveId_ContainsPlanetId()
    {
        var planet = new Planet { Id = "earth" };
        Assert.Equal("planet_earth", planet.SaveId);
    }

    [Fact]
    public void SaveVersion_IsOne()
    {
        var planet = new Planet();
        Assert.Equal(1, planet.SaveVersion);
    }

    #endregion

    #region PlanetType Enum

    [Fact]
    public void PlanetType_HasAllExpectedValues()
    {
        var types = Enum.GetValues<PlanetType>();
        Assert.Contains(PlanetType.Terrestrial, types);
        Assert.Contains(PlanetType.Mining, types);
        Assert.Contains(PlanetType.GasGiant, types);
        Assert.Contains(PlanetType.Ice, types);
        Assert.Contains(PlanetType.Desert, types);
        Assert.Contains(PlanetType.Ocean, types);
        Assert.Contains(PlanetType.Station, types);
        Assert.Contains(PlanetType.AsteroidBase, types);
        Assert.Contains(PlanetType.Derelict, types);
        Assert.Contains(PlanetType.Capital, types);
        Assert.Equal(10, types.Length);
    }

    #endregion

    #region PlanetRegistry

    [Fact]
    public void Registry_Register_AddsPlanet()
    {
        PlanetRegistry.Clear();
        var planet = CreateTestPlanet();

        PlanetRegistry.Register(planet);

        var retrieved = PlanetRegistry.Get("test_planet");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Planet", retrieved!.Name);
    }

    [Fact]
    public void Registry_Get_NonExistent_ReturnsNull()
    {
        PlanetRegistry.Clear();
        var result = PlanetRegistry.Get("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void Registry_All_ReturnsAllRegistered()
    {
        PlanetRegistry.Clear();
        var p1 = CreateTestPlanet("planet_a", "Planet A", "SystemA");
        var p2 = CreateTestPlanet("planet_b", "Planet B", "SystemB");

        PlanetRegistry.Register(p1);
        PlanetRegistry.Register(p2);

        var all = PlanetRegistry.All;
        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.Id == "planet_a");
        Assert.Contains(all, p => p.Id == "planet_b");
    }

    [Fact]
    public void Registry_GetBySystem_ReturnsCorrectPlanets()
    {
        PlanetRegistry.Clear();
        var p1 = CreateTestPlanet("planet_a", "Planet A", "Sol");
        var p2 = CreateTestPlanet("planet_b", "Planet B", "Sol");
        var p3 = CreateTestPlanet("planet_c", "Planet C", "AlphaCentauri");

        PlanetRegistry.Register(p1);
        PlanetRegistry.Register(p2);
        PlanetRegistry.Register(p3);

        var solPlanets = PlanetRegistry.GetBySystem("Sol");
        Assert.Equal(2, solPlanets.Count);
        Assert.All(solPlanets, p => Assert.Equal("Sol", p.SystemName));

        var alphaPlanets = PlanetRegistry.GetBySystem("AlphaCentauri");
        Assert.Single(alphaPlanets);
        Assert.Equal("planet_c", alphaPlanets[0].Id);
    }

    [Fact]
    public void Registry_GetBySystem_EmptySystem_ReturnsEmptyList()
    {
        PlanetRegistry.Clear();
        var result = PlanetRegistry.GetBySystem("UnknownSystem");
        Assert.Empty(result);
    }

    [Fact]
    public void Registry_GetByEconomy_ReturnsCorrectPlanets()
    {
        PlanetRegistry.Clear();
        var industrial = CreateTestPlanet("ind", "Industrial World", "Sol", EconomyType.Industrial);
        var agri = CreateTestPlanet("agri", "Farm World", "Sol", EconomyType.Agricultural);
        var tech = CreateTestPlanet("tech", "Tech World", "Sol", EconomyType.HighTech);

        PlanetRegistry.Register(industrial);
        PlanetRegistry.Register(agri);
        PlanetRegistry.Register(tech);

        var industrialPlanets = PlanetRegistry.GetByEconomy(EconomyType.Industrial);
        Assert.Single(industrialPlanets);
        Assert.Equal("ind", industrialPlanets[0].Id);

        var agriPlanets = PlanetRegistry.GetByEconomy(EconomyType.Agricultural);
        Assert.Single(agriPlanets);
        Assert.Equal("agri", agriPlanets[0].Id);
    }

    [Fact]
    public void Registry_GetByEconomy_EmptyEconomy_ReturnsEmptyList()
    {
        PlanetRegistry.Clear();
        var result = PlanetRegistry.GetByEconomy(EconomyType.Criminal);
        Assert.Empty(result);
    }

    [Fact]
    public void Registry_GetByFaction_ReturnsCorrectPlanets()
    {
        PlanetRegistry.Clear();
        var p1 = CreateTestPlanet("p1", "Planet 1", "Sol", EconomyType.Balanced, "faction_a");
        var p2 = CreateTestPlanet("p2", "Planet 2", "Sol", EconomyType.Balanced, "faction_a");
        var p3 = CreateTestPlanet("p3", "Planet 3", "Sol", EconomyType.Balanced, "faction_b");

        PlanetRegistry.Register(p1);
        PlanetRegistry.Register(p2);
        PlanetRegistry.Register(p3);

        var factionAPlanets = PlanetRegistry.GetByFaction("faction_a").ToList();
        Assert.Equal(2, factionAPlanets.Count);
        Assert.All(factionAPlanets, p => Assert.Equal("faction_a", p.FactionId));

        var factionBPlanets = PlanetRegistry.GetByFaction("faction_b").ToList();
        Assert.Single(factionBPlanets);
        Assert.Equal("p3", factionBPlanets[0].Id);
    }

    [Fact]
    public void Registry_GetByFaction_NoMatch_ReturnsEmpty()
    {
        PlanetRegistry.Clear();
        var result = PlanetRegistry.GetByFaction("nonexistent_faction");
        Assert.Empty(result);
    }

    [Fact]
    public void Registry_Clear_RemovesAll()
    {
        PlanetRegistry.Clear();
        PlanetRegistry.Register(CreateTestPlanet());

        PlanetRegistry.Clear();

        Assert.Empty(PlanetRegistry.All);
        Assert.Null(PlanetRegistry.Get("test_planet"));
        Assert.Empty(PlanetRegistry.GetBySystem("Sol"));
        Assert.Empty(PlanetRegistry.GetByEconomy(EconomyType.Industrial));
    }

    [Fact]
    public void Registry_LoadFromJson_PopulatesRegistry()
    {
        PlanetRegistry.Clear();
        var json = @"[
            {
                ""id"": ""earth"",
                ""name"": ""Earth"",
                ""type"": ""Terrestrial"",
                ""systemName"": ""Sol"",
                ""orbitIndex"": 3,
                ""factionId"": ""terran_federation"",
                ""playerAlignment"": ""Friendly"",
                ""economyType"": ""Balanced"",
                ""techLevel"": 8,
                ""population"": 10000000000,
                ""securityLevel"": 7,
                ""travelDanger"": 5,
                ""hasShipyard"": true,
                ""hasOutfitter"": true,
                ""hasMissionBoard"": true,
                ""hasCommodityExchange"": true,
                ""hasBlackMarket"": false,
                ""isDiscovered"": true,
                ""isHomeBase"": true
            },
            {
                ""id"": ""mars"",
                ""name"": ""Mars"",
                ""type"": ""Mining"",
                ""systemName"": ""Sol"",
                ""orbitIndex"": 4,
                ""factionId"": ""terran_federation"",
                ""playerAlignment"": ""Neutral"",
                ""economyType"": ""Mining"",
                ""techLevel"": 6,
                ""population"": 5000000,
                ""securityLevel"": 5,
                ""travelDanger"": 10,
                ""hasShipyard"": false,
                ""hasOutfitter"": true,
                ""hasMissionBoard"": true,
                ""hasCommodityExchange"": true,
                ""hasBlackMarket"": true,
                ""isDiscovered"": true,
                ""isHomeBase"": false
            }
        ]";

        PlanetRegistry.LoadFromJson(json);

        Assert.Equal(2, PlanetRegistry.All.Count);

        var earth = PlanetRegistry.Get("earth");
        Assert.NotNull(earth);
        Assert.Equal("Earth", earth!.Name);
        Assert.Equal(PlanetType.Terrestrial, earth.Type);
        Assert.Equal("Sol", earth.SystemName);
        Assert.True(earth.IsHomeBase);

        var mars = PlanetRegistry.Get("mars");
        Assert.NotNull(mars);
        Assert.Equal("Mars", mars!.Name);
        Assert.Equal(PlanetType.Mining, mars.Type);
        Assert.Equal(EconomyType.Mining, mars.EconomyType);
        Assert.True(mars.HasBlackMarket);
    }

    [Fact]
    public void Registry_LoadFromJson_ClearsPreviousData()
    {
        PlanetRegistry.Clear();
        PlanetRegistry.Register(CreateTestPlanet());

        PlanetRegistry.LoadFromJson(@"[{""id"":""earth"",""name"":""Earth"",""type"":""Terrestrial"",""systemName"":""Sol"",""economyType"":""Balanced""}]");

        Assert.Single(PlanetRegistry.All);
        Assert.Null(PlanetRegistry.Get("test_planet"));
        Assert.NotNull(PlanetRegistry.Get("earth"));
    }

    [Fact]
    public void Registry_GetDiscovered_ReturnsOnlyDiscovered()
    {
        PlanetRegistry.Clear();
        var discovered = CreateTestPlanet("p1", "Discovered", "Sol");
        discovered.IsDiscovered = true;
        var undiscovered = CreateTestPlanet("p2", "Undiscovered", "Sol");
        undiscovered.IsDiscovered = false;

        PlanetRegistry.Register(discovered);
        PlanetRegistry.Register(undiscovered);

        var discoveredPlanets = PlanetRegistry.GetDiscovered().ToList();
        Assert.Single(discoveredPlanets);
        Assert.Equal("p1", discoveredPlanets[0].Id);
    }

    [Fact]
    public void Registry_GetHomeBase_ReturnsHomeBase()
    {
        PlanetRegistry.Clear();
        var home = CreateTestPlanet("home", "Home Base", "Sol");
        home.IsHomeBase = true;
        var notHome = CreateTestPlanet("other", "Other", "Sol");
        notHome.IsHomeBase = false;

        PlanetRegistry.Register(home);
        PlanetRegistry.Register(notHome);

        var homeBase = PlanetRegistry.GetHomeBase();
        Assert.NotNull(homeBase);
        Assert.Equal("home", homeBase!.Id);
    }

    [Fact]
    public void Registry_GetHomeBase_NoHomeBase_ReturnsNull()
    {
        PlanetRegistry.Clear();
        var planet = CreateTestPlanet();
        planet.IsHomeBase = false;
        PlanetRegistry.Register(planet);

        var homeBase = PlanetRegistry.GetHomeBase();
        Assert.Null(homeBase);
    }

    #endregion

    #region Helpers

    private static Planet CreateTestPlanet(
        string id = "test_planet",
        string name = "Test Planet",
        string systemName = "Sol",
        EconomyType economyType = EconomyType.Industrial,
        string factionId = "faction_test")
    {
        return new Planet
        {
            Id = id,
            Name = name,
            Type = PlanetType.Terrestrial,
            SystemName = systemName,
            OrbitIndex = 3,
            FactionId = factionId,
            PlayerAlignment = FactionAlignment.Neutral,
            EconomyType = economyType,
            TechLevel = 7,
            Population = 5_000_000,
            SecurityLevel = 6,
            TravelDanger = 15,
            HasShipyard = true,
            HasOutfitter = false,
            HasMissionBoard = true,
            HasCommodityExchange = true,
            HasBlackMarket = false,
            Description = "A test planet",
            ImageResource = "planet_icon",
            IsDiscovered = true,
            IsHomeBase = false,
            Market = new Market
            {
                MarketId = $"market_{id}",
                Name = $"{name} Market",
                EconomyType = economyType
            }
        };
    }

    #endregion
}
