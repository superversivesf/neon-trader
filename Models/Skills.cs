using System;
using System.Collections.Generic;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Models;

/// <summary>
/// Skill system - skill trees, XP, levels, perks for trading, combat, piloting, engineering
/// </summary>
public sealed class Skills : ISaveable
{
    /// <summary>
    /// Player's skill instances (skillId -> SkillInstance)
    /// </summary>
    public Dictionary<string, SkillInstance> SkillInstances { get; } = new();

    /// <summary>
    /// Total unspent skill points
    /// </summary>
    public int UnspentSkillPoints { get; set; } = 0;

    /// <summary>
    /// Total XP earned across all skills
    /// </summary>
    public long TotalXP { get; set; } = 0;

    /// <summary>
    /// Player's current level (based on total XP)
    /// </summary>
    public int PlayerLevel { get; set; } = 1;

    // ISaveable implementation
    public string SaveId => "skills";
    public int SaveVersion => 1;

    /// <summary>
    /// Serialize skills to JSON
    /// </summary>
    public JObject Serialize()
    {
        var skillsDict = new Dictionary<string, JObject>();
        foreach (var kvp in SkillInstances)
        {
            skillsDict[kvp.Key] = kvp.Value.Serialize();
        }

        return new JObject
        {
            ["skillInstances"] = JObject.FromObject(skillsDict),
            ["unspentSkillPoints"] = UnspentSkillPoints,
            ["totalXP"] = TotalXP,
            ["playerLevel"] = PlayerLevel
        };
    }

    /// <summary>
    /// Deserialize skills from JSON
    /// </summary>
    public void Deserialize(JObject data)
    {
        SkillInstances.Clear();
        if (data["skillInstances"] is JObject skillsObj)
        {
            foreach (var kvp in skillsObj)
            {
                if (kvp.Value is JObject skillObj)
                {
                    var instance = new SkillInstance();
                    instance.Deserialize(skillObj);
                    SkillInstances[kvp.Key] = instance;
                }
            }
        }

        UnspentSkillPoints = data["unspentSkillPoints"]?.ToObject<int>() ?? 0;
        TotalXP = data["totalXP"]?.ToObject<long>() ?? 0;
        PlayerLevel = data["playerLevel"]?.ToObject<int>() ?? 1;
    }

    /// <summary>
    /// Gets a skill instance, creating it if it doesn't exist
    /// </summary>
    public SkillInstance GetOrCreateSkill(string skillId)
    {
        if (!SkillInstances.TryGetValue(skillId, out var instance))
        {
            var definition = SkillRegistry.Get(skillId);
            if (definition != null)
            {
                instance = new SkillInstance { SkillId = skillId, Level = 0, CurrentXP = 0 };
                SkillInstances[skillId] = instance;
            }
        }
        return instance!;
    }

    /// <summary>
    /// Gets a skill instance
    /// </summary>
    public SkillInstance? GetSkill(string skillId)
    {
        SkillInstances.TryGetValue(skillId, out var instance);
        return instance;
    }

    /// <summary>
    /// Gets skill level (0 if not learned)
    /// </summary>
    public int GetSkillLevel(string skillId)
    {
        return SkillInstances.TryGetValue(skillId, out var instance) ? instance.Level : 0;
    }

    /// <summary>
    /// Adds XP to a skill
    /// </summary>
    public bool AddSkillXP(string skillId, int xpAmount)
    {
        var instance = GetOrCreateSkill(skillId);
        if (instance == null) return false;

        var definition = SkillRegistry.Get(skillId);
        if (definition == null) return false;

        instance.CurrentXP += xpAmount;
        TotalXP += xpAmount;

        // Check for level up
        var leveledUp = false;
        while (instance.Level < definition.MaxLevel)
        {
            var xpForNext = GetXPForLevel(instance.Level + 1, definition);
            if (instance.CurrentXP >= xpForNext)
            {
                instance.CurrentXP -= xpForNext;
                instance.Level++;
                leveledUp = true;
                
                // Grant skill point every few levels
                if (instance.Level % 2 == 0)
                {
                    UnspentSkillPoints++;
                }
            }
            else
            {
                break;
            }
        }

        // Update player level based on total XP
        UpdatePlayerLevel();

        return leveledUp;
    }

    /// <summary>
    /// Gets XP required for a specific level
    /// </summary>
    public static int GetXPForLevel(int level, SkillDefinition definition)
    {
        // Base XP formula: 100 * level^1.5
        var baseXP = (int)(100 * Math.Pow(level, 1.5));
        
        // Apply skill-specific multiplier
        return (int)(baseXP * definition.XPMultiplier);
    }

    /// <summary>
    /// Gets total XP required to reach a level
    /// </summary>
    public static int GetTotalXPForLevel(int level, SkillDefinition definition)
    {
        int total = 0;
        for (int i = 1; i <= level; i++)
        {
            total += GetXPForLevel(i, definition);
        }
        return total;
    }

    /// <summary>
    /// Gets XP progress towards next level (0.0 to 1.0)
    /// </summary>
    public float GetSkillProgress(string skillId)
    {
        var instance = GetSkill(skillId);
        if (instance == null) return 0f;

        var definition = SkillRegistry.Get(skillId);
        if (definition == null || instance.Level >= definition.MaxLevel) return 1f;

        var xpForNext = GetXPForLevel(instance.Level + 1, definition);
        return (float)instance.CurrentXP / xpForNext;
    }

    /// <summary>
    /// Spends a skill point to increase a skill level (for perk points)
    /// </summary>
    public bool SpendSkillPoint(string skillId)
    {
        if (UnspentSkillPoints <= 0) return false;

        var instance = GetOrCreateSkill(skillId);
        var definition = SkillRegistry.Get(skillId);
        
        if (definition == null || instance.Level >= definition.MaxLevel) return false;

        UnspentSkillPoints--;
        instance.Level++;
        
        UpdatePlayerLevel();
        return true;
    }

    /// <summary>
    /// Updates player level based on total XP
    /// </summary>
    private void UpdatePlayerLevel()
    {
        // Player level: every 1000 total XP = 1 level, with diminishing returns
        // Level 1: 0 XP, Level 2: 1000, Level 3: 2500, Level 4: 4500, etc.
        int newLevel = 1;
        long xpThreshold = 0;
        
        while (true)
        {
            var nextThreshold = xpThreshold + (1000 * newLevel);
            if (TotalXP >= nextThreshold)
            {
                xpThreshold = nextThreshold;
                newLevel++;
            }
            else
            {
                break;
            }
        }

        PlayerLevel = newLevel;
    }

    /// <summary>
    /// Gets the total bonus from a skill category
    /// </summary>
    public float GetCategoryBonus(SkillCategory category)
    {
        float totalBonus = 0f;
        
        foreach (var kvp in SkillInstances)
        {
            var definition = SkillRegistry.Get(kvp.Key);
            if (definition != null && definition.Category == category)
            {
                totalBonus += kvp.Value.GetTotalBonus();
            }
        }
        
        return totalBonus;
    }

    /// <summary>
    /// Gets a specific perk bonus by perk ID
    /// </summary>
    public float GetPerkBonus(string perkId)
    {
        foreach (var kvp in SkillInstances)
        {
            var definition = SkillRegistry.Get(kvp.Key);
            if (definition != null)
            {
                var perk = definition.Perks.FirstOrDefault(p => p.Id == perkId);
                if (perk != null && kvp.Value.Level >= perk.RequiredLevel)
                {
                    return perk.BonusValue;
                }
            }
        }
        return 0f;
    }

    /// <summary>
    /// Checks if a perk is unlocked
    /// </summary>
    public bool IsPerkUnlocked(string perkId)
    {
        foreach (var kvp in SkillInstances)
        {
            var definition = SkillRegistry.Get(kvp.Key);
            if (definition != null)
            {
                var perk = definition.Perks.FirstOrDefault(p => p.Id == perkId);
                if (perk != null && kvp.Value.Level >= perk.RequiredLevel)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all unlocked perks
    /// </summary>
    public List<SkillPerk> GetUnlockedPerks()
    {
        var perks = new List<SkillPerk>();
        
        foreach (var kvp in SkillInstances)
        {
            var definition = SkillRegistry.Get(kvp.Key);
            if (definition != null)
            {
                foreach (var perk in definition.Perks)
                {
                    if (kvp.Value.Level >= perk.RequiredLevel)
                    {
                        perks.Add(perk);
                    }
                }
            }
        }
        
        return perks;
    }

    /// <summary>
    /// Resets a skill (for respec)
    /// </summary>
    public void ResetSkill(string skillId)
    {
        if (SkillInstances.TryGetValue(skillId, out var instance))
        {
            // Return skill points (roughly: level / 2)
            UnspentSkillPoints += instance.Level / 2;
            
            // Subtract XP from total
            var definition = SkillRegistry.Get(skillId);
            if (definition != null)
            {
                TotalXP -= GetTotalXPForLevel(instance.Level, definition);
                TotalXP = Math.Max(0, TotalXP);
            }

            SkillInstances.Remove(skillId);
            UpdatePlayerLevel();
        }
    }

    /// <summary>
    /// Initializes skills for a new game
    /// </summary>
    public void InitializeNewGame()
    {
        SkillInstances.Clear();
        UnspentSkillPoints = 0;
        TotalXP = 0;
        PlayerLevel = 1;

        // Grant starting skills based on character creation choices
        // This would be called with specific starting skills
    }
}

/// <summary>
/// Individual skill instance for a player
/// </summary>
public sealed class SkillInstance : ISaveable
{
    public string SkillId { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
    public int CurrentXP { get; set; } = 0;
    public List<string> UnlockedPerks { get; } = new();

    public string SaveId => $"skill_{SkillId}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["skillId"] = SkillId,
            ["level"] = Level,
            ["currentXP"] = CurrentXP,
            ["unlockedPerks"] = JArray.FromObject(UnlockedPerks)
        };
    }

    public void Deserialize(JObject data)
    {
        SkillId = data["skillId"]?.ToString() ?? string.Empty;
        Level = data["level"]?.ToObject<int>() ?? 0;
        CurrentXP = data["currentXP"]?.ToObject<int>() ?? 0;

        UnlockedPerks.Clear();
        if (data["unlockedPerks"] is JArray perksArray)
        {
            foreach (var perk in perksArray)
            {
                var perkStr = perk?.ToString();
                if (!string.IsNullOrEmpty(perkStr))
                    UnlockedPerks.Add(perkStr);
            }
        }
    }

    /// <summary>
    /// Gets the total bonus value from this skill's level and perks
    /// </summary>
    public float GetTotalBonus()
    {
        var definition = SkillRegistry.Get(SkillId);
        if (definition == null) return 0f;

        float bonus = 0f;
        
        // Base bonus per level
        bonus += Level * definition.BaseBonusPerLevel;
        
        // Perk bonuses
        foreach (var perkId in UnlockedPerks)
        {
            var perk = definition.Perks.FirstOrDefault(p => p.Id == perkId);
            if (perk != null)
            {
                bonus += perk.BonusValue;
            }
        }
        
        return bonus;
    }
}

/// <summary>
/// Skill definition (from data file)
/// </summary>
public sealed class SkillDefinition : ISaveable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SkillCategory Category { get; set; } = SkillCategory.Trading;
    public int MaxLevel { get; set; } = 10;
    public float BaseBonusPerLevel { get; set; } = 0.02f; // 2% per level
    public float XPMultiplier { get; set; } = 1.0f;
    public List<SkillPerk> Perks { get; set; } = new();
    public List<string> PrerequisiteSkills { get; } = new();
    public string IconResource { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;

    public string SaveId => $"skill_def_{Id}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["description"] = Description,
            ["category"] = Category.ToString(),
            ["maxLevel"] = MaxLevel,
            ["baseBonusPerLevel"] = BaseBonusPerLevel,
            ["xpMultiplier"] = XPMultiplier,
            ["perks"] = JArray.FromObject(Perks),
            ["prerequisiteSkills"] = JArray.FromObject(PrerequisiteSkills),
            ["iconResource"] = IconResource,
            ["displayOrder"] = DisplayOrder
        };
    }

    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;
        Description = data["description"]?.ToString() ?? string.Empty;

        if (Enum.TryParse<SkillCategory>(data["category"]?.ToString(), out var cat))
            Category = cat;

        MaxLevel = data["maxLevel"]?.ToObject<int>() ?? 10;
        BaseBonusPerLevel = data["baseBonusPerLevel"]?.ToObject<float>() ?? 0.02f;
        XPMultiplier = data["xpMultiplier"]?.ToObject<float>() ?? 1.0f;

        Perks.Clear();
        if (data["perks"] is JArray perksArray)
        {
            Perks.AddRange(perksArray.ToObject<List<SkillPerk>>() ?? new());
        }

        PrerequisiteSkills.Clear();
        if (data["prerequisiteSkills"] is JArray prereqArray)
        {
            foreach (var prereq in prereqArray)
            {
                var prereqStr = prereq?.ToString();
                if (!string.IsNullOrEmpty(prereqStr))
                    PrerequisiteSkills.Add(prereqStr);
            }
        }

        IconResource = data["iconResource"]?.ToString() ?? string.Empty;
        DisplayOrder = data["displayOrder"]?.ToObject<int>() ?? 0;
    }

    /// <summary>
    /// Checks if player meets prerequisites for this skill
    /// </summary>
    public bool MeetsPrerequisites(Skills playerSkills)
    {
        foreach (var prereqId in PrerequisiteSkills)
        {
            if (playerSkills.GetSkillLevel(prereqId) <= 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the next perk to unlock
    /// </summary>
    public SkillPerk? GetNextPerk(int currentLevel)
    {
        return Perks
            .Where(p => p.RequiredLevel > currentLevel)
            .OrderBy(p => p.RequiredLevel)
            .FirstOrDefault();
    }
}

/// <summary>
/// Skill categories
/// </summary>
public enum SkillCategory
{
    Trading,      // Buy/sell prices, market info, cargo capacity
    Combat,       // Damage, accuracy, shields, weapons
    Piloting,     // Speed, maneuverability, fuel efficiency, jump range
    Engineering,  // Repair, upgrade efficiency, ship systems
    Exploration,  // Scan range, discovery rewards, navigation
    Leadership    // Crew efficiency, mission rewards, faction relations
}

/// <summary>
/// Skill perk definition
/// </summary>
public sealed class SkillPerk : ISaveable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RequiredLevel { get; set; } = 1;
    public float BonusValue { get; set; } = 0f;
    public PerkType Type { get; set; } = PerkType.FlatBonus;
    public string TargetStat { get; set; } = string.Empty; // What stat this affects

    public string SaveId => $"perk_{Id}";
    public int SaveVersion => 1;

    public JObject Serialize()
    {
        return new JObject
        {
            ["id"] = Id,
            ["name"] = Name,
            ["description"] = Description,
            ["requiredLevel"] = RequiredLevel,
            ["bonusValue"] = BonusValue,
            ["type"] = Type.ToString(),
            ["targetStat"] = TargetStat
        };
    }

    public void Deserialize(JObject data)
    {
        Id = data["id"]?.ToString() ?? string.Empty;
        Name = data["name"]?.ToString() ?? string.Empty;
        Description = data["description"]?.ToString() ?? string.Empty;
        RequiredLevel = data["requiredLevel"]?.ToObject<int>() ?? 1;
        BonusValue = data["bonusValue"]?.ToObject<float>() ?? 0f;

        if (Enum.TryParse<PerkType>(data["type"]?.ToString(), out var type))
            Type = type;

        TargetStat = data["targetStat"]?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Perk effect types
/// </summary>
public enum PerkType
{
    FlatBonus,        // Adds flat value
    PercentageBonus,  // Adds percentage multiplier
    UnlockFeature,    // Unlocks a game feature
    UnlockItem,       // Unlocks item/ship purchase
    SpecialAbility    // Grants active ability
}

/// <summary>
/// Static registry of all skill definitions
/// </summary>
public static class SkillRegistry
{
    private static readonly Dictionary<string, SkillDefinition> _skills = new();

    /// <summary>
    /// Gets a skill definition by ID
    /// </summary>
    public static SkillDefinition? Get(string id)
    {
        _skills.TryGetValue(id, out var skill);
        return skill;
    }

    /// <summary>
    /// Gets all skill definitions
    /// </summary>
    public static IReadOnlyCollection<SkillDefinition> All => _skills.Values;

    /// <summary>
    /// Gets skills by category
    /// </summary>
    public static IEnumerable<SkillDefinition> GetByCategory(SkillCategory category)
    {
        return _skills.Values.Where(s => s.Category == category).OrderBy(s => s.DisplayOrder);
    }

    /// <summary>
    /// Registers a skill definition
    /// </summary>
    public static void Register(SkillDefinition skill)
    {
        _skills[skill.Id] = skill;
    }

    /// <summary>
    /// Clears the registry
    /// </summary>
    public static void Clear()
    {
        _skills.Clear();
    }

    /// <summary>
    /// Loads skills from JSON data
    /// </summary>
    public static void LoadFromJson(string json)
    {
        Clear();
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var skill = new SkillDefinition();
            skill.Deserialize((JObject)item);
            Register(skill);
        }
    }

    /// <summary>
    /// Initializes default skills
    /// </summary>
    public static void InitializeDefaults()
    {
        // Trading Skills
        var haggling = new SkillDefinition
        {
            Id = "haggling",
            Name = "Haggling",
            Description = "Reduces purchase prices and increases sale prices at markets",
            Category = SkillCategory.Trading,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.02f, // 2% better prices per level
            XPMultiplier = 1.0f,
            DisplayOrder = 1,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "haggling_1", Name = "Sharp Eye", Description = "See market price trends", RequiredLevel = 2, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "price_trends" },
                new SkillPerk { Id = "haggling_2", Name = "Master Trader", Description = "10% better prices at all markets", RequiredLevel = 5, BonusValue = 0.1f, Type = PerkType.PercentageBonus, TargetStat = "price_modifier" },
                new SkillPerk { Id = "haggling_3", Name = "Trade Prince", Description = "Access to exclusive high-value trade contracts", RequiredLevel = 8, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "exclusive_contracts" }
            }
        };
        Register(haggling);

        var market_analysis = new SkillDefinition
        {
            Id = "market_analysis",
            Name = "Market Analysis",
            Description = "Better market data, predict price changes, identify profitable routes",
            Category = SkillCategory.Trading,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.015f,
            XPMultiplier = 1.1f,
            DisplayOrder = 2,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "market_analysis_1", Name = "Price History", Description = "View 7-day price history for commodities", RequiredLevel = 3, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "price_history" },
                new SkillPerk { Id = "market_analysis_2", Name = "Route Optimizer", Description = "Auto-calculate most profitable trade routes", RequiredLevel = 6, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "route_optimizer" }
            }
        };
        Register(market_analysis);

        var cargo_management = new SkillDefinition
        {
            Id = "cargo_management",
            Name = "Cargo Management",
            Description = "Increased cargo capacity, better organization, reduced mass penalty",
            Category = SkillCategory.Trading,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.03f, // 3% more cargo capacity per level
            XPMultiplier = 0.9f,
            DisplayOrder = 3,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "cargo_management_1", Name = "Efficient Packing", Description = "+10% cargo capacity", RequiredLevel = 4, BonusValue = 0.1f, Type = PerkType.PercentageBonus, TargetStat = "cargo_capacity" },
                new SkillPerk { Id = "cargo_management_2", Name = "Mass Optimization", Description = "Cargo mass reduced by 20%", RequiredLevel = 7, BonusValue = 0.2f, Type = PerkType.PercentageBonus, TargetStat = "cargo_mass_reduction" }
            }
        };
        Register(cargo_management);

        // Combat Skills
        var gunnery = new SkillDefinition
        {
            Id = "gunnery",
            Name = "Gunnery",
            Description = "Increased weapon damage, accuracy, and projectile speed",
            Category = SkillCategory.Combat,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.05f, // 5% damage per level
            XPMultiplier = 1.0f,
            DisplayOrder = 10,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "gunnery_1", Name = "Precision Targeting", Description = "+15% weapon accuracy", RequiredLevel = 3, BonusValue = 0.15f, Type = PerkType.PercentageBonus, TargetStat = "weapon_accuracy" },
                new SkillPerk { Id = "gunnery_2", Name = "Overcharge", Description = "Weapons deal 25% more damage but overheat 50% faster", RequiredLevel = 6, BonusValue = 0.25f, Type = PerkType.PercentageBonus, TargetStat = "weapon_damage" },
                new SkillPerk { Id = "gunnery_3", Name = "Devastating Salvo", Description = "Every 5th shot deals double damage", RequiredLevel = 9, BonusValue = 1f, Type = PerkType.SpecialAbility, TargetStat = "devastating_salvo" }
            }
        };
        Register(gunnery);

        var shields = new SkillDefinition
        {
            Id = "shields",
            Name = "Shield Systems",
            Description = "Stronger shields, faster recharge, better resistance",
            Category = SkillCategory.Combat,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.04f, // 4% shield capacity per level
            XPMultiplier = 0.9f,
            DisplayOrder = 11,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "shields_1", Name = "Fast Recharge", Description = "Shield recharge rate +50%", RequiredLevel = 3, BonusValue = 0.5f, Type = PerkType.PercentageBonus, TargetStat = "shield_recharge" },
                new SkillPerk { Id = "shields_2", Name = "Harmonics", Description = "Shields resist energy damage 30% better", RequiredLevel = 5, BonusValue = 0.3f, Type = PerkType.PercentageBonus, TargetStat = "energy_resistance" },
                new SkillPerk { Id = "shields_3", Name = "Reflective Plating", Description = "10% chance to reflect projectile damage", RequiredLevel = 8, BonusValue = 0.1f, Type = PerkType.PercentageBonus, TargetStat = "reflect_chance" }
            }
        };
        Register(shields);

        var missiles = new SkillDefinition
        {
            Id = "missiles",
            Name = "Missile Systems",
            Description = "Better missile tracking, damage, capacity, and countermeasures",
            Category = SkillCategory.Combat,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.05f,
            XPMultiplier = 1.1f,
            DisplayOrder = 12,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "missiles_1", Name = "Target Lock", Description = "Missile lock-on time reduced by 30%", RequiredLevel = 2, BonusValue = 0.3f, Type = PerkType.PercentageBonus, TargetStat = "lock_time" },
                new SkillPerk { Id = "missiles_2", Name = "Warhead Specialist", Description = "Missiles deal +30% damage", RequiredLevel = 5, BonusValue = 0.3f, Type = PerkType.PercentageBonus, TargetStat = "missile_damage" },
                new SkillPerk { Id = "missiles_3", Name = "Swarm Launch", Description = "Fire 2 missiles per launch", RequiredLevel = 8, BonusValue = 1f, Type = PerkType.SpecialAbility, TargetStat = "swarm_launch" }
            }
        };
        Register(missiles);

        // Piloting Skills
        var navigation = new SkillDefinition
        {
            Id = "navigation",
            Name = "Navigation",
            Description = "Increased jump range, reduced fuel consumption, better route planning",
            Category = SkillCategory.Piloting,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.03f, // 3% jump range per level
            XPMultiplier = 1.0f,
            DisplayOrder = 20,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "navigation_1", Name = "Fuel Efficiency", Description = "Jump fuel cost reduced by 15%", RequiredLevel = 3, BonusValue = 0.15f, Type = PerkType.PercentageBonus, TargetStat = "fuel_efficiency" },
                new SkillPerk { Id = "navigation_2", Name = "Warp Drive Tuning", Description = "Jump range increased by 20%", RequiredLevel = 6, BonusValue = 0.2f, Type = PerkType.PercentageBonus, TargetStat = "jump_range" },
                new SkillPerk { Id = "navigation_3", Name = "Quantum Slipstream", Description = "Chance to jump for free (no fuel)", RequiredLevel = 9, BonusValue = 0.1f, Type = PerkType.PercentageBonus, TargetStat = "free_jump_chance" }
            }
        };
        Register(navigation);

        var evasion = new SkillDefinition
        {
            Id = "evasion",
            Name = "Evasive Maneuvers",
            Description = "Better turning, speed boost, damage avoidance",
            Category = SkillCategory.Piloting,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.025f,
            XPMultiplier = 1.0f,
            DisplayOrder = 21,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "evasion_1", Name = "Afterburner Mastery", Description = "Afterburner duration +50%, cooldown -20%", RequiredLevel = 3, BonusValue = 0.5f, Type = PerkType.PercentageBonus, TargetStat = "afterburner_duration" },
                new SkillPerk { Id = "evasion_2", Name = "Jinking", Description = "15% chance to evade incoming fire", RequiredLevel = 5, BonusValue = 0.15f, Type = PerkType.PercentageBonus, TargetStat = "evasion_chance" },
                new SkillPerk { Id = "evasion_3", Name = "Phase Shift", Description = "Brief invulnerability on low health (once per combat)", RequiredLevel = 8, BonusValue = 1f, Type = PerkType.SpecialAbility, TargetStat = "phase_shift" }
            }
        };
        Register(evasion);

        // Engineering Skills
        var repair = new SkillDefinition
        {
            Id = "repair",
            Name = "Ship Repair",
            Description = "Faster repairs, cheaper maintenance, emergency repairs in combat",
            Category = SkillCategory.Engineering,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.05f, // 5% repair speed per level
            XPMultiplier = 0.8f,
            DisplayOrder = 30,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "repair_1", Name = "Field Repairs", Description = "Repair hull during combat (slow)", RequiredLevel = 4, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "combat_repair" },
                new SkillPerk { Id = "repair_2", Name = "Jury Rigging", Description = "Temporary repair boosts system performance", RequiredLevel = 7, BonusValue = 0.2f, Type = PerkType.PercentageBonus, TargetStat = "jury_rig_bonus" }
            }
        };
        Register(repair);

        var engineering = new SkillDefinition
        {
            Id = "systems_engineering",
            Name = "Systems Engineering",
            Description = "Upgrade efficiency, reduced power draw, overclock systems",
            Category = SkillCategory.Engineering,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.03f,
            XPMultiplier = 1.0f,
            DisplayOrder = 31,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "systems_engineering_1", Name = "Power Management", Description = "All systems use 10% less power", RequiredLevel = 3, BonusValue = 0.1f, Type = PerkType.PercentageBonus, TargetStat = "power_efficiency" },
                new SkillPerk { Id = "systems_engineering_2", Name = "Overclocking", Description = "Overclock one system for 50% performance (risk of failure)", RequiredLevel = 6, BonusValue = 0.5f, Type = PerkType.SpecialAbility, TargetStat = "overclock" }
            }
        };
        Register(engineering);

        // Exploration Skills
        var survey = new SkillDefinition
        {
            Id = "survey",
            Name = "Surveying",
            Description = "Better scanner range, more detailed scans, discover hidden resources",
            Category = SkillCategory.Exploration,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.04f,
            XPMultiplier = 1.0f,
            DisplayOrder = 40,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "survey_1", Name = "Deep Scan", Description = "Scanner range doubled", RequiredLevel = 3, BonusValue = 1f, Type = PerkType.PercentageBonus, TargetStat = "scan_range" },
                new SkillPerk { Id = "survey_2", Name = "Resource Detection", Description = "See mineral content of asteroids/planets", RequiredLevel = 5, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "resource_detection" }
            }
        };
        Register(survey);

        var archaeology = new SkillDefinition
        {
            Id = "archaeology",
            Name = "Xenoarchaeology",
            Description = "Find and analyze ancient artifacts, tech fragments, and ruins",
            Category = SkillCategory.Exploration,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.03f,
            XPMultiplier = 1.2f,
            DisplayOrder = 41,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "archaeology_1", Name = "Artifact Hunter", Description = "Artifacts found 3x more often", RequiredLevel = 4, BonusValue = 3f, Type = PerkType.FlatBonus, TargetStat = "artifact_find_rate" },
                new SkillPerk { Id = "archaeology_2", Name = "Tech Decoder", Description = "Decode alien tech fragments into blueprints", RequiredLevel = 7, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "tech_decoding" }
            }
        };
        Register(archaeology);

        // Leadership Skills
        var command = new SkillDefinition
        {
            Id = "command",
            Name = "Command",
            Description = "Crew efficiency, mission rewards, faction influence",
            Category = SkillCategory.Leadership,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.03f,
            XPMultiplier = 1.0f,
            DisplayOrder = 50,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "command_1", Name = "Inspiring Presence", Description = "Crew work 15% faster", RequiredLevel = 3, BonusValue = 0.15f, Type = PerkType.PercentageBonus, TargetStat = "crew_efficiency" },
                new SkillPerk { Id = "command_2", Name = "Diplomat", Description = "Faction reputation gains increased by 25%", RequiredLevel = 5, BonusValue = 0.25f, Type = PerkType.PercentageBonus, TargetStat = "rep_gain_bonus" },
                new SkillPerk { Id = "command_3", Name = "Fleet Commander", Description = "Can hire escort ships", RequiredLevel = 8, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "escort_ships" }
            }
        };
        Register(command);

        var persuasion = new SkillDefinition
        {
            Id = "persuasion",
            Name = "Persuasion",
            Description = "Better mission rewards, lower bribe costs, unique dialogue options",
            Category = SkillCategory.Leadership,
            MaxLevel = 10,
            BaseBonusPerLevel = 0.025f,
            XPMultiplier = 1.1f,
            DisplayOrder = 51,
            Perks = new List<SkillPerk>
            {
                new SkillPerk { Id = "persuasion_1", Name = "Silver Tongue", Description = "Mission rewards +20%", RequiredLevel = 3, BonusValue = 0.2f, Type = PerkType.PercentageBonus, TargetStat = "mission_reward_bonus" },
                new SkillPerk { Id = "persuasion_2", Name = "Connections", Description = "Access to restricted faction missions", RequiredLevel = 6, BonusValue = 1f, Type = PerkType.UnlockFeature, TargetStat = "restricted_missions" }
            }
        };
        Register(persuasion);
    }
}