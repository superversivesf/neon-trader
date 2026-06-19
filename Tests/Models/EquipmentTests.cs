using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

[Collection("Sequential")]
public class EquipmentTests
{
    private Weapon CreateTestEquipment(string id = "eq_test", string name = "Test Equipment")
    {
        return new Weapon
        {
            Id = id, Name = name, Type = EquipmentType.Utility, Size = EquipmentSize.Small,
            MountType = MountType.Internal, Rarity = EquipmentRarity.Common, Manufacturer = "TestCorp",
            Description = "Test equipment item", BasePrice = 1000, Mass = 1.0, PowerDraw = 5,
            HeatGeneration = 2, MinimumShipSize = ShipSize.Tiny, RequiredReputationTier = 0,
            RequiredFaction = "", Condition = 1.0, Damage = 10, Range = 1000, OptimalRange = 500,
            FalloffRange = 1500, FireRate = 1.0, ProjectileSpeed = 500, Accuracy = 0.9,
            CritChance = 0.05, CritMultiplier = 2.0
        };
    }

    [Fact] public void Validate_ValidEquipment_ReturnsTrue() { var eq = CreateTestEquipment(); Assert.True(eq.Validate(out var e)); Assert.Empty(e); }
    [Fact] public void Validate_EmptyId_ReturnsFalse() { var eq = CreateTestEquipment(); eq.Id = ""; Assert.False(eq.Validate(out var e)); Assert.Contains("ID", e); }
    [Fact] public void Validate_WhitespaceId_ReturnsFalse() { var eq = CreateTestEquipment(); eq.Id = "   "; Assert.False(eq.Validate(out var e)); }
    [Fact] public void Validate_EmptyName_ReturnsFalse() { var eq = CreateTestEquipment(); eq.Name = ""; Assert.False(eq.Validate(out var e)); Assert.Contains("name", e); }
    [Fact] public void Validate_NegativeBasePrice_ReturnsFalse() { var eq = CreateTestEquipment(); eq.BasePrice = -100; Assert.False(eq.Validate(out var e)); Assert.Contains("price", e); }
    [Fact] public void Validate_NegativeMass_ReturnsFalse() { var eq = CreateTestEquipment(); eq.Mass = -1; Assert.False(eq.Validate(out var e)); Assert.Contains("Mass", e); }
    [Fact] public void Validate_NegativePowerDraw_ReturnsFalse() { var eq = CreateTestEquipment(); eq.PowerDraw = -1; Assert.False(eq.Validate(out var e)); Assert.Contains("Power", e); }
    [Fact] public void Validate_NegativeHeatGeneration_ReturnsFalse() { var eq = CreateTestEquipment(); eq.HeatGeneration = -1; Assert.False(eq.Validate(out var e)); Assert.Contains("Heat", e); }
    [Fact] public void Validate_ReputationTierOutOfRange_ReturnsFalse() { var eq = CreateTestEquipment(); eq.RequiredReputationTier = 5; Assert.False(eq.Validate(out var e)); Assert.Contains("reputation", e); }
    [Fact] public void Validate_ReputationTierNegative_ReturnsFalse() { var eq = CreateTestEquipment(); eq.RequiredReputationTier = -1; Assert.False(eq.Validate(out var e)); }
    [Fact] public void Validate_ConditionOutOfRange_ReturnsFalse() { var eq = CreateTestEquipment(); eq.Condition = 1.5; Assert.False(eq.Validate(out var e)); Assert.Contains("Condition", e); }
    [Fact] public void Validate_ConditionNegative_ReturnsFalse() { var eq = CreateTestEquipment(); eq.Condition = -0.1; Assert.False(eq.Validate(out var e)); }

    [Fact] public void OnInstalled_SetsInstallationState() { var eq = CreateTestEquipment(); var ship = new Ship { Id = "ship_1" }; eq.OnInstalled(ship, "slot_engine"); Assert.True(eq.IsInstalled); Assert.Equal("ship_1", eq.InstalledOnShipId); Assert.Equal("slot_engine", eq.InstalledSlotId); }
    [Fact] public void OnRemoved_ClearsInstallationState() { var eq = CreateTestEquipment(); eq.IsInstalled = true; eq.InstalledOnShipId = "ship_1"; eq.InstalledSlotId = "slot_engine"; eq.OnRemoved(); Assert.False(eq.IsInstalled); Assert.Null(eq.InstalledOnShipId); Assert.Null(eq.InstalledSlotId); }

