using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

[Collection("Sequential")]
public class UpgradeSlotTests
{
    private UpgradeSlot CreateValidSlot(string id = "slot_engine", string name = "Engine Slot")
    {
        return new UpgradeSlot
        {
            Id = id, Name = name, Type = UpgradeSlotType.Internal, Size = UpgradeSlotSize.Medium,
            IsUnlocked = true, UnlockRequirement = "", UnlockCost = 0, Position = (0, 0, 0),
            Orientation = (0, 0, 0), ArcLimits = null, IsDamaged = false, DamageSeverity = 0,
            PowerPriority = 0, CoolingPriority = 0
        };
    }

    private Weapon CreateTestWeapon(string id = "wpn_test", string name = "Test Laser")
    {
        return new Weapon
        {
            Id = id, Name = name, Type = EquipmentType.Weapon, Size = EquipmentSize.Small,
            MountType = MountType.Hardpoint, Rarity = EquipmentRarity.Common, Manufacturer = "TestCorp",
            Description = "Test weapon", BasePrice = 1000, Mass = 1.0, PowerDraw = 5, HeatGeneration = 2,
            MinimumShipSize = ShipSize.Tiny, Damage = 10, Range = 1000, OptimalRange = 500,
            FalloffRange = 1500, FireRate = 1.0, ProjectileSpeed = 500, Accuracy = 0.9,
            CritChance = 0.05, CritMultiplier = 2.0
        };
    }

    [Fact] public void Validate_ValidSlot_ReturnsTrue() { var s = CreateValidSlot(); Assert.True(s.Validate(out var e)); Assert.Empty(e); }
    [Fact] public void Validate_EmptyId_ReturnsFalse() { var s = CreateValidSlot(); s.Id = ""; Assert.False(s.Validate(out var e)); Assert.Contains("ID", e); }
    [Fact] public void Validate_WhitespaceId_ReturnsFalse() { var s = CreateValidSlot(); s.Id = "   "; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_EmptyName_ReturnsFalse() { var s = CreateValidSlot(); s.Name = ""; Assert.False(s.Validate(out var e)); Assert.Contains("name", e); }
    [Fact] public void Validate_DamageSeverityOutOfRange_ReturnsFalse() { var s = CreateValidSlot(); s.DamageSeverity = 1.5; Assert.False(s.Validate(out var e)); Assert.Contains("Damage", e); }
    [Fact] public void Validate_DamageSeverityNegative_ReturnsFalse() { var s = CreateValidSlot(); s.DamageSeverity = -0.1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_NegativeUnlockCost_ReturnsFalse() { var s = CreateValidSlot(); s.UnlockCost = -100; Assert.False(s.Validate(out var e)); Assert.Contains("Unlock", e); }

    [Fact] public void CanInstall_UnlockedEmptySlot_ReturnsTrue() { var s = CreateValidSlot(); Assert.True(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_LockedSlot_ReturnsFalse() { var s = CreateValidSlot(); s.IsUnlocked = false; Assert.False(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_OccupiedSlot_ReturnsFalse() { var s = CreateValidSlot(); s.InstalledEquipmentId = "existing_eq"; Assert.False(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_SeverelyDamagedSlot_ReturnsFalse() { var s = CreateValidSlot(); s.IsDamaged = true; s.DamageSeverity = 0.8; Assert.False(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_MildlyDamagedSlot_ReturnsTrue() { var s = CreateValidSlot(); s.IsDamaged = true; s.DamageSeverity = 0.3; Assert.True(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_DamageSeverityAtBoundary_ReturnsTrue() { var s = CreateValidSlot(); s.IsDamaged = true; s.DamageSeverity = 0.5; Assert.True(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_AllowedTypesMatch_ReturnsTrue() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); var eq = CreateTestWeapon(); eq.Type = EquipmentType.Weapon; Assert.True(s.CanInstall(eq)); }
    [Fact] public void CanInstall_AllowedTypesMismatch_ReturnsFalse() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Shield); var eq = CreateTestWeapon(); eq.Type = EquipmentType.Weapon; Assert.False(s.CanInstall(eq)); }
    [Fact] public void CanInstall_DisallowedType_ReturnsFalse() { var s = CreateValidSlot(); s.DisallowedEquipmentTypes.Add(EquipmentType.Weapon); var eq = CreateTestWeapon(); eq.Type = EquipmentType.Weapon; Assert.False(s.CanInstall(eq)); }
    [Fact] public void CanInstall_AllowedSizes_EquipmentFits_ReturnsTrue() { var s = CreateValidSlot(); s.Size = UpgradeSlotSize.Medium; s.AllowedEquipmentSizes.Add(EquipmentSize.Small); s.AllowedEquipmentSizes.Add(EquipmentSize.Medium); var eq = CreateTestWeapon(); eq.Size = EquipmentSize.Small; Assert.True(s.CanInstall(eq)); }
    [Fact] public void CanInstall_AllowedSizes_EquipmentTooLarge_ReturnsFalse() { var s = CreateValidSlot(); s.Size = UpgradeSlotSize.Small; s.AllowedEquipmentSizes.Add(EquipmentSize.Small); var eq = CreateTestWeapon(); eq.Size = EquipmentSize.Large; Assert.False(s.CanInstall(eq)); }
    [Fact] public void CanInstall_UniversalSlot_AcceptsAnySize() { var s = CreateValidSlot(); s.Size = UpgradeSlotSize.Universal; var eq = CreateTestWeapon(); eq.Size = EquipmentSize.Huge; Assert.True(s.CanInstall(eq)); }
    [Fact] public void CanInstall_RequiredTagsMatch_ReturnsTrue() { var s = CreateValidSlot(); s.RequiredTags.Add("military"); var eq = CreateTestWeapon(); eq.Tags.Add("military"); Assert.True(s.CanInstall(eq)); }
    [Fact] public void CanInstall_RequiredTagsMissing_ReturnsFalse() { var s = CreateValidSlot(); s.RequiredTags.Add("military"); Assert.False(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_MultipleRequiredTags_AllMustMatch() { var s = CreateValidSlot(); s.RequiredTags.Add("military"); s.RequiredTags.Add("experimental"); var eq = CreateTestWeapon(); eq.Tags.Add("military"); Assert.False(s.CanInstall(eq)); }
    [Fact] public void CanInstall_ForbiddenTag_ReturnsFalse() { var s = CreateValidSlot(); s.ForbiddenTags.Add("civilian"); var eq = CreateTestWeapon(); eq.Tags.Add("civilian"); Assert.False(s.CanInstall(eq)); }
    [Fact] public void CanInstall_ForbiddenTagNotPresent_ReturnsTrue() { var s = CreateValidSlot(); s.ForbiddenTags.Add("civilian"); Assert.True(s.CanInstall(CreateTestWeapon())); }
    [Fact] public void CanInstall_NoAllowedTypes_AllTypesAllowed() { var s = CreateValidSlot(); var eq = CreateTestWeapon(); eq.Type = EquipmentType.Shield; Assert.True(s.CanInstall(eq)); }
    [Fact] public void CanInstall_NoAllowedSizes_AllSizesAllowed() { var s = CreateValidSlot(); var eq = CreateTestWeapon(); eq.Size = EquipmentSize.Huge; Assert.True(s.CanInstall(eq)); }

    [Fact] public void InstallEquipment_ValidEquipment_SetsEquipmentId() { var s = CreateValidSlot(); var eq = CreateTestWeapon("wpn_install", "Install Test"); Assert.True(s.CanInstall(eq)); s.InstalledEquipmentId = eq.Id; Assert.Equal("wpn_install", s.InstalledEquipmentId); }
    [Fact] public void InstallEquipment_IncompatibleEquipment_ReturnsFalse() { var s = CreateValidSlot(); s.IsUnlocked = false; var eq = CreateTestWeapon(); Assert.False(s.InstallEquipment(eq)); Assert.Null(s.InstalledEquipmentId); }
    [Fact] public void InstallEquipment_OccupiedSlot_ReturnsFalse() { var s = CreateValidSlot(); s.InstalledEquipmentId = "existing"; Assert.False(s.InstallEquipment(CreateTestWeapon())); }

    [Fact] public void RemoveEquipment_InstalledEquipment_ReturnsId() { var s = CreateValidSlot(); s.InstalledEquipmentId = "wpn_removed"; Assert.Equal("wpn_removed", s.RemoveEquipment()); Assert.Null(s.InstalledEquipmentId); }
    [Fact] public void RemoveEquipment_EmptySlot_ReturnsNull() { var s = CreateValidSlot(); Assert.Null(s.RemoveEquipment()); }

    [Fact] public void GetInstalledEquipment_RegisteredEquipment_ReturnsEquipment() { EquipmentRegistry.Clear(); var eq = CreateTestWeapon("wpn_get", "Get Test"); EquipmentRegistry.Register(eq); var s = CreateValidSlot(); s.InstalledEquipmentId = "wpn_get"; var retrieved = s.GetInstalledEquipment(); Assert.NotNull(retrieved); Assert.Equal("Get Test", retrieved!.Name); }
    [Fact] public void GetInstalledEquipment_UnregisteredEquipment_ReturnsNull() { EquipmentRegistry.Clear(); var s = CreateValidSlot(); s.InstalledEquipmentId = "nonexistent"; Assert.Null(s.GetInstalledEquipment()); }
    [Fact] public void GetInstalledEquipment_EmptySlot_ReturnsNull() { var s = CreateValidSlot(); Assert.Null(s.GetInstalledEquipment()); }

    [Fact] public void Damage_SetsDamagedState() { var s = CreateValidSlot(); s.Damage(0.3); Assert.True(s.IsDamaged); Assert.Equal(0.3, s.DamageSeverity); }
    [Fact] public void Damage_ClampedToOne() { var s = CreateValidSlot(); s.Damage(1.5); Assert.Equal(1.0, s.DamageSeverity); }
    [Fact] public void Damage_ClampedToZero() { var s = CreateValidSlot(); s.Damage(-0.5); Assert.Equal(0, s.DamageSeverity); }
    [Fact] public void Damage_FullSeverity_RemovesEquipment() { var s = CreateValidSlot(); s.InstalledEquipmentId = "wpn_destroyed"; s.Damage(1.0); Assert.True(s.IsDamaged); Assert.Equal(1.0, s.DamageSeverity); Assert.Null(s.InstalledEquipmentId); }
    [Fact] public void Damage_PartialSeverity_KeepsEquipment() { var s = CreateValidSlot(); s.InstalledEquipmentId = "wpn_keep"; s.Damage(0.5); Assert.Equal("wpn_keep", s.InstalledEquipmentId); }

    [Fact] public void Repair_ReducesSeverity() { var s = CreateValidSlot(); s.Damage(0.8); s.Repair(0.3); Assert.Equal(0.5, s.DamageSeverity); Assert.True(s.IsDamaged); }
    [Fact] public void Repair_FullRepair_ClearsDamage() { var s = CreateValidSlot(); s.Damage(0.3); s.Repair(0.5); Assert.Equal(0, s.DamageSeverity); Assert.False(s.IsDamaged); }
    [Fact] public void Repair_ExcessRepair_ClampedToZero() { var s = CreateValidSlot(); s.Damage(0.2); s.Repair(1.0); Assert.Equal(0, s.DamageSeverity); Assert.False(s.IsDamaged); }
    [Fact] public void FullRepair_ClearsAllDamage() { var s = CreateValidSlot(); s.Damage(0.9); s.FullRepair(); Assert.Equal(0, s.DamageSeverity); Assert.False(s.IsDamaged); }

    [Fact] public void Unlock_SetsUnlocked() { var s = CreateValidSlot(); s.IsUnlocked = false; s.UnlockRequirement = "rep_tier_3"; s.UnlockCost = 50000; s.Unlock(); Assert.True(s.IsUnlocked); Assert.Equal(string.Empty, s.UnlockRequirement); Assert.Equal(0, s.UnlockCost); }
    [Fact] public void Lock_SetsLockedWithRequirement() { var s = CreateValidSlot(); s.InstalledEquipmentId = "wpn_removed_on_lock"; s.Lock("rep_tier_4", 100000); Assert.False(s.IsUnlocked); Assert.Equal("rep_tier_4", s.UnlockRequirement); Assert.Equal(100000, s.UnlockCost); Assert.Null(s.InstalledEquipmentId); }
    [Fact] public void Lock_RemovesInstalledEquipment() { var s = CreateValidSlot(); s.InstalledEquipmentId = "wpn_to_remove"; s.Lock("requirement", 5000); Assert.Null(s.InstalledEquipmentId); }

    [Fact] public void Clone_CreatesDeepCopy() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); s.AllowedEquipmentTypes.Add(EquipmentType.Shield); s.AllowedEquipmentSizes.Add(EquipmentSize.Small); s.DisallowedEquipmentTypes.Add(EquipmentType.Mining); s.RequiredTags.Add("military"); s.ForbiddenTags.Add("civilian"); s.InstalledEquipmentId = "wpn_clone"; s.IsUnlocked = true; s.UnlockRequirement = "rep_2"; s.UnlockCost = 25000; s.Position = (1, 2, 3); s.Orientation = (45, 90, 0); s.ArcLimits = (-30, 30, -90, 90); s.IsDamaged = true; s.DamageSeverity = 0.4; s.PowerPriority = 2; s.CoolingPriority = 1; s.Tags.Add("core_slot"); var clone = s.Clone(); Assert.Equal(s.Id, clone.Id); Assert.Equal(s.Name, clone.Name); Assert.Equal(s.Type, clone.Type); Assert.Equal(s.Size, clone.Size); Assert.Equal(s.InstalledEquipmentId, clone.InstalledEquipmentId); Assert.Equal(s.IsUnlocked, clone.IsUnlocked); Assert.Equal(s.UnlockRequirement, clone.UnlockRequirement); Assert.Equal(s.UnlockCost, clone.UnlockCost); Assert.Equal(s.Position, clone.Position); Assert.Equal(s.Orientation, clone.Orientation); Assert.Equal(s.ArcLimits, clone.ArcLimits); Assert.Equal(s.IsDamaged, clone.IsDamaged); Assert.Equal(s.DamageSeverity, clone.DamageSeverity); Assert.Equal(s.PowerPriority, clone.PowerPriority); Assert.Equal(s.CoolingPriority, clone.CoolingPriority); Assert.Equal(2, clone.AllowedEquipmentTypes.Count); Assert.Contains(EquipmentType.Weapon, clone.AllowedEquipmentTypes); Assert.Contains(EquipmentType.Shield, clone.AllowedEquipmentTypes); Assert.Single(clone.AllowedEquipmentSizes); Assert.Single(clone.DisallowedEquipmentTypes); Assert.Single(clone.RequiredTags); Assert.Single(clone.ForbiddenTags); Assert.Single(clone.Tags); Assert.Contains("core_slot", clone.Tags); }
    [Fact] public void Clone_IsIndependent() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); var clone = s.Clone(); clone.AllowedEquipmentTypes.Add(EquipmentType.Shield); clone.Name = "Modified Clone"; Assert.Single(s.AllowedEquipmentTypes); Assert.NotEqual("Modified Clone", s.Name); }

    [Fact] public void Serialize_ProducesValidJson() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); s.RequiredTags.Add("military"); s.InstalledEquipmentId = "wpn_serialized"; s.Position = (1, 2, 3); s.ArcLimits = (-30, 30, -90, 90); var json = s.Serialize(); Assert.Equal("slot_engine", json["id"]!.ToString()); Assert.Equal("Engine Slot", json["name"]!.ToString()); Assert.Equal("Internal", json["type"]!.ToString()); Assert.Equal("Medium", json["size"]!.ToString()); Assert.Equal("wpn_serialized", json["installedEquipmentId"]!.ToString()); Assert.True(json["isUnlocked"]!.ToObject<bool>()); Assert.Equal(1, json["positionX"]!.ToObject<double>()); Assert.Equal(2, json["positionY"]!.ToObject<double>()); Assert.Equal(3, json["positionZ"]!.ToObject<double>()); Assert.True(json["hasArcLimits"]!.ToObject<bool>()); Assert.Equal(-30, json["arcLimitsMinPitch"]!.ToObject<double>()); Assert.Equal(30, json["arcLimitsMaxPitch"]!.ToObject<double>()); }
    [Fact] public void Deserialize_RestoresAllProperties() { var original = CreateValidSlot(); original.AllowedEquipmentTypes.Add(EquipmentType.Weapon); original.AllowedEquipmentTypes.Add(EquipmentType.Shield); original.AllowedEquipmentSizes.Add(EquipmentSize.Medium); original.DisallowedEquipmentTypes.Add(EquipmentType.Mining); original.RequiredTags.Add("military"); original.ForbiddenTags.Add("civilian"); original.InstalledEquipmentId = "wpn_deser"; original.IsUnlocked = false; original.UnlockRequirement = "rep_3"; original.UnlockCost = 75000; original.Position = (5, 10, 15); original.Orientation = (90, 180, 270); original.ArcLimits = (-45, 45, -180, 180); original.IsDamaged = true; original.DamageSeverity = 0.6; original.PowerPriority = 3; original.CoolingPriority = 2; original.Tags.Add("important"); var json = original.Serialize(); var restored = new UpgradeSlot(); restored.Deserialize(json); Assert.Equal(original.Id, restored.Id); Assert.Equal(original.Name, restored.Name); Assert.Equal(original.Type, restored.Type); Assert.Equal(original.Size, restored.Size); Assert.Equal(2, restored.AllowedEquipmentTypes.Count); Assert.Contains(EquipmentType.Weapon, restored.AllowedEquipmentTypes); Assert.Contains(EquipmentType.Shield, restored.AllowedEquipmentTypes); Assert.Single(restored.AllowedEquipmentSizes); Assert.Single(restored.DisallowedEquipmentTypes); Assert.Single(restored.RequiredTags); Assert.Single(restored.ForbiddenTags); Assert.Equal("wpn_deser", restored.InstalledEquipmentId); Assert.False(restored.IsUnlocked); Assert.Equal("rep_3", restored.UnlockRequirement); Assert.Equal(75000, restored.UnlockCost); Assert.Equal((5, 10, 15), restored.Position); Assert.Equal((90, 180, 270), restored.Orientation); Assert.NotNull(restored.ArcLimits); Assert.Equal((-45, 45, -180, 180), restored.ArcLimits!.Value); Assert.True(restored.IsDamaged); Assert.Equal(0.6, restored.DamageSeverity); Assert.Equal(3, restored.PowerPriority); Assert.Equal(2, restored.CoolingPriority); Assert.Contains("important", restored.Tags); }
    [Fact] public void Deserialize_NoArcLimits_SetsNull() { var s = CreateValidSlot(); var json = s.Serialize(); var restored = new UpgradeSlot(); restored.Deserialize(json); Assert.Null(restored.ArcLimits); }
    [Fact] public void Deserialize_EmptyInstalledEquipmentId_SetsNull() { var s = CreateValidSlot(); var json = s.Serialize(); var restored = new UpgradeSlot(); restored.Deserialize(json); Assert.Null(restored.InstalledEquipmentId); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var s = CreateValidSlot("slot_rt", "Round Trip"); s.AllowedEquipmentTypes.Add(EquipmentType.Engine); s.RequiredTags.Add("premium"); s.Tags.Add("special"); s.ArcLimits = (-20, 20, -60, 60); var json = s.Serialize(); var restored = new UpgradeSlot(); restored.Deserialize(json); Assert.Equal(s.Id, restored.Id); Assert.Equal(s.Name, restored.Name); Assert.Single(restored.AllowedEquipmentTypes); Assert.Single(restored.RequiredTags); Assert.Single(restored.Tags); Assert.NotNull(restored.ArcLimits); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var s = CreateValidSlot("slot_save", "Save Test"); Assert.Equal("upgradeslot_slot_save", s.SaveId); }
    [Fact] public void SaveVersion_IsOne() { Assert.Equal(1, new UpgradeSlot().SaveVersion); }

    [Fact] public void DefaultValues_AreSensible() { var s = new UpgradeSlot(); Assert.Equal(string.Empty, s.Id); Assert.Equal(string.Empty, s.Name); Assert.Equal(UpgradeSlotType.Internal, s.Type); Assert.Equal(UpgradeSlotSize.Medium, s.Size); Assert.Empty(s.AllowedEquipmentTypes); Assert.Empty(s.AllowedEquipmentSizes); Assert.Empty(s.DisallowedEquipmentTypes); Assert.Empty(s.RequiredTags); Assert.Empty(s.ForbiddenTags); Assert.Null(s.InstalledEquipmentId); Assert.True(s.IsUnlocked); Assert.Equal(string.Empty, s.UnlockRequirement); Assert.Equal(0, s.UnlockCost); Assert.Equal((0, 0, 0), s.Position); Assert.Equal((0, 0, 0), s.Orientation); Assert.Null(s.ArcLimits); Assert.False(s.IsDamaged); Assert.Equal(0, s.DamageSeverity); Assert.Equal(0, s.PowerPriority); Assert.Equal(0, s.CoolingPriority); Assert.Empty(s.Tags); }

    [Fact] public void UpgradeSlotType_AllValues_CanBeSet() { foreach (UpgradeSlotType ust in Enum.GetValues<UpgradeSlotType>()) { var s = CreateValidSlot(); s.Type = ust; Assert.Equal(ust, s.Type); } }
    [Fact] public void UpgradeSlotSize_AllValues_CanBeSet() { foreach (UpgradeSlotSize uss in Enum.GetValues<UpgradeSlotSize>()) { var s = CreateValidSlot(); s.Size = uss; Assert.Equal(uss, s.Size); } }

    [Fact] public void Validate_DamageSeverityZero_Valid() { var s = CreateValidSlot(); s.DamageSeverity = 0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_DamageSeverityOne_Valid() { var s = CreateValidSlot(); s.DamageSeverity = 1.0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_ZeroUnlockCost_Valid() { var s = CreateValidSlot(); s.UnlockCost = 0; Assert.True(s.Validate(out _)); }
    [Fact] public void CanInstall_AllChecksCombined_AllPass() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); s.AllowedEquipmentSizes.Add(EquipmentSize.Small); s.RequiredTags.Add("military"); var eq = CreateTestWeapon(); eq.Type = EquipmentType.Weapon; eq.Size = EquipmentSize.Small; eq.Tags.Add("military"); Assert.True(s.CanInstall(eq)); }
    [Fact] public void CanInstall_AllChecksCombined_OneFails() { var s = CreateValidSlot(); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); s.AllowedEquipmentSizes.Add(EquipmentSize.Small); s.RequiredTags.Add("military"); var eq = CreateTestWeapon(); eq.Type = EquipmentType.Weapon; eq.Size = EquipmentSize.Small; Assert.False(s.CanInstall(eq)); }
    [Fact] public void Damage_ZeroSeverity_StillMarksDamaged() { var s = CreateValidSlot(); s.Damage(0); Assert.True(s.IsDamaged); Assert.Equal(0, s.DamageSeverity); }
    [Fact] public void Repair_UndamagedSlot_NoEffect() { var s = CreateValidSlot(); s.Repair(0.5); Assert.Equal(0, s.DamageSeverity); Assert.False(s.IsDamaged); }
}

[Collection("Sequential")]
public class UpgradeSlotCollectionTests
{
    private UpgradeSlot CreateSlot(string id, UpgradeSlotType type = UpgradeSlotType.Internal)
    {
        return new UpgradeSlot { Id = id, Name = $"Slot {id}", Type = type, Size = UpgradeSlotSize.Medium, IsUnlocked = true };
    }

    private Weapon CreateTestWeapon(string id)
    {
        return new Weapon
        {
            Id = id, Name = $"Weapon {id}", Type = EquipmentType.Weapon, Size = EquipmentSize.Small,
            MountType = MountType.Hardpoint, Damage = 10, Range = 1000, OptimalRange = 500,
            FalloffRange = 1500, FireRate = 1.0, ProjectileSpeed = 500, Accuracy = 0.9,
            CritChance = 0.05, CritMultiplier = 2.0
        };
    }

    [Fact] public void AddSlot_AddsToCollection() { var c = new UpgradeSlotCollection { ShipId = "ship_1" }; c.AddSlot(CreateSlot("slot_a")); Assert.Single(c.Slots); Assert.True(c.Slots.ContainsKey("slot_a")); }
    [Fact] public void RemoveSlot_ExistingSlot_ReturnsTrue() { var c = new UpgradeSlotCollection(); c.AddSlot(CreateSlot("slot_a")); Assert.True(c.RemoveSlot("slot_a")); Assert.Empty(c.Slots); }
    [Fact] public void RemoveSlot_NonexistentSlot_ReturnsFalse() { var c = new UpgradeSlotCollection(); Assert.False(c.RemoveSlot("nonexistent")); }
    [Fact] public void GetSlot_ExistingSlot_ReturnsSlot() { var c = new UpgradeSlotCollection(); var s = CreateSlot("slot_a"); c.AddSlot(s); var r = c.GetSlot("slot_a"); Assert.NotNull(r); Assert.Equal("slot_a", r!.Id); }
    [Fact] public void GetSlot_NonexistentSlot_ReturnsNull() { var c = new UpgradeSlotCollection(); Assert.Null(c.GetSlot("nonexistent")); }

    [Fact] public void GetSlotsByType_FiltersCorrectly() { var c = new UpgradeSlotCollection(); c.AddSlot(CreateSlot("slot_w1", UpgradeSlotType.Weapon)); c.AddSlot(CreateSlot("slot_w2", UpgradeSlotType.Weapon)); c.AddSlot(CreateSlot("slot_u1", UpgradeSlotType.Utility)); c.AddSlot(CreateSlot("slot_i1", UpgradeSlotType.Internal)); Assert.Equal(2, c.GetSlotsByType(UpgradeSlotType.Weapon).Count()); Assert.Single(c.GetSlotsByType(UpgradeSlotType.Utility)); Assert.Empty(c.GetSlotsByType(UpgradeSlotType.Sensor)); }
    [Fact] public void GetUnlockedSlotsByType_ExcludesLocked() { var c = new UpgradeSlotCollection(); var unlocked = CreateSlot("slot_w1", UpgradeSlotType.Weapon); unlocked.IsUnlocked = true; var locked = CreateSlot("slot_w2", UpgradeSlotType.Weapon); locked.IsUnlocked = false; c.AddSlot(unlocked); c.AddSlot(locked); var slots = c.GetUnlockedSlotsByType(UpgradeSlotType.Weapon).ToList(); Assert.Single(slots); Assert.Equal("slot_w1", slots[0].Id); }
    [Fact] public void GetFreeSlotsByType_ExcludesOccupiedAndDamaged() { var c = new UpgradeSlotCollection(); var free = CreateSlot("slot_w1", UpgradeSlotType.Weapon); var occupied = CreateSlot("slot_w2", UpgradeSlotType.Weapon); occupied.InstalledEquipmentId = "wpn_1"; var damaged = CreateSlot("slot_w3", UpgradeSlotType.Weapon); damaged.IsDamaged = true; damaged.DamageSeverity = 0.8; var locked = CreateSlot("slot_w4", UpgradeSlotType.Weapon); locked.IsUnlocked = false; c.AddSlot(free); c.AddSlot(occupied); c.AddSlot(damaged); c.AddSlot(locked); var freeSlots = c.GetFreeSlotsByType(UpgradeSlotType.Weapon).ToList(); Assert.Single(freeSlots); Assert.Equal("slot_w1", freeSlots[0].Id); }

    [Fact] public void FindFreeSlotFor_CompatibleSlot_ReturnsSlot() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var s = CreateSlot("slot_w1", UpgradeSlotType.Weapon); s.AllowedEquipmentTypes.Add(EquipmentType.Weapon); c.AddSlot(s); var eq = CreateTestWeapon("wpn_find"); eq.Type = EquipmentType.Weapon; EquipmentRegistry.Register(eq); var found = c.FindFreeSlotFor(eq); Assert.NotNull(found); Assert.Equal("slot_w1", found!.Id); }
    [Fact] public void FindFreeSlotFor_NoCompatibleSlot_ReturnsNull() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var s = CreateSlot("slot_w1", UpgradeSlotType.Weapon); s.AllowedEquipmentTypes.Add(EquipmentType.Shield); c.AddSlot(s); var eq = CreateTestWeapon("wpn_nofit"); eq.Type = EquipmentType.Weapon; EquipmentRegistry.Register(eq); Assert.Null(c.FindFreeSlotFor(eq)); }
    [Fact] public void FindFreeSlotFor_OccupiedSlot_SkipsIt() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var occupied = CreateSlot("slot_w1", UpgradeSlotType.Weapon); occupied.InstalledEquipmentId = "existing"; var free = CreateSlot("slot_w2", UpgradeSlotType.Weapon); c.AddSlot(occupied); c.AddSlot(free); var eq = CreateTestWeapon("wpn_skip"); eq.Type = EquipmentType.Weapon; EquipmentRegistry.Register(eq); var found = c.FindFreeSlotFor(eq); Assert.NotNull(found); Assert.Equal("slot_w2", found!.Id); }

    [Fact] public void InstallEquipment_FindsAndInstalls_ReturnsTrue() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var s = CreateSlot("slot_w1", UpgradeSlotType.Weapon); c.AddSlot(s); var eq = CreateTestWeapon("wpn_col_install"); eq.Type = EquipmentType.Weapon; EquipmentRegistry.Register(eq); Assert.True(c.InstallEquipment(eq, out var slotId)); Assert.Equal("slot_w1", slotId); Assert.Equal("wpn_col_install", s.InstalledEquipmentId); }
    [Fact] public void InstallEquipment_NoFreeSlot_ReturnsFalse() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var s = CreateSlot("slot_w1", UpgradeSlotType.Weapon); s.IsUnlocked = false; c.AddSlot(s); var eq = CreateTestWeapon("wpn_noslot"); eq.Type = EquipmentType.Weapon; EquipmentRegistry.Register(eq); Assert.False(c.InstallEquipment(eq, out var slotId)); Assert.Null(slotId); }

    [Fact] public void TotalPowerDraw_SumsAllEquipment() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var eq1 = CreateTestWeapon("wpn_pwr1"); eq1.PowerDraw = 10; EquipmentRegistry.Register(eq1); var s1 = CreateSlot("slot_1"); s1.InstallEquipment(eq1); c.AddSlot(s1); var eq2 = CreateTestWeapon("wpn_pwr2"); eq2.PowerDraw = 15; EquipmentRegistry.Register(eq2); var s2 = CreateSlot("slot_2"); s2.InstallEquipment(eq2); c.AddSlot(s2); Assert.Equal(25, c.TotalPowerDraw); }
    [Fact] public void TotalHeatGeneration_SumsAllEquipment() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var eq1 = CreateTestWeapon("wpn_heat1"); eq1.HeatGeneration = 5; EquipmentRegistry.Register(eq1); var s1 = CreateSlot("slot_1"); s1.InstallEquipment(eq1); c.AddSlot(s1); var eq2 = CreateTestWeapon("wpn_heat2"); eq2.HeatGeneration = 8; EquipmentRegistry.Register(eq2); var s2 = CreateSlot("slot_2"); s2.InstallEquipment(eq2); c.AddSlot(s2); Assert.Equal(13, c.TotalHeatGeneration); }
    [Fact] public void TotalMass_SumsAllEquipment() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var eq1 = CreateTestWeapon("wpn_mass1"); eq1.Mass = 3.0; EquipmentRegistry.Register(eq1); var s1 = CreateSlot("slot_1"); s1.InstallEquipment(eq1); c.AddSlot(s1); var eq2 = CreateTestWeapon("wpn_mass2"); eq2.Mass = 7.0; EquipmentRegistry.Register(eq2); var s2 = CreateSlot("slot_2"); s2.InstallEquipment(eq2); c.AddSlot(s2); Assert.Equal(10.0, c.TotalMass); }
    [Fact] public void TotalProperties_EmptyCollection_ReturnsZero() { var c = new UpgradeSlotCollection(); Assert.Equal(0, c.TotalPowerDraw); Assert.Equal(0, c.TotalHeatGeneration); Assert.Equal(0, c.TotalMass); }

    [Fact] public void GetInstalledEquipment_ReturnsAllInstalled() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var eq1 = CreateTestWeapon("wpn_inst1"); EquipmentRegistry.Register(eq1); var s1 = CreateSlot("slot_1"); s1.InstallEquipment(eq1); c.AddSlot(s1); var eq2 = CreateTestWeapon("wpn_inst2"); EquipmentRegistry.Register(eq2); var s2 = CreateSlot("slot_2"); s2.InstallEquipment(eq2); c.AddSlot(s2); c.AddSlot(CreateSlot("slot_3")); Assert.Equal(2, c.GetInstalledEquipment().Count()); }
    [Fact] public void GetInstalledEquipmentByType_FiltersCorrectly() { EquipmentRegistry.Clear(); var c = new UpgradeSlotCollection(); var weapon = CreateTestWeapon("wpn_type1"); weapon.Type = EquipmentType.Weapon; EquipmentRegistry.Register(weapon); var s1 = CreateSlot("slot_1"); s1.InstallEquipment(weapon); c.AddSlot(s1); var shield = new Shield { Id = "shld_type1", Name = "Test Shield", Type = EquipmentType.Shield, Capacity = 100, RechargeRate = 10, RechargeDelay = 3 }; EquipmentRegistry.Register(shield); var s2 = CreateSlot("slot_2"); s2.InstallEquipment(shield); c.AddSlot(s2); Assert.Single(c.GetInstalledEquipmentByType(EquipmentType.Weapon)); Assert.Single(c.GetInstalledEquipmentByType(EquipmentType.Shield)); }

    [Fact] public void Serialize_ProducesValidJson() { var c = new UpgradeSlotCollection { ShipId = "ship_test" }; c.AddSlot(CreateSlot("slot_a")); c.AddSlot(CreateSlot("slot_b")); var json = c.Serialize(); Assert.Equal("ship_test", json["shipId"]!.ToString()); Assert.NotNull(json["slots"]); }
    [Fact] public void Deserialize_RestoresSlots() { var original = new UpgradeSlotCollection { ShipId = "ship_orig" }; original.AddSlot(CreateSlot("slot_a")); original.AddSlot(CreateSlot("slot_b")); var json = original.Serialize(); var restored = new UpgradeSlotCollection(); restored.Deserialize(json); Assert.Equal("ship_orig", restored.ShipId); Assert.Equal(2, restored.Slots.Count); Assert.True(restored.Slots.ContainsKey("slot_a")); Assert.True(restored.Slots.ContainsKey("slot_b")); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var original = new UpgradeSlotCollection { ShipId = "ship_rt" }; var s = CreateSlot("slot_rt"); s.InstalledEquipmentId = "wpn_rt"; s.IsDamaged = true; s.DamageSeverity = 0.3; original.AddSlot(s); var json = original.Serialize(); var restored = new UpgradeSlotCollection(); restored.Deserialize(json); Assert.Equal("ship_rt", restored.ShipId); var rs = restored.GetSlot("slot_rt"); Assert.NotNull(rs); Assert.Equal("wpn_rt", rs!.InstalledEquipmentId); Assert.True(rs.IsDamaged); Assert.Equal(0.3, rs.DamageSeverity); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var c = new UpgradeSlotCollection { ShipId = "ship_save" }; Assert.Equal("upgradeslots_ship_save", c.SaveId); }
    [Fact] public void SaveVersion_IsOne() { Assert.Equal(1, new UpgradeSlotCollection().SaveVersion); }
}
