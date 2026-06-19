using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Economy types that define what a location produces and consumes
/// </summary>
public enum EconomyType
{
    /// <summary>Produces ore, consumes tech and food</summary>
    Industrial,
    
    /// <summary>Produces organics/food, consumes tech and luxury</summary>
    Agricultural,
    
    /// <summary>Produces tech, consumes ore and organics</summary>
    HighTech,
    
    /// <summary>Produces luxury goods, consumes organics and ore</summary>
    Luxury,
    
    /// <summary>Produces weapons, consumes ore and tech</summary>
    Military,
    
    /// <summary>Produces medical supplies, consumes organics and tech</summary>
    Medical,
    
    /// <summary>Produces illegal goods, consumes luxury and weapons</summary>
    Criminal,
    
    /// <summary>Balanced production and consumption</summary>
    Balanced,
    
    /// <summary>Service economy, consumes everything, produces little</summary>
    Service,
    
    /// <summary>Mining colony, produces ore heavily</summary>
    Mining,
    
    /// <summary>Refining station, produces refined materials</summary>
    Refining,
    
    /// <summary>Research outpost, produces tech</summary>
    Research
}

/// <summary>
/// Represents a trade profile for an economy type
/// </summary>
public sealed class EconomyTradeProfile : ISaveable
{
    /// <summary>
    /// Economy type this profile belongs to
    /// </summary>
    public EconomyType EconomyType { get; set; }

    /// <summary>
    /// Production modifiers by commodity category (1.0 = normal, >1 = produces more, <1 = produces less)
    /// </summary>
    public Dictionary<CommodityCategory, double> ProductionModifiers { get; } = new();

    /// <summary>
    /// Consumption modifiers by commodity category (1.0 = normal, >1 = consumes more, <1 = consumes less)
    /// </summary>
    public Dictionary<CommodityCategory, double> ConsumptionModifiers { get; } = new();

    /// <summary>
    /// Base supply level for each category (0-100)
    /// </summary>
    public Dictionary<CommodityCategory, int> BaseSupply { get; } = new();

    /// <summary>
    /// Base demand level for each category (0-100)
    /// </summary>
    public Dictionary<CommodityCategory, int> BaseDemand { get; } = new();

    /// <summary>
    /// Price multiplier for buying (players sell to market)
    /// </summary>
    public Dictionary<CommodityCategory, double> BuyPriceMultiplier { get; } = new();

    /// <summary>
    /// Price multiplier for selling (players buy from market)
    /// </summary>
    public Dictionary<CommodityCategory, double> SellPriceMultiplier { get; } = new();

    /// <summary>
    /// Special commodities this economy produces (commodity ID -> production bonus)
    /// </summary>
    public Dictionary<string, double> SpecialProduction { get; } = new();

    /// <summary>
    /// Special commodities this economy consumes (commodity ID -> consumption bonus)
    /// </summary>
    public Dictionary<string, double> SpecialConsumption { get; } = new();

    /// <summary>
    /// Illegal commodity tolerance (0 = none, 1 = full black market)
    /// </summary>
    public double IllegalTolerance { get; set; } = 0.0;

    /// <summary>
    /// Market refresh rate modifier (1.0 = normal)
    /// </summary>
    public double RefreshRateModifier { get; set; } = 1.0;

    /// <summary>
    /// Market size modifier (affects volume)
    /// </summary>
    public double MarketSizeModifier { get; set; } = 1.0;

    // ISaveable implementation
    public string SaveId => $"economy_profile_{EconomyType}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        var prodMods = new JObject();
        foreach (var kvp in ProductionModifiers)
            prodMods[kvp.Key.ToString()] = kvp.Value;

        var consMods = new JObject();
        foreach (var kvp in ConsumptionModifiers)
            consMods[kvp.Key.ToString()] = kvp.Value;

        var baseSupply = new JObject();
        foreach (var kvp in BaseSupply)
            baseSupply[kvp.Key.ToString()] = kvp.Value;

        var baseDemand = new JObject();
        foreach (var kvp in BaseDemand)
            baseDemand[kvp.Key.ToString()] = kvp.Value;

        var buyMult = new JObject();
        foreach (var kvp in BuyPriceMultiplier)
            buyMult[kvp.Key.ToString()] = kvp.Value;

        var sellMult = new JObject();
        foreach (var kvp in SellPriceMultiplier)
            sellMult[kvp.Key.ToString()] = kvp.Value;

