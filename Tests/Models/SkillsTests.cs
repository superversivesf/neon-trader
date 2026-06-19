using NeonTrader.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Models;

[Collection("Sequential")]
public class SkillsTests
{
    public SkillsTests()
    {
        SkillRegistry.InitializeDefaults();
    }

    [Fact]
    public void Skills_Serialize_Deserialize_RoundTrip()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 500);
        skills.AddSkillXP("gunnery", 300);
        skills.UnspentSkillPoints = 3;
        skills.TotalXP = 800;
        skills.PlayerLevel = 2;
        var json = skills.Serialize();
        var restored = new Skills();
        restored.Deserialize(json);
        Assert.Equal(3, restored.UnspentSkillPoints);
        Assert.Equal(800, restored.TotalXP);
        Assert.Equal(2, restored.PlayerLevel);
        Assert.True(restored.GetSkillLevel("haggling") > 0);
        Assert.True(restored.GetSkillLevel("gunnery") > 0);
    }

    [Fact]
    public void Skills_Serialize_Deserialize_EmptySkills()
    {
        var skills = new Skills();
        var json = skills.Serialize();
        var restored = new Skills();
        restored.Deserialize(json);
        Assert.Equal(0, restored.UnspentSkillPoints);
        Assert.Equal(0, restored.TotalXP);
        Assert.Equal(1, restored.PlayerLevel);
        Assert.Empty(restored.SkillInstances);
    }

    [Fact]
    public void SkillInstance_Serialize_Deserialize_RoundTrip()
    {
        var instance = new SkillInstance { SkillId = "haggling", Level = 5, CurrentXP = 200 };
        instance.UnlockedPerks.Add("haggling_1");
        instance.UnlockedPerks.Add("haggling_2");
        var json = instance.Serialize();
        var restored = new SkillInstance();
        restored.Deserialize(json);
        Assert.Equal("haggling", restored.SkillId);
        Assert.Equal(5, restored.Level);
        Assert.Equal(200, restored.CurrentXP);
        Assert.Equal(2, restored.UnlockedPerks.Count);
        Assert.Contains("haggling_1", restored.UnlockedPerks);
        Assert.Contains("haggling_2", restored.UnlockedPerks);
    }

    [Fact]
    public void SkillDefinition_Serialize_Deserialize_RoundTrip()
    {
        var def = new SkillDefinition
        {
            Id = "test_skill", Name = "Test Skill", Description = "A test skill",
            Category = SkillCategory.Combat, MaxLevel = 15,
            BaseBonusPerLevel = 0.05f, XPMultiplier = 1.2f,
            DisplayOrder = 99, IconResource = "icon_test"
        };
        def.PrerequisiteSkills.Add("gunnery");
        def.Perks.Add(new SkillPerk { Id = "test_perk_1", Name = "Test Perk", Description = "A test perk", RequiredLevel = 3, BonusValue = 0.15f, Type = PerkType.PercentageBonus, TargetStat = "test_stat" });
        var json = def.Serialize();
        var restored = new SkillDefinition();
        restored.Deserialize(json);
        Assert.Equal("test_skill", restored.Id);
        Assert.Equal("Test Skill", restored.Name);
        Assert.Equal(SkillCategory.Combat, restored.Category);
        Assert.Equal(15, restored.MaxLevel);
        Assert.Equal(0.05f, restored.BaseBonusPerLevel);
        Assert.Equal(1.2f, restored.XPMultiplier);
        Assert.Equal(99, restored.DisplayOrder);
        Assert.Single(restored.PrerequisiteSkills);
        Assert.Contains("gunnery", restored.PrerequisiteSkills);
        Assert.Single(restored.Perks);
        Assert.Equal("test_perk_1", restored.Perks[0].Id);
    }

    [Fact]
    public void SkillPerk_Serialize_Deserialize_RoundTrip()
    {
        var perk = new SkillPerk { Id = "perk_test", Name = "Test Perk", Description = "A test perk", RequiredLevel = 5, BonusValue = 0.25f, Type = PerkType.SpecialAbility, TargetStat = "special_ability" };
        var json = perk.Serialize();
        var restored = new SkillPerk();
        restored.Deserialize(json);
        Assert.Equal("perk_test", restored.Id);
        Assert.Equal("Test Perk", restored.Name);
        Assert.Equal(5, restored.RequiredLevel);
        Assert.Equal(0.25f, restored.BonusValue);
        Assert.Equal(PerkType.SpecialAbility, restored.Type);
        Assert.Equal("special_ability", restored.TargetStat);
    }

    [Fact]
    public void AddSkillXP_NewSkill_CreatesInstanceAndAddsXP()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 100);
        Assert.True(skills.GetSkillLevel("haggling") >= 0);
        Assert.True(skills.TotalXP >= 100);
    }

    [Fact]
    public void AddSkillXP_LevelsUp_WhenThresholdReached()
    {
        var skills = new Skills();
        var leveledUp = skills.AddSkillXP("haggling", 100);
        Assert.True(leveledUp);
        Assert.Equal(1, skills.GetSkillLevel("haggling"));
    }

    [Fact]
    public void AddSkillXP_MultipleLevels_AccumulatesCorrectly()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        Assert.Equal(2, skills.GetSkillLevel("haggling"));
    }

    [Fact]
    public void AddSkillXP_GrantsSkillPoints_EveryTwoLevels()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        Assert.Equal(1, skills.UnspentSkillPoints);
    }

    [Fact]
    public void AddSkillXP_UpdatesPlayerLevel()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 1000);
        Assert.Equal(2, skills.PlayerLevel);
    }

    [Fact]
    public void AddSkillXP_NonexistentSkill_ReturnsFalse()
    {
        var skills = new Skills();
        var result = skills.AddSkillXP("nonexistent_skill", 100);
        Assert.False(result);
    }

    [Fact]
    public void AddSkillXP_MaxLevel_DoesNotExceed()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 100000);
        Assert.True(skills.GetSkillLevel("haggling") <= 10);
    }

    [Fact]
    public void GetXPForLevel_ReturnsCorrectBaseValues()
    {
        var def = SkillRegistry.Get("haggling")!;
        Assert.Equal(100, Skills.GetXPForLevel(1, def));
        Assert.Equal(282, Skills.GetXPForLevel(2, def));
        Assert.Equal(1118, Skills.GetXPForLevel(5, def));
    }

    [Fact]
    public void GetXPForLevel_AppliesMultiplier()
    {
        var def = SkillRegistry.Get("market_analysis")!;
        var baseXP = (int)(100 * Math.Pow(1, 1.5));
        var expected = (int)(baseXP * 1.1);
        Assert.Equal(expected, Skills.GetXPForLevel(1, def));
    }

    [Fact]
    public void GetTotalXPForLevel_ReturnsCumulativeXP()
    {
        var def = SkillRegistry.Get("haggling")!;
        var total = Skills.GetTotalXPForLevel(3, def);
        Assert.Equal(100 + 282 + 519, total);
    }

    [Fact]
    public void GetSkillProgress_NewSkill_ReturnsZero()
    {
        var skills = new Skills();
        Assert.Equal(0f, skills.GetSkillProgress("haggling"));
    }

    [Fact]
    public void GetSkillProgress_PartialProgress_ReturnsFraction()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 50);
        var progress = skills.GetSkillProgress("haggling");
        Assert.True(progress > 0f && progress < 1f);
    }

    [Fact]
    public void GetSkillProgress_MaxLevel_ReturnsOne()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 100000);
        Assert.Equal(1f, skills.GetSkillProgress("haggling"));
    }

    [Fact]
    public void GetSkillProgress_NonexistentSkill_ReturnsZero()
    {
        var skills = new Skills();
        Assert.Equal(0f, skills.GetSkillProgress("nonexistent"));
    }

    [Fact]
    public void SpendSkillPoint_WithPoints_IncreasesLevel()
    {
        var skills = new Skills();
        skills.UnspentSkillPoints = 2;
        skills.AddSkillXP("haggling", 100);
        var result = skills.SpendSkillPoint("haggling");
        Assert.True(result);
        Assert.Equal(2, skills.GetSkillLevel("haggling"));
        Assert.Equal(1, skills.UnspentSkillPoints);
    }

    [Fact]
    public void SpendSkillPoint_NoPoints_ReturnsFalse()
    {
        var skills = new Skills();
        skills.UnspentSkillPoints = 0;
        skills.AddSkillXP("haggling", 100);
        var result = skills.SpendSkillPoint("haggling");
        Assert.False(result);
        Assert.Equal(1, skills.GetSkillLevel("haggling"));
    }

    [Fact]
    public void SpendSkillPoint_MaxLevel_ReturnsFalse()
    {
        var skills = new Skills();
        skills.UnspentSkillPoints = 5;
        skills.AddSkillXP("haggling", 100000);
        var result = skills.SpendSkillPoint("haggling");
        Assert.False(result);
    }

    [Fact]
    public void GetOrCreateSkill_CreatesNewInstance()
    {
        var skills = new Skills();
        var instance = skills.GetOrCreateSkill("haggling");
        Assert.NotNull(instance);
        Assert.Equal("haggling", instance.SkillId);
        Assert.Equal(0, instance.Level);
    }

    [Fact]
    public void GetOrCreateSkill_NonexistentSkill_ReturnsNull()
    {
        var skills = new Skills();
        Assert.Null(skills.GetOrCreateSkill("nonexistent"));
    }

    [Fact]
    public void GetSkill_ExistingSkill_ReturnsInstance()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 100);
        var instance = skills.GetSkill("haggling");
        Assert.NotNull(instance);
        Assert.Equal("haggling", instance!.SkillId);
    }

    [Fact]
    public void GetSkill_NonexistentSkill_ReturnsNull()
    {
        var skills = new Skills();
        Assert.Null(skills.GetSkill("nonexistent"));
    }

    [Fact]
    public void GetSkillLevel_ExistingSkill_ReturnsLevel()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        Assert.Equal(2, skills.GetSkillLevel("haggling"));
    }

    [Fact]
    public void GetSkillLevel_NonexistentSkill_ReturnsZero()
    {
        var skills = new Skills();
        Assert.Equal(0, skills.GetSkillLevel("nonexistent"));
    }

    [Fact]
    public void GetCategoryBonus_TradingSkills_ReturnsSum()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        skills.AddSkillXP("market_analysis", 100);
        var bonus = skills.GetCategoryBonus(SkillCategory.Trading);
        Assert.True(bonus > 0f);
    }

    [Fact]
    public void GetCategoryBonus_EmptyCategory_ReturnsZero()
    {
        var skills = new Skills();
        Assert.Equal(0f, skills.GetCategoryBonus(SkillCategory.Leadership));
    }

    [Fact]
    public void GetPerkBonus_UnlockedPerk_ReturnsBonusValue()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        Assert.Equal(1f, skills.GetPerkBonus("haggling_1"));
    }

    [Fact]
    public void GetPerkBonus_LockedPerk_ReturnsZero()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 100);
        Assert.Equal(0f, skills.GetPerkBonus("haggling_1"));
    }

    [Fact]
    public void GetPerkBonus_NonexistentPerk_ReturnsZero()
    {
        var skills = new Skills();
        Assert.Equal(0f, skills.GetPerkBonus("nonexistent_perk"));
    }

    [Fact]
    public void IsPerkUnlocked_UnlockedPerk_ReturnsTrue()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        Assert.True(skills.IsPerkUnlocked("haggling_1"));
    }

    [Fact]
    public void IsPerkUnlocked_LockedPerk_ReturnsFalse()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 100);
        Assert.False(skills.IsPerkUnlocked("haggling_1"));
    }

    [Fact]
    public void GetUnlockedPerks_ReturnsAllUnlocked()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        var perks = skills.GetUnlockedPerks();
        Assert.Contains(perks, p => p.Id == "haggling_1");
    }

    [Fact]
    public void GetUnlockedPerks_NoSkills_ReturnsEmpty()
    {
        var skills = new Skills();
        Assert.Empty(skills.GetUnlockedPerks());
    }

    [Fact]
    public void ResetSkill_RemovesSkillAndReturnsPoints()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 400);
        var pointsBefore = skills.UnspentSkillPoints;
        skills.ResetSkill("haggling");
        Assert.Equal(0, skills.GetSkillLevel("haggling"));
        Assert.True(skills.UnspentSkillPoints >= pointsBefore);
    }

    [Fact]
    public void ResetSkill_NonexistentSkill_DoesNothing()
    {
        var skills = new Skills();
        var pointsBefore = skills.UnspentSkillPoints;
        skills.ResetSkill("nonexistent");
        Assert.Equal(pointsBefore, skills.UnspentSkillPoints);
    }

    [Fact]
    public void InitializeNewGame_ClearsAllSkills()
    {
        var skills = new Skills();
        skills.AddSkillXP("haggling", 500);
        skills.UnspentSkillPoints = 5;
        skills.InitializeNewGame();
        Assert.Equal(0, skills.UnspentSkillPoints);
        Assert.Equal(0, skills.TotalXP);
        Assert.Equal(1, skills.PlayerLevel);
        Assert.Empty(skills.SkillInstances);
    }

    [Fact]
    public void MeetsPrerequisites_AllMet_ReturnsTrue()
    {
        var skills = new Skills();
        skills.AddSkillXP("gunnery", 100);
        var def = new SkillDefinition { Id = "advanced_combat", PrerequisiteSkills = { "gunnery" } };
        Assert.True(def.MeetsPrerequisites(skills));
    }

    [Fact]
    public void MeetsPrerequisites_NotMet_ReturnsFalse()
    {
        var skills = new Skills();
        var def = new SkillDefinition { Id = "advanced_combat", PrerequisiteSkills = { "gunnery" } };
        Assert.False(def.MeetsPrerequisites(skills));
    }

    [Fact]
    public void MeetsPrerequisites_NoPrerequisites_ReturnsTrue()
    {
        var skills = new Skills();
        var def = new SkillDefinition { Id = "basic_skill" };
        Assert.True(def.MeetsPrerequisites(skills));
    }

    [Fact]
    public void GetNextPerk_ReturnsNextUnlockablePerk()
    {
        var def = SkillRegistry.Get("haggling")!;
        var nextPerk = def.GetNextPerk(0);
        Assert.NotNull(nextPerk);
        Assert.Equal("haggling_1", nextPerk!.Id);
    }

    [Fact]
    public void GetNextPerk_MaxLevel_ReturnsNull()
    {
        var def = SkillRegistry.Get("haggling")!;
        Assert.Null(def.GetNextPerk(10));
    }

    [Fact]
    public void SkillRegistry_Get_ReturnsDefinition()
    {
        var def = SkillRegistry.Get("haggling");
        Assert.NotNull(def);
        Assert.Equal("haggling", def!.Id);
        Assert.Equal("Haggling", def.Name);
    }

    [Fact]
    public void SkillRegistry_Get_Nonexistent_ReturnsNull()
    {
        Assert.Null(SkillRegistry.Get("nonexistent"));
    }

    [Fact]
    public void SkillRegistry_All_ReturnsAllSkills()
    {
        SkillRegistry.InitializeDefaults();
        var all = SkillRegistry.All;
        Assert.NotEmpty(all);
        Assert.Contains(all, s => s.Id == "haggling");
        Assert.Contains(all, s => s.Id == "gunnery");
        Assert.Contains(all, s => s.Id == "navigation");
    }

    [Fact]
    public void SkillRegistry_GetByCategory_ReturnsCorrectSkills()
    {
        var trading = SkillRegistry.GetByCategory(SkillCategory.Trading);
        Assert.All(trading, s => Assert.Equal(SkillCategory.Trading, s.Category));
        var combat = SkillRegistry.GetByCategory(SkillCategory.Combat);
        Assert.All(combat, s => Assert.Equal(SkillCategory.Combat, s.Category));
    }

    [Fact]
    public void SkillRegistry_Register_AddsSkill()
    {
        var def = new SkillDefinition { Id = "custom_skill", Name = "Custom Skill", Category = SkillCategory.Exploration };
        SkillRegistry.Register(def);
        var retrieved = SkillRegistry.Get("custom_skill");
        Assert.NotNull(retrieved);
        Assert.Equal("Custom Skill", retrieved!.Name);
    }

    [Fact]
    public void SkillRegistry_Clear_RemovesAllSkills()
    {
        SkillRegistry.Clear();
        Assert.Empty(SkillRegistry.All);
        SkillRegistry.InitializeDefaults();
    }

    [Fact]
    public void SkillRegistry_LoadFromJson_PopulatesRegistry()
    {
        var json = @"[{""id"":""json_skill"",""name"":""JSON Skill"",""description"":""Loaded from JSON"",""category"":""Trading"",""maxLevel"":5,""baseBonusPerLevel"":0.01,""xpMultiplier"":1.0,""perks"":[],""prerequisiteSkills"":[],""iconResource"":"""",""displayOrder"":0}]";
        SkillRegistry.LoadFromJson(json);
        var def = SkillRegistry.Get("json_skill");
        Assert.NotNull(def);
        Assert.Equal("JSON Skill", def!.Name);
        Assert.Equal(SkillCategory.Trading, def.Category);
        Assert.Equal(5, def.MaxLevel);
        SkillRegistry.InitializeDefaults();
    }

    [Fact]
    public void GetTotalBonus_ReturnsLevelBonusPlusPerkBonuses()
    {
        var instance = new SkillInstance { SkillId = "haggling", Level = 3 };
        instance.UnlockedPerks.Add("haggling_1");
        var bonus = instance.GetTotalBonus();
        Assert.True(bonus > 0f);
    }

    [Fact]
    public void GetTotalBonus_NoSkillDefinition_ReturnsZero()
    {
        var instance = new SkillInstance { SkillId = "nonexistent", Level = 5 };
        Assert.Equal(0f, instance.GetTotalBonus());
    }

    [Fact]
    public void Skills_SaveId_IsCorrect() => Assert.Equal("skills", new Skills().SaveId);
    [Fact]
    public void SkillInstance_SaveId_IsCorrect() => Assert.Equal("skill_haggling", new SkillInstance { SkillId = "haggling" }.SaveId);
    [Fact]
    public void SkillDefinition_SaveId_IsCorrect() => Assert.Equal("skill_def_test", new SkillDefinition { Id = "test" }.SaveId);
    [Fact]
    public void SkillPerk_SaveId_IsCorrect() => Assert.Equal("perk_perk_1", new SkillPerk { Id = "perk_1" }.SaveId);
}