    [Fact] public void GetEffectiveModifier_WithFullCondition_ReturnsFullValue() { var eq = CreateTestEquipment(); eq.StatModifiers["speed"] = 1.5; eq.Condition = 1.0; Assert.Equal(1.5, eq.GetEffectiveModifier("speed")); }
    [Fact] public void GetEffectiveModifier_WithReducedCondition_ReturnsReducedValue() { var eq = CreateTestEquipment(); eq.StatModifiers["speed"] = 1.5; eq.Condition = 0.5; Assert.Equal(0.75, eq.GetEffectiveModifier("speed")); }
    [Fact] public void GetEffectiveModifier_NonexistentStat_ReturnsZero() { var eq = CreateTestEquipment(); Assert.Equal(0, eq.GetEffectiveModifier("nonexistent")); }
    [Fact] public void GetEffectiveModifier_ZeroCondition_ReturnsZero() { var eq = CreateTestEquipment(); eq.StatModifiers["armor"] = 2.0; eq.Condition = 0; Assert.Equal(0, eq.GetEffectiveModifier("armor")); }

    [Fact] public void Clone_CreatesDeepCopy() { var eq = CreateTestEquipment(); eq.Tags.Add("military"); eq.StatModifiers["damage"] = 1.2; eq.IsInstalled = true; eq.InstalledOnShipId = "ship_1"; eq.InstalledSlotId = "slot_1"; var clone = eq.Clone(); Assert.Equal(eq.Id, clone.Id); Assert.Equal(eq.Name, clone.Name); Assert.Equal(eq.Type, clone.Type); Assert.Equal(eq.Size, clone.Size); Assert.Equal(eq.MountType, clone.MountType); Assert.Equal(eq.Rarity, clone.Rarity); Assert.Equal(eq.Manufacturer, clone.Manufacturer); Assert.Equal(eq.BasePrice, clone.BasePrice); Assert.Equal(eq.Mass, clone.Mass); Assert.Equal(eq.PowerDraw, clone.PowerDraw); Assert.Equal(eq.HeatGeneration, clone.HeatGeneration); Assert.Equal(eq.Condition, clone.Condition); Assert.Contains("military", clone.Tags); Assert.Equal(1.2, clone.StatModifiers["damage"]); Assert.False(clone.IsInstalled); Assert.Null(clone.InstalledOnShipId); Assert.Null(clone.InstalledSlotId); }
    [Fact] public void Clone_IsIndependent() { var eq = CreateTestEquipment(); var clone = eq.Clone(); clone.Name = "Modified"; clone.Tags.Add("clone_tag"); clone.StatModifiers["speed"] = 2.0; Assert.NotEqual("Modified", eq.Name); Assert.DoesNotContain("clone_tag", eq.Tags); Assert.False(eq.StatModifiers.ContainsKey("speed")); }

    [Fact] public void Serialize_ProducesValidJson() { var eq = CreateTestEquipment(); var json = eq.Serialize(); Assert.Equal("eq_test", json["id"]!.ToString()); Assert.Equal("Test Equipment", json["name"]!.ToString()); Assert.Equal("Utility", json["type"]!.ToString()); Assert.Equal("Small", json["size"]!.ToString()); Assert.Equal("Internal", json["mountType"]!.ToString()); Assert.Equal("Common", json["rarity"]!.ToString()); Assert.Equal(1000, json["basePrice"]!.ToObject<long>()); Assert.Equal(1.0, json["mass"]!.ToObject<double>()); Assert.Equal(5, json["powerDraw"]!.ToObject<double>()); Assert.Equal(2, json["heatGeneration"]!.ToObject<double>()); Assert.Equal(1.0, json["condition"]!.ToObject<double>()); }
    [Fact] public void Deserialize_RestoresAllProperties() { var original = CreateTestEquipment(); original.Tags.Add("test_tag"); original.StatModifiers["range"] = 1.1; var json = original.Serialize(); var restored = new Weapon(); restored.Deserialize(json); Assert.Equal(original.Id, restored.Id); Assert.Equal(original.Name, restored.Name); Assert.Equal(EquipmentType.Weapon, restored.Type); Assert.Equal(original.Size, restored.Size); Assert.Equal(MountType.Hardpoint, restored.MountType); Assert.Equal(original.Rarity, restored.Rarity); Assert.Equal(original.Manufacturer, restored.Manufacturer); Assert.Equal(original.BasePrice, restored.BasePrice); Assert.Equal(original.Mass, restored.Mass); Assert.Equal(original.PowerDraw, restored.PowerDraw); Assert.Equal(original.HeatGeneration, restored.HeatGeneration); Assert.Equal(original.Condition, restored.Condition); Assert.Contains("test_tag", restored.Tags); Assert.Equal(1.1, restored.StatModifiers["range"]); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var eq = CreateTestEquipment("eq_rt", "Round Trip"); eq.Tags.Add("tag1"); eq.Tags.Add("tag2"); eq.StatModifiers["power"] = 0.8; eq.StatModifiers["heat"] = 0.9; var json = eq.Serialize(); var restored = new Weapon(); restored.Deserialize(json); Assert.Equal(eq.Id, restored.Id); Assert.Equal(eq.Name, restored.Name); Assert.Equal(2, restored.Tags.Count); Assert.Equal(2, restored.StatModifiers.Count); Assert.Equal(0.8, restored.StatModifiers["power"]); Assert.Equal(0.9, restored.StatModifiers["heat"]); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var eq = CreateTestEquipment("eq_save", "Save Test"); Assert.Equal("weapon_eq_save", eq.SaveId); }
    [Fact] public void SaveVersion_IsCorrect() { Assert.Equal(2, new Weapon().SaveVersion); }

    [Fact] public void DefaultValues_AreSensible() { var eq = new Weapon(); Assert.Equal(string.Empty, eq.Id); Assert.Equal(string.Empty, eq.Name); Assert.Equal(EquipmentType.Utility, eq.Type); Assert.Equal(EquipmentSize.Small, eq.Size); Assert.Equal(MountType.Internal, eq.MountType); Assert.Equal(EquipmentRarity.Common, eq.Rarity); Assert.Equal(string.Empty, eq.Manufacturer); Assert.Equal(string.Empty, eq.Description); Assert.Equal(1000, eq.BasePrice); Assert.Equal(1.0, eq.Mass); Assert.Equal(0, eq.PowerDraw); Assert.Equal(0, eq.HeatGeneration); Assert.Equal(ShipSize.Tiny, eq.MinimumShipSize); Assert.Equal(0, eq.RequiredReputationTier); Assert.Equal(string.Empty, eq.RequiredFaction); Assert.False(eq.IsInstalled); Assert.Null(eq.InstalledOnShipId); Assert.Null(eq.InstalledSlotId); Assert.Equal(1.0, eq.Condition); Assert.Empty(eq.Tags); Assert.Empty(eq.StatModifiers); }

    [Fact] public void Registry_Register_AddsEquipment() { EquipmentRegistry.Clear(); var eq = CreateTestEquipment("eq_reg_test", "Registry Test"); EquipmentRegistry.Register(eq); var retrieved = EquipmentRegistry.Get("eq_reg_test"); Assert.NotNull(retrieved); Assert.Equal("Registry Test", retrieved!.Name); }
    [Fact] public void Registry_Register_InvalidEquipment_Throws() { EquipmentRegistry.Clear(); var eq = new Weapon { Id = "", Name = "" }; Assert.Throws<ArgumentException>(() => EquipmentRegistry.Register(eq)); }
    [Fact] public void Registry_Get_Nonexistent_ReturnsNull() { EquipmentRegistry.Clear(); Assert.Null(EquipmentRegistry.Get("nonexistent")); }
    [Fact] public void Registry_All_ReturnsAllRegistered() { EquipmentRegistry.Clear(); EquipmentRegistry.Register(CreateTestEquipment("eq_a", "A")); EquipmentRegistry.Register(CreateTestEquipment("eq_b", "B")); Assert.Equal(2, EquipmentRegistry.All.Count); }
    [Fact] public void Registry_GetByType_FiltersCorrectly() { EquipmentRegistry.Clear(); var weapon = CreateTestEquipment("eq_w1", "Weapon 1"); weapon.Type = EquipmentType.Weapon; var shield = CreateTestEquipment("eq_s1", "Shield 1"); shield.Type = EquipmentType.Shield; var weapon2 = CreateTestEquipment("eq_w2", "Weapon 2"); weapon2.Type = EquipmentType.Weapon; EquipmentRegistry.Register(weapon); EquipmentRegistry.Register(shield); EquipmentRegistry.Register(weapon2); Assert.Equal(2, EquipmentRegistry.GetByType(EquipmentType.Weapon).Count); Assert.Single(EquipmentRegistry.GetByType(EquipmentType.Shield)); }
    [Fact] public void Registry_GetBySize_FiltersCorrectly() { EquipmentRegistry.Clear(); var small = CreateTestEquipment("eq_sm", "Small"); small.Size = EquipmentSize.Small; var medium = CreateTestEquipment("eq_md", "Medium"); medium.Size = EquipmentSize.Medium; EquipmentRegistry.Register(small); EquipmentRegistry.Register(medium); Assert.Single(EquipmentRegistry.GetBySize(EquipmentSize.Small)); Assert.Empty(EquipmentRegistry.GetBySize(EquipmentSize.Large)); }
    [Fact] public void Registry_GetByMountType_FiltersCorrectly() { EquipmentRegistry.Clear(); var internalEq = CreateTestEquipment("eq_int", "Internal"); internalEq.MountType = MountType.Internal; var hardpoint = CreateTestEquipment("eq_hp", "Hardpoint"); hardpoint.MountType = MountType.Hardpoint; EquipmentRegistry.Register(internalEq); EquipmentRegistry.Register(hardpoint); Assert.Single(EquipmentRegistry.GetByMountType(MountType.Internal)); Assert.Empty(EquipmentRegistry.GetByMountType(MountType.Turret)); }
    [Fact] public void Registry_GetCompatibleWith_FiltersByShipSize() { EquipmentRegistry.Clear(); var tiny = CreateTestEquipment("eq_tiny", "Tiny"); tiny.MinimumShipSize = ShipSize.Tiny; var medium = CreateTestEquipment("eq_med", "Medium"); medium.MinimumShipSize = ShipSize.Medium; var large = CreateTestEquipment("eq_lrg", "Large"); large.MinimumShipSize = ShipSize.Large; EquipmentRegistry.Register(tiny); EquipmentRegistry.Register(medium); EquipmentRegistry.Register(large); var smallCompatible = EquipmentRegistry.GetCompatibleWith(ShipSize.Small); Assert.Equal(3, smallCompatible.Count); var capitalCompatible = EquipmentRegistry.GetCompatibleWith(ShipSize.Capital); Assert.Equal(3, capitalCompatible.Count); }
    [Fact] public void Registry_Unregister_RemovesEquipment() { EquipmentRegistry.Clear(); var eq = CreateTestEquipment("eq_unreg", "Unregister"); EquipmentRegistry.Register(eq); Assert.NotNull(EquipmentRegistry.Get("eq_unreg")); Assert.True(EquipmentRegistry.Unregister("eq_unreg")); Assert.Null(EquipmentRegistry.Get("eq_unreg")); }
    [Fact] public void Registry_Unregister_Nonexistent_ReturnsFalse() { EquipmentRegistry.Clear(); Assert.False(EquipmentRegistry.Unregister("nonexistent")); }
    [Fact] public void Registry_Clear_RemovesAll() { EquipmentRegistry.Clear(); EquipmentRegistry.Register(CreateTestEquipment("eq_1", "One")); EquipmentRegistry.Register(CreateTestEquipment("eq_2", "Two")); Assert.Equal(2, EquipmentRegistry.All.Count); EquipmentRegistry.Clear(); Assert.Empty(EquipmentRegistry.All); }
    [Fact] public void Registry_LoadFromJson_PopulatesRegistry() { EquipmentRegistry.Clear(); var json = @"[ { ""id"": ""eq_json_wpn"", ""name"": ""JSON Weapon"", ""type"": ""Weapon"", ""size"": ""Small"", ""mountType"": ""Hardpoint"", ""damage"": 30, ""range"": 1500, ""optimalRange"": 750, ""falloffRange"": 2000, ""fireRate"": 1.5, ""projectileSpeed"": 800, ""accuracy"": 0.85, ""critChance"": 0.1, ""critMultiplier"": 2.5 }, { ""id"": ""eq_json_shld"", ""name"": ""JSON Shield"", ""type"": ""Shield"", ""size"": ""Medium"", ""mountType"": ""Internal"", ""capacity"": 300, ""rechargeRate"": 25, ""rechargeDelay"": 2 } ]"; EquipmentRegistry.LoadFromJson(json); Assert.Equal(2, EquipmentRegistry.All.Count); var wpn = EquipmentRegistry.Get("eq_json_wpn"); Assert.NotNull(wpn); Assert.IsType<Weapon>(wpn); Assert.Equal("JSON Weapon", wpn!.Name); var shld = EquipmentRegistry.Get("eq_json_shld"); Assert.NotNull(shld); Assert.IsType<Shield>(shld); Assert.Equal("JSON Shield", shld!.Name); }

    [Fact] public void EquipmentType_AllValues_CanBeSet() { foreach (EquipmentType et in Enum.GetValues<EquipmentType>()) { var eq = CreateTestEquipment(); eq.Type = et; Assert.Equal(et, eq.Type); } }
    [Fact] public void EquipmentSize_AllValues_CanBeSet() { foreach (EquipmentSize es in Enum.GetValues<EquipmentSize>()) { var eq = CreateTestEquipment(); eq.Size = es; Assert.Equal(es, eq.Size); } }
    [Fact] public void MountType_AllValues_CanBeSet() { foreach (MountType mt in Enum.GetValues<MountType>()) { var eq = CreateTestEquipment(); eq.MountType = mt; Assert.Equal(mt, eq.MountType); } }
    [Fact] public void EquipmentRarity_AllValues_CanBeSet() { foreach (EquipmentRarity er in Enum.GetValues<EquipmentRarity>()) { var eq = CreateTestEquipment(); eq.Rarity = er; Assert.Equal(er, eq.Rarity); } }

    [Fact] public void Validate_ZeroBasePrice_Valid() { var eq = CreateTestEquipment(); eq.BasePrice = 0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ZeroMass_Valid() { var eq = CreateTestEquipment(); eq.Mass = 0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ZeroPowerDraw_Valid() { var eq = CreateTestEquipment(); eq.PowerDraw = 0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ZeroHeatGeneration_Valid() { var eq = CreateTestEquipment(); eq.HeatGeneration = 0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ConditionZero_Valid() { var eq = CreateTestEquipment(); eq.Condition = 0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ConditionOne_Valid() { var eq = CreateTestEquipment(); eq.Condition = 1.0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ReputationTierZero_Valid() { var eq = CreateTestEquipment(); eq.RequiredReputationTier = 0; Assert.True(eq.Validate(out _)); }
    [Fact] public void Validate_ReputationTierFour_Valid() { var eq = CreateTestEquipment(); eq.RequiredReputationTier = 4; Assert.True(eq.Validate(out _)); }
    [Fact] public void OnInstalled_DifferentShip_UpdatesCorrectly() { var eq = CreateTestEquipment(); var ship1 = new Ship { Id = "ship_a" }; var ship2 = new Ship { Id = "ship_b" }; eq.OnInstalled(ship1, "slot_1"); Assert.Equal("ship_a", eq.InstalledOnShipId); eq.OnInstalled(ship2, "slot_2"); Assert.Equal("ship_b", eq.InstalledOnShipId); Assert.Equal("slot_2", eq.InstalledSlotId); }
}