        return new JObject
        {
            ["economyType"] = EconomyType.ToString(),
            ["productionModifiers"] = prodMods,
            ["consumptionModifiers"] = consMods,
            ["baseSupply"] = baseSupply,
            ["baseDemand"] = baseDemand,
            ["buyPriceMultiplier"] = buyMult,
            ["sellPriceMultiplier"] = sellMult,
            ["specialProduction"] = JObject.FromObject(SpecialProduction),
            ["specialConsumption"] = JObject.FromObject(SpecialConsumption),
            ["illegalTolerance"] = IllegalTolerance,
            ["refreshRateModifier"] = RefreshRateModifier,
            ["marketSizeModifier"] = MarketSizeModifier
        };
    }

    public void Deserialize(JObject data)
    {
        if (Enum.TryParse<EconomyType>(data["economyType"]?.ToString(), out var econType))
            EconomyType = econType;

        ProductionModifiers.Clear();
        if (data["productionModifiers"] is JObject prodObj)
        {
            foreach (var kvp in prodObj)
            {
                if (Enum.TryParse<CommodityCategory>(kvp.Key, out var cat) && kvp.Value is JValue val)
                    ProductionModifiers[cat] = val.ToObject<double>();
            }
        }

        ConsumptionModifiers.Clear();
        if (data["consumptionModifiers"] is JObject consObj)
        {
            foreach (var kvp in consObj)
            {
                if (Enum.TryParse<CommodityCategory>(kvp.Key, out var cat) && kvp.Value is JValue val)
                    ConsumptionModifiers[cat] = val.ToObject<double>();
            }
        }

        BaseSupply.Clear();
        if (data["baseSupply"] is JObject supplyObj)
        {
            foreach (var kvp in supplyObj)
            {
                if (Enum.TryParse<CommodityCategory>(kvp.Key, out var cat) && kvp.Value is JValue val)
                    BaseSupply[cat] = val.ToObject<int>();
            }
        }

        BaseDemand.Clear();
        if (data["baseDemand"] is JObject demandObj)
        {
            foreach (var kvp in demandObj)
            {
                if (Enum.TryParse<CommodityCategory>(kvp.Key, out var cat) && kvp.Value is JValue val)
                    BaseDemand[cat] = val.ToObject<int>();
            }
        }

        BuyPriceMultiplier.Clear();
        if (data["buyPriceMultiplier"] is JObject buyObj)
        {
            foreach (var kvp in buyObj)
            {
                if (Enum.TryParse<CommodityCategory>(kvp.Key, out var cat) && kvp.Value is JValue val)
                    BuyPriceMultiplier[cat] = val.ToObject<double>();
            }
        }

        SellPriceMultiplier.Clear();
        if (data["sellPriceMultiplier"] is JObject sellObj)
        {
            foreach (var kvp in sellObj)
            {
                if (Enum.TryParse<CommodityCategory>(kvp.Key, out var cat) && kvp.Value is JValue val)
                    SellPriceMultiplier[cat] = val.ToObject<double>();
            }
        }

        SpecialProduction.Clear();
        if (data["specialProduction"] is JObject spObj)
        {
            foreach (var kvp in spObj)
            {
                if (kvp.Value is JValue val)
                    SpecialProduction[kvp.Key] = val.ToObject<double>();
            }
        }

        SpecialConsumption.Clear();
        if (data["specialConsumption"] is JObject scObj)
        {
            foreach (var kvp in scObj)
            {
                if (kvp.Value is JValue val)
                    SpecialConsumption[kvp.Key] = val.ToObject<double>();
            }
        }

        IllegalTolerance = data["illegalTolerance"]?.ToObject<double>() ?? 0.0;
        RefreshRateModifier = data["refreshRateModifier"]?.ToObject<double>() ?? 1.0;
        MarketSizeModifier = data["marketSizeModifier"]?.ToObject<double>() ?? 1.0;
    }
}

/// <summary>
/// Static registry of economy profiles
/// </summary>
public static class EconomyRegistry
{
    private static readonly Dictionary<EconomyType, EconomyTradeProfile> _profiles = new();

    static EconomyRegistry()
    {
        InitializeDefaultProfiles();
    }

    /// <summary>
    /// Gets the trade profile for an economy type
    /// </summary>
    public static EconomyTradeProfile GetProfile(EconomyType economyType)
    {
        if (_profiles.TryGetValue(economyType, out var profile))
            return profile;

        // Return balanced as fallback
        return _profiles[EconomyType.Balanced];
    }

    /// <summary>
    /// Gets all economy profiles
    /// </summary>
    public static IReadOnlyDictionary<EconomyType, EconomyTradeProfile> AllProfiles => _profiles;

    /// <summary>
    /// Registers a custom economy profile
    /// </summary>
    public static void RegisterProfile(EconomyTradeProfile profile)
    {
        _profiles[profile.EconomyType] = profile;
    }

    /// <summary>
    /// Initializes default economy profiles
    /// </summary>
    private static void InitializeDefaultProfiles()
    {
        // Industrial: Produces ore, consumes tech and organics
        var industrial = new EconomyTradeProfile { EconomyType = EconomyType.Industrial };
        industrial.ProductionModifiers[CommodityCategory.Ore] = 1.5;
        industrial.ProductionModifiers[CommodityCategory.Tech] = 0.8;
        industrial.ConsumptionModifiers[CommodityCategory.Tech] = 1.3;
        industrial.ConsumptionModifiers[CommodityCategory.Organics] = 1.2;
        industrial.ConsumptionModifiers[CommodityCategory.Luxury] = 0.8;
        industrial.BaseSupply[CommodityCategory.Ore] = 80;
        industrial.BaseSupply[CommodityCategory.Tech] = 30;
        industrial.BaseDemand[CommodityCategory.Tech] = 70;
        industrial.BaseDemand[CommodityCategory.Organics] = 60;
        industrial.BaseDemand[CommodityCategory.Luxury] = 40;
        industrial.BuyPriceMultiplier[CommodityCategory.Ore] = 0.9;  // Buy ore cheaper (they produce it)
        industrial.SellPriceMultiplier[CommodityCategory.Tech] = 1.2; // Sell tech higher (they need it)
        industrial.MarketSizeModifier = 1.2;
        RegisterProfile(industrial);

        // Agricultural: Produces organics, consumes tech and luxury
        var agricultural = new EconomyTradeProfile { EconomyType = EconomyType.Agricultural };
        agricultural.ProductionModifiers[CommodityCategory.Organics] = 1.5;
        agricultural.ConsumptionModifiers[CommodityCategory.Tech] = 1.3;
        agricultural.ConsumptionModifiers[CommodityCategory.Luxury] = 1.2;
        agricultural.ConsumptionModifiers[CommodityCategory.Medical] = 1.1;
        agricultural.BaseSupply[CommodityCategory.Organics] = 85;
        agricultural.BaseDemand[CommodityCategory.Tech] = 70;
        agricultural.BaseDemand[CommodityCategory.Luxury] = 60;
        agricultural.BaseDemand[CommodityCategory.Medical] = 50;
        agricultural.BuyPriceMultiplier[CommodityCategory.Organics] = 0.85;
        agricultural.SellPriceMultiplier[CommodityCategory.Tech] = 1.25;
        agricultural.SellPriceMultiplier[CommodityCategory.Luxury] = 1.2;
        agricultural.MarketSizeModifier = 1.0;
        RegisterProfile(agricultural);

        // HighTech: Produces tech, consumes ore and organics
        var highTech = new EconomyTradeProfile { EconomyType = EconomyType.HighTech };
        highTech.ProductionModifiers[CommodityCategory.Tech] = 1.6;
        highTech.ProductionModifiers[CommodityCategory.Medical] = 1.2;
        highTech.ConsumptionModifiers[CommodityCategory.Ore] = 1.4;
        highTech.ConsumptionModifiers[CommodityCategory.Organics] = 1.2;
        highTech.BaseSupply[CommodityCategory.Tech] = 80;
        highTech.BaseSupply[CommodityCategory.Medical] = 50;
        highTech.BaseDemand[CommodityCategory.Ore] = 75;
        highTech.BaseDemand[CommodityCategory.Organics] = 65;
        highTech.BuyPriceMultiplier[CommodityCategory.Tech] = 0.9;
        highTech.SellPriceMultiplier[CommodityCategory.Ore] = 1.3;
        highTech.MarketSizeModifier = 1.1;
        RegisterProfile(highTech);

        // Luxury: Produces luxury goods, consumes organics and ore
        var luxury = new EconomyTradeProfile { EconomyType = EconomyType.Luxury };
        luxury.ProductionModifiers[CommodityCategory.Luxury] = 1.5;
        luxury.ConsumptionModifiers[CommodityCategory.Organics] = 1.3;
        luxury.ConsumptionModifiers[CommodityCategory.Ore] = 1.2;
        luxury.ConsumptionModifiers[CommodityCategory.Medical] = 1.1;
        luxury.BaseSupply[CommodityCategory.Luxury] = 75;
        luxury.BaseDemand[CommodityCategory.Organics] = 70;
        luxury.BaseDemand[CommodityCategory.Ore] = 60;
        luxury.BaseDemand[CommodityCategory.Medical] = 50;
        luxury.BuyPriceMultiplier[CommodityCategory.Luxury] = 0.85;
        luxury.SellPriceMultiplier[CommodityCategory.Organics] = 1.2;
        luxury.SellPriceMultiplier[CommodityCategory.Ore] = 1.15;
        luxury.MarketSizeModifier = 0.9;
        RegisterProfile(luxury);

        // Military: Produces weapons, consumes ore and tech
        var military = new EconomyTradeProfile { EconomyType = EconomyType.Military };
        military.ProductionModifiers[CommodityCategory.Weapons] = 1.5;
        military.ProductionModifiers[CommodityCategory.Tech] = 1.1;
        military.ConsumptionModifiers[CommodityCategory.Ore] = 1.4;
        military.ConsumptionModifiers[CommodityCategory.Tech] = 1.2;
        military.ConsumptionModifiers[CommodityCategory.Medical] = 1.3;
        military.BaseSupply[CommodityCategory.Weapons] = 70;
        military.BaseSupply[CommodityCategory.Tech] = 50;
        military.BaseDemand[CommodityCategory.Ore] = 80;
        military.BaseDemand[CommodityCategory.Tech] = 65;
        military.BaseDemand[CommodityCategory.Medical] = 60;
        military.BuyPriceMultiplier[CommodityCategory.Weapons] = 0.9;
        military.SellPriceMultiplier[CommodityCategory.Ore] = 1.25;
        military.SellPriceMultiplier[CommodityCategory.Medical] = 1.2;
        military.MarketSizeModifier = 1.1;
        military.IllegalTolerance = 0.1;
        RegisterProfile(military);

        // Medical: Produces medical supplies, consumes organics and tech
        var medical = new EconomyTradeProfile { EconomyType = EconomyType.Medical };
        medical.ProductionModifiers[CommodityCategory.Medical] = 1.6;
        medical.ConsumptionModifiers[CommodityCategory.Organics] = 1.4;
        medical.ConsumptionModifiers[CommodityCategory.Tech] = 1.2;
        medical.ConsumptionModifiers[CommodityCategory.Luxury] = 1.1;
        medical.BaseSupply[CommodityCategory.Medical] = 80;
        medical.BaseDemand[CommodityCategory.Organics] = 75;
        medical.BaseDemand[CommodityCategory.Tech] = 65;
        medical.BaseDemand[CommodityCategory.Luxury] = 50;
        medical.BuyPriceMultiplier[CommodityCategory.Medical] = 0.85;
        medical.SellPriceMultiplier[CommodityCategory.Organics] = 1.25;
        medical.SellPriceMultiplier[CommodityCategory.Tech] = 1.2;
        medical.MarketSizeModifier = 0.9;
        RegisterProfile(medical);

        // Criminal: Produces illegal goods, consumes luxury and weapons
        var criminal = new EconomyTradeProfile { EconomyType = EconomyType.Criminal };
        criminal.ProductionModifiers[CommodityCategory.Illegal] = 1.5;
        criminal.ConsumptionModifiers[CommodityCategory.Luxury] = 1.4;
        criminal.ConsumptionModifiers[CommodityCategory.Weapons] = 1.3;
        criminal.ConsumptionModifiers[CommodityCategory.Tech] = 1.1;
        criminal.BaseSupply[CommodityCategory.Illegal] = 60;
        criminal.BaseDemand[CommodityCategory.Luxury] = 70;
        criminal.BaseDemand[CommodityCategory.Weapons] = 65;
        criminal.BaseDemand[CommodityCategory.Tech] = 50;
        criminal.BuyPriceMultiplier[CommodityCategory.Illegal] = 0.8;
        criminal.SellPriceMultiplier[CommodityCategory.Luxury] = 1.3;
        criminal.SellPriceMultiplier[CommodityCategory.Weapons] = 1.25;
        criminal.MarketSizeModifier = 0.8;
        criminal.IllegalTolerance = 1.0;
        RegisterProfile(criminal);

        // Balanced: Moderate everything
        var balanced = new EconomyTradeProfile { EconomyType = EconomyType.Balanced };
        foreach (CommodityCategory cat in Enum.GetValues<CommodityCategory>())
        {
            balanced.ProductionModifiers[cat] = 1.0;
            balanced.ConsumptionModifiers[cat] = 1.0;
            balanced.BaseSupply[cat] = 50;
            balanced.BaseDemand[cat] = 50;
            balanced.BuyPriceMultiplier[cat] = 1.0;
            balanced.SellPriceMultiplier[cat] = 1.0;
        }
        balanced.MarketSizeModifier = 1.0;
        RegisterProfile(balanced);

        // Service: Consumes everything, produces little
        var service = new EconomyTradeProfile { EconomyType = EconomyType.Service };
        foreach (CommodityCategory cat in Enum.GetValues<CommodityCategory>())
        {
            service.ProductionModifiers[cat] = 0.5;
            service.ConsumptionModifiers[cat] = 1.3;
            service.BaseSupply[cat] = 30;
            service.BaseDemand[cat] = 70;
            service.BuyPriceMultiplier[cat] = 1.1;  // Buy higher (need goods)
            service.SellPriceMultiplier[cat] = 0.9; // Sell lower (don't produce)
        }
        service.MarketSizeModifier = 0.9;
        RegisterProfile(service);

        // Mining: Heavy ore production
        var mining = new EconomyTradeProfile { EconomyType = EconomyType.Mining };
        mining.ProductionModifiers[CommodityCategory.Ore] = 2.0;
        mining.ConsumptionModifiers[CommodityCategory.Tech] = 1.2;
        mining.ConsumptionModifiers[CommodityCategory.Organics] = 1.3;
        mining.ConsumptionModifiers[CommodityCategory.Medical] = 1.2;
        mining.BaseSupply[CommodityCategory.Ore] = 95;
        mining.BaseDemand[CommodityCategory.Tech] = 65;
        mining.BaseDemand[CommodityCategory.Organics] = 70;
        mining.BaseDemand[CommodityCategory.Medical] = 55;
        mining.BuyPriceMultiplier[CommodityCategory.Ore] = 0.7;  // Very cheap ore
        mining.SellPriceMultiplier[CommodityCategory.Tech] = 1.3;
        mining.SellPriceMultiplier[CommodityCategory.Organics] = 1.25;
        mining.MarketSizeModifier = 1.3;
        RegisterProfile(mining);

        // Refining: Produces refined materials (ore -> tech/weapons)
        var refining = new EconomyTradeProfile { EconomyType = EconomyType.Refining };
        refining.ProductionModifiers[CommodityCategory.Tech] = 1.3;
        refining.ProductionModifiers[CommodityCategory.Weapons] = 1.2;
        refining.ConsumptionModifiers[CommodityCategory.Ore] = 1.8;
        refining.ConsumptionModifiers[CommodityCategory.Organics] = 1.1;
        refining.BaseSupply[CommodityCategory.Tech] = 70;
        refining.BaseSupply[CommodityCategory.Weapons] = 55;
        refining.BaseDemand[CommodityCategory.Ore] = 85;
        refining.BaseDemand[CommodityCategory.Organics] = 50;
        refining.BuyPriceMultiplier[CommodityCategory.Tech] = 0.9;
        refining.BuyPriceMultiplier[CommodityCategory.Weapons] = 0.95;
        refining.SellPriceMultiplier[CommodityCategory.Ore] = 1.2;
        refining.MarketSizeModifier = 1.1;
        RegisterProfile(refining);

        // Research: Produces tech heavily
        var research = new EconomyTradeProfile { EconomyType = EconomyType.Research };
        research.ProductionModifiers[CommodityCategory.Tech] = 1.8;
        research.ProductionModifiers[CommodityCategory.Medical] = 1.3;
        research.ConsumptionModifiers[CommodityCategory.Ore] = 1.3;
        research.ConsumptionModifiers[CommodityCategory.Organics] = 1.2;
        research.ConsumptionModifiers[CommodityCategory.Luxury] = 1.1;
        research.BaseSupply[CommodityCategory.Tech] = 85;
        research.BaseSupply[CommodityCategory.Medical] = 60;
        research.BaseDemand[CommodityCategory.Ore] = 70;
        research.BaseDemand[CommodityCategory.Organics] = 60;
        research.BaseDemand[CommodityCategory.Luxury] = 45;
        research.BuyPriceMultiplier[CommodityCategory.Tech] = 0.85;
        research.BuyPriceMultiplier[CommodityCategory.Medical] = 0.9;
        research.SellPriceMultiplier[CommodityCategory.Ore] = 1.25;
        research.MarketSizeModifier = 0.8;
        RegisterProfile(research);
    }
}