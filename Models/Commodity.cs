using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Commodity categories for classification and economy interactions
/// </summary>
public enum CommodityCategory
{
    Ore,
    Organics,
    Tech,
    Luxury,
    Weapons,
    Medical,
    Illegal
}

/// <summary>
/// Legality status of a commodity
/// </summary>
public enum CommodityLegality
{
    Legal,
    Restricted,
    Illegal
}

/// <summary>
/// Represents a tradable commodity in the game
/// </summary>
public sealed class Commodity : ISaveable
{
    /// <summary>
    /// Unique identifier for the commodity
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the commodity
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category classification for economy interactions
    /// </summary>
    public CommodityCategory Category { get; set; } = CommodityCategory.Ore;

    /// <summary>
    /// Base price in credits per unit
    /// </summary>
    public decimal BasePrice { get; set; } = 100m;

    /// <summary>
    /// Price volatility factor (0.0 - 1.0). Higher = more price variation
    /// </summary>
    public double Volatility { get; set; } = 0.1;

    /// <summary>
    /// Legality status affecting where it can be traded
    /// </summary>
    public CommodityLegality Legality { get; set; } = CommodityLegality.Legal;

    /// <summary>
    /// Base trade volume available at markets
    /// </summary>
    public int BaseVolume { get; set; } = 100;

    /// <summary>
    /// Mass per unit in tons (for cargo capacity calculations)
    /// </summary>
    public double MassPerUnit { get; set; } = 1.0;

    /// <summary>
    /// Description for UI display
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tags for special commodity properties (e.g., "perishable", "radioactive")
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    /// <summary>
    /// Minimum price floor (prevents prices from crashing)
    /// </summary>
    public decimal MinPrice { get; set; } = 10m;

    /// <summary>
    /// Maximum price ceiling (prevents infinite inflation)
    /// </summary>
    public decimal MaxPrice { get; set; } = 10000m;

    // ISaveable implementation
    public string SaveId => $"commodity_{Id}";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize the commodity to JSON
    /// </summary>
    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["category"] = Category.ToString(),
            ["basePrice"] = BasePrice,
            ["volatility"] = Volatility,
            ["legality"] = Legality.ToString(),
            ["baseVolume"] = BaseVolume,
            ["massPerUnit"] = MassPerUnit,
            ["description"] = Description,
            ["tags"] = JArray.FromObject(Tags),
            ["minPrice"] = MinPrice,
            ["maxPrice"] = MaxPrice
        };
    }

    /// <summary>
    /// Deserialize the commodity from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;
        
        if (Enum.TryParse<CommodityCategory>(data["category"]?.ToString(), out var category))
            Category = category;
        
        BasePrice = data["basePrice"]?.ToObject<decimal>() ?? 100m;
        Volatility = data["volatility"]?.ToObject<double>() ?? 0.1;
        
        if (Enum.TryParse<CommodityLegality>(data["legality"]?.ToString(), out var legality))
            Legality = legality;
        
        BaseVolume = data["baseVolume"]?.ToObject<int>() ?? 100;
        MassPerUnit = data["massPerUnit"]?.ToObject<double>() ?? 1.0;
        Description = data["description"]?.ToString() ?? string.Empty;
        MinPrice = data["minPrice"]?.ToObject<decimal>() ?? 10m;
        MaxPrice = data["maxPrice"]?.ToObject<decimal>() ?? 10000m;

        Tags.Clear();
        if (data["tags"] is JArray tagsArray)
        {
            foreach (var tag in tagsArray)
            {
                var tagStr = tag?.ToString();
                if (!string.IsNullOrEmpty(tagStr))
                    Tags.Add(tagStr);
            }
        }
    }

    /// <summary>
    /// Creates a copy of this commodity
    /// </summary>
    public Commodity Clone()
    {
        var clone = new Commodity
        {
            Id = Id,
            Name = Name,
            Category = Category,
            BasePrice = BasePrice,
            Volatility = Volatility,
            Legality = Legality,
            BaseVolume = BaseVolume,
            MassPerUnit = MassPerUnit,
            Description = Description,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice
        };
        
        foreach (var tag in Tags)
            clone.Tags.Add(tag);
        
        return clone;
    }

    /// <summary>
    /// Validates the commodity data
    /// </summary>
    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "Commodity ID cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Commodity name cannot be empty";
            return false;
        }

        if (BasePrice <= 0)
        {
            error = "Base price must be positive";
            return false;
        }

        if (Volatility < 0 || Volatility > 1)
        {
            error = "Volatility must be between 0 and 1";
            return false;
        }

        if (BaseVolume < 0)
        {
            error = "Base volume cannot be negative";
            return false;
        }

        if (MassPerUnit <= 0)
        {
            error = "Mass per unit must be positive";
            return false;
        }

        if (MinPrice <= 0 || MaxPrice <= 0 || MinPrice >= MaxPrice)
        {
            error = "Min price must be positive and less than max price";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

/// <summary>
/// Static class containing all commodity definitions loaded from data files
/// </summary>
public static class CommodityRegistry
{
    private static readonly Dictionary<string, Commodity> _commodities = new();
    private static readonly Dictionary<CommodityCategory, List<Commodity>> _byCategory = new();

    /// <summary>
    /// Gets a commodity by ID
    /// </summary>
    public static Commodity? Get(string id)
    {
        _commodities.TryGetValue(id, out var commodity);
        return commodity;
    }

    /// <summary>
    /// Gets all commodities
    /// </summary>
    public static IReadOnlyCollection<Commodity> All => _commodities.Values;

    /// <summary>
    /// Gets commodities by category
    /// </summary>
    public static IReadOnlyList<Commodity> GetByCategory(CommodityCategory category)
    {
        _byCategory.TryGetValue(category, out var list);
        return list ?? new List<Commodity>();
    }

    /// <summary>
    /// Registers a commodity
    /// </summary>
    public static void Register(Commodity commodity)
    {
        if (commodity.Validate(out var error))
        {
            _commodities[commodity.Id] = commodity;
            
            if (!_byCategory.ContainsKey(commodity.Category))
                _byCategory[commodity.Category] = new List<Commodity>();
            
            _byCategory[commodity.Category].Add(commodity);
        }
        else
        {
            throw new ArgumentException($"Invalid commodity: {error}");
        }
    }

    /// <summary>
    /// Clears the registry (for testing or reload)
    /// </summary>
    public static void Clear()
    {
        _commodities.Clear();
        _byCategory.Clear();
    }

    /// <summary>
    /// Loads commodities from JSON data
    /// </summary>
    public static void LoadFromJson(string json)
    {
        Clear();
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var commodity = new Commodity();
            commodity.Deserialize((JObject)item);
            Register(commodity);
        }
    }
}