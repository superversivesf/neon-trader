using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

public class WeaponTests
{
    private Weapon CreateValidWeapon(string id = "wpn_laser", string name = "Pulse Laser")
    {
        return new Weapon
        {
            Id = id, Name = name, Type = EquipmentType.Weapon, Size = EquipmentSize.Small,
            MountType = MountType.Hardpoint, Rarity = EquipmentRarity.Common, Manufacturer = "LaserCorp",
            Description = "Standard pulse laser", BasePrice = 5000, Mass = 2.0, PowerDraw = 10,
            HeatGeneration = 5, MinimumShipSize = ShipSize.Tiny, Damage = 25, DamageType = DamageType.Energy,
            Range = 2000, OptimalRange = 1000, FalloffRange = 3000, FireRate = 2.0,
            FireMode = FireMode.Single, BurstCount = 3, BurstDelay = 0.1, EnergyPerShot = 15,
            ProjectileType = ProjectileType.Laser, ProjectileSpeed = 1500, ProjectileLifetime = 3,
            MountSize = WeaponMountSize.Small, Accuracy = 0.95, TrackingSpeed = 90, AmmoCapacity = 0,
            CurrentAmmo = 0, ReloadTime = 0, Spread = 0.3, DamageFalloffPerMeter = 0.001,
            ShieldDamageMultiplier = 1.5, HullDamageMultiplier = 0.8, CritChance = 0.1,
            CritMultiplier = 2.5, StatusEffectChance = 0, StatusEffect = "", StatusEffectDuration = 0,
            RequiredReactorOutput = 20, HeatPerShot = 8
        };
    }

    [Fact] public void Validate_ValidWeapon_ReturnsTrue() { var w = CreateValidWeapon(); Assert.True(w.Validate(out var e)); Assert.Empty(e); }
    [Fact] public void Validate_ZeroDamage_ReturnsFalse() { var w = CreateValidWeapon(); w.Damage = 0; Assert.False(w.Validate(out var e)); Assert.Contains("Damage", e); }
    [Fact] public void Validate_NegativeDamage_ReturnsFalse() { var w = CreateValidWeapon(); w.Damage = -5; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_ZeroRange_ReturnsFalse() { var w = CreateValidWeapon(); w.Range = 0; Assert.False(w.Validate(out var e)); Assert.Contains("Range", e); }
    [Fact] public void Validate_OptimalRangeGreaterThanRange_ReturnsFalse() { var w = CreateValidWeapon(); w.Range = 1000; w.OptimalRange = 1500; Assert.False(w.Validate(out var e)); Assert.Contains("Optimal", e); }
    [Fact] public void Validate_FalloffRangeLessThanOptimal_ReturnsFalse() { var w = CreateValidWeapon(); w.OptimalRange = 1000; w.FalloffRange = 500; Assert.False(w.Validate(out var e)); Assert.Contains("Falloff", e); }
    [Fact] public void Validate_ZeroFireRate_ReturnsFalse() { var w = CreateValidWeapon(); w.FireRate = 0; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_NegativeEnergyPerShot_ReturnsFalse() { var w = CreateValidWeapon(); w.EnergyPerShot = -1; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_ZeroProjectileSpeed_ReturnsFalse() { var w = CreateValidWeapon(); w.ProjectileSpeed = 0; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_AccuracyOutOfRange_ReturnsFalse() { var w = CreateValidWeapon(); w.Accuracy = 1.5; Assert.False(w.Validate(out var e)); Assert.Contains("Accuracy", e); }
    [Fact] public void Validate_NegativeAccuracy_ReturnsFalse() { var w = CreateValidWeapon(); w.Accuracy = -0.1; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_NegativeAmmoCapacity_ReturnsFalse() { var w = CreateValidWeapon(); w.AmmoCapacity = -1; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_CurrentAmmoExceedsCapacity_ReturnsFalse() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 15; Assert.False(w.Validate(out var e)); Assert.Contains("ammo", e); }
    [Fact] public void Validate_NegativeReloadTime_ReturnsFalse() { var w = CreateValidWeapon(); w.ReloadTime = -1; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_CritChanceOutOfRange_ReturnsFalse() { var w = CreateValidWeapon(); w.CritChance = 1.5; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_CritMultiplierTooLow_ReturnsFalse() { var w = CreateValidWeapon(); w.CritMultiplier = 1.0; Assert.False(w.Validate(out var e)); }
    [Fact] public void Validate_CritMultiplierZero_ReturnsFalse() { var w = CreateValidWeapon(); w.CritMultiplier = 0; Assert.False(w.Validate(out var e)); }

    [Fact] public void DPS_CalculatesCorrectly() { var w = CreateValidWeapon(); w.Damage = 25; w.FireRate = 2.0; Assert.Equal(50, w.DPS); }
    [Fact] public void DPS_WithHighFireRate() { var w = CreateValidWeapon(); w.Damage = 10; w.FireRate = 10.0; Assert.Equal(100, w.DPS); }

    [Fact] public void GetDamageAtRange_WithinOptimal_ReturnsFullDamage() { var w = CreateValidWeapon(); w.Damage = 25; w.OptimalRange = 1000; Assert.Equal(25, w.GetDamageAtRange(500)); }
    [Fact] public void GetDamageAtRange_AtOptimalBoundary_ReturnsFullDamage() { var w = CreateValidWeapon(); w.Damage = 25; w.OptimalRange = 1000; Assert.Equal(25, w.GetDamageAtRange(1000)); }
    [Fact] public void GetDamageAtRange_BeyondFalloff_ReturnsZero() { var w = CreateValidWeapon(); w.Damage = 25; w.FalloffRange = 3000; Assert.Equal(0, w.GetDamageAtRange(3000)); }
    [Fact] public void GetDamageAtRange_BeyondFalloffFar_ReturnsZero() { var w = CreateValidWeapon(); w.Damage = 25; w.FalloffRange = 3000; Assert.Equal(0, w.GetDamageAtRange(5000)); }
    [Fact] public void GetDamageAtRange_MidFalloff_ReturnsReducedDamage() { var w = CreateValidWeapon(); w.Damage = 100; w.OptimalRange = 500; w.FalloffRange = 1500; Assert.Equal(50, w.GetDamageAtRange(1000)); }
    [Fact] public void GetDPSAtRange_WithinOptimal_ReturnsFullDPS() { var w = CreateValidWeapon(); w.Damage = 25; w.FireRate = 2.0; w.OptimalRange = 1000; Assert.Equal(50, w.GetDPSAtRange(500)); }
    [Fact] public void GetDPSAtRange_BeyondFalloff_ReturnsZero() { var w = CreateValidWeapon(); w.FalloffRange = 3000; Assert.Equal(0, w.GetDPSAtRange(3000)); }

    [Fact] public void IsInRange_WithinRange_ReturnsTrue() { var w = CreateValidWeapon(); w.Range = 2000; Assert.True(w.IsInRange(1500)); }
    [Fact] public void IsInRange_AtRangeBoundary_ReturnsTrue() { var w = CreateValidWeapon(); w.Range = 2000; Assert.True(w.IsInRange(2000)); }
    [Fact] public void IsInRange_BeyondRange_ReturnsFalse() { var w = CreateValidWeapon(); w.Range = 2000; Assert.False(w.IsInRange(2001)); }
    [Fact] public void IsInOptimalRange_WithinOptimal_ReturnsTrue() { var w = CreateValidWeapon(); w.OptimalRange = 1000; Assert.True(w.IsInOptimalRange(500)); }
    [Fact] public void IsInOptimalRange_BeyondOptimal_ReturnsFalse() { var w = CreateValidWeapon(); w.OptimalRange = 1000; Assert.False(w.IsInOptimalRange(1500)); }

    [Fact] public void Fire_EnergyWeapon_ReturnsTrue() { var w = CreateValidWeapon(); w.AmmoCapacity = 0; Assert.True(w.Fire()); }
    [Fact] public void Fire_AmmoWeapon_ConsumesAmmo() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 10; Assert.True(w.Fire()); Assert.Equal(9, w.CurrentAmmo); }
    [Fact] public void Fire_OutOfAmmo_StartsReload() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 0; Assert.False(w.Fire()); Assert.True(w.IsReloading); Assert.Equal(0, w.ReloadProgress); }
    [Fact] public void Fire_WhileReloading_ReturnsFalse() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 5; w.IsReloading = true; Assert.False(w.Fire()); Assert.Equal(5, w.CurrentAmmo); }
    [Fact] public void Fire_MultipleShots_ConsumesAmmo() { var w = CreateValidWeapon(); w.AmmoCapacity = 5; w.CurrentAmmo = 5; Assert.True(w.Fire()); Assert.True(w.Fire()); Assert.True(w.Fire()); Assert.Equal(2, w.CurrentAmmo); }
    [Fact] public void Fire_LastShot_TriggersReload() { var w = CreateValidWeapon(); w.AmmoCapacity = 3; w.CurrentAmmo = 1; Assert.True(w.Fire()); Assert.Equal(0, w.CurrentAmmo); Assert.False(w.Fire()); Assert.True(w.IsReloading); }

    [Fact] public void StartReload_WithAmmoCapacity_SetsReloading() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 5; w.StartReload(); Assert.True(w.IsReloading); Assert.Equal(0, w.ReloadProgress); }
    [Fact] public void StartReload_EnergyWeapon_DoesNotReload() { var w = CreateValidWeapon(); w.AmmoCapacity = 0; w.StartReload(); Assert.False(w.IsReloading); }
    [Fact] public void StartReload_AlreadyFull_DoesNotReload() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 10; w.StartReload(); Assert.False(w.IsReloading); }
    [Fact] public void UpdateReload_ProgressesOverTime() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 0; w.ReloadTime = 5; w.StartReload(); w.UpdateReload(2.5); Assert.True(w.IsReloading); Assert.Equal(0.5, w.ReloadProgress); }
    [Fact] public void UpdateReload_CompletesReload() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 0; w.ReloadTime = 5; w.StartReload(); w.UpdateReload(5.0); Assert.False(w.IsReloading); Assert.Equal(0, w.ReloadProgress); Assert.Equal(10, w.CurrentAmmo); }
    [Fact] public void UpdateReload_ExcessTime_CompletesReload() { var w = CreateValidWeapon(); w.AmmoCapacity = 10; w.CurrentAmmo = 0; w.ReloadTime = 3; w.StartReload(); w.UpdateReload(10.0); Assert.False(w.IsReloading); Assert.Equal(0, w.ReloadProgress); Assert.Equal(10, w.CurrentAmmo); }
    [Fact] public void InstantReload_FillsAmmo() { var w = CreateValidWeapon(); w.AmmoCapacity = 20; w.CurrentAmmo = 3; w.IsReloading = true; w.ReloadProgress = 0.7; w.InstantReload(); Assert.Equal(20, w.CurrentAmmo); Assert.False(w.IsReloading); Assert.Equal(0, w.ReloadProgress); }

    [Fact] public void Clone_CreatesDeepCopy() { var w = CreateValidWeapon(); w.Tags.Add("military"); w.StatModifiers["damage"] = 1.1; var clone = (Weapon)w.Clone(); Assert.Equal(w.Id, clone.Id); Assert.Equal(w.Name, clone.Name); Assert.Equal(w.Damage, clone.Damage); Assert.Equal(w.DamageType, clone.DamageType); Assert.Equal(w.Range, clone.Range); Assert.Equal(w.FireRate, clone.FireRate); Assert.Equal(w.Accuracy, clone.Accuracy); Assert.Equal(w.ShieldDamageMultiplier, clone.ShieldDamageMultiplier); Assert.Equal(w.HullDamageMultiplier, clone.HullDamageMultiplier); Assert.Equal(w.CritChance, clone.CritChance); Assert.Equal(w.CritMultiplier, clone.CritMultiplier); Assert.Contains("military", clone.Tags); Assert.Equal(1.1, clone.StatModifiers["damage"]); }
    [Fact] public void Clone_IsIndependent() { var w = CreateValidWeapon(); var clone = (Weapon)w.Clone(); clone.Damage = 999; clone.Tags.Add("clone_tag"); Assert.NotEqual(999, w.Damage); Assert.DoesNotContain("clone_tag", w.Tags); }

    [Fact] public void Serialize_ProducesValidJson() { var w = CreateValidWeapon(); var json = w.Serialize(); Assert.Equal("wpn_laser", json["id"]!.ToString()); Assert.Equal("Pulse Laser", json["name"]!.ToString()); Assert.Equal("Weapon", json["type"]!.ToString()); Assert.Equal(25, json["damage"]!.ToObject<int>()); Assert.Equal("Energy", json["damageType"]!.ToString()); Assert.Equal(2000, json["range"]!.ToObject<double>()); Assert.Equal(2.0, json["fireRate"]!.ToObject<double>()); Assert.Equal(0.95, json["accuracy"]!.ToObject<double>()); }
    [Fact] public void Deserialize_RestoresAllProperties() { var original = CreateValidWeapon(); original.Tags.Add("test_tag"); original.StatModifiers["range"] = 1.2; var json = original.Serialize(); var restored = new Weapon(); restored.Deserialize(json); Assert.Equal(original.Id, restored.Id); Assert.Equal(original.Name, restored.Name); Assert.Equal(original.Damage, restored.Damage); Assert.Equal(original.DamageType, restored.DamageType); Assert.Equal(original.Range, restored.Range); Assert.Equal(original.FireRate, restored.FireRate); Assert.Equal(original.Accuracy, restored.Accuracy); Assert.Contains("test_tag", restored.Tags); Assert.Equal(1.2, restored.StatModifiers["range"]); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var w = CreateValidWeapon("wpn_rt", "Round Trip"); w.Tags.Add("tag1"); w.StatModifiers["speed"] = 0.9; var json = w.Serialize(); var restored = new Weapon(); restored.Deserialize(json); Assert.Equal(w.Id, restored.Id); Assert.Equal(w.Name, restored.Name); Assert.Equal(w.Damage, restored.Damage); Assert.Contains("tag1", restored.Tags); Assert.Equal(0.9, restored.StatModifiers["speed"]); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var w = CreateValidWeapon("wpn_save", "Save Test"); Assert.Equal("weapon_wpn_save", w.SaveId); }
    [Fact] public void SaveVersion_IsTwo() { Assert.Equal(2, new Weapon().SaveVersion); }

    [Fact] public void DefaultValues_AreSensible() { var w = new Weapon(); Assert.Equal(10, w.Damage); Assert.Equal(DamageType.Kinetic, w.DamageType); Assert.Equal(1000, w.Range); Assert.Equal(500, w.OptimalRange); Assert.Equal(1500, w.FalloffRange); Assert.Equal(1.0, w.FireRate); Assert.Equal(FireMode.Single, w.FireMode); Assert.Equal(3, w.BurstCount); Assert.Equal(0.1, w.BurstDelay); Assert.Equal(10, w.EnergyPerShot); Assert.Equal(ProjectileType.Bullet, w.ProjectileType); Assert.Equal(500, w.ProjectileSpeed); Assert.Equal(5, w.ProjectileLifetime); Assert.Equal(WeaponMountSize.Small, w.MountSize); Assert.Equal(0.9, w.Accuracy); Assert.Equal(60, w.TrackingSpeed); Assert.Equal(0, w.AmmoCapacity); Assert.Equal(0, w.CurrentAmmo); Assert.Equal(5, w.ReloadTime); Assert.Equal(0.5, w.Spread); Assert.Equal(1.0, w.ShieldDamageMultiplier); Assert.Equal(1.0, w.HullDamageMultiplier); Assert.Equal(0.05, w.CritChance); Assert.Equal(2.0, w.CritMultiplier); }

    [Fact] public void DamageType_AllValues_CanBeSet() { foreach (DamageType dt in Enum.GetValues<DamageType>()) { var w = CreateValidWeapon(); w.DamageType = dt; Assert.Equal(dt, w.DamageType); } }
    [Fact] public void FireMode_AllValues_CanBeSet() { foreach (FireMode fm in Enum.GetValues<FireMode>()) { var w = CreateValidWeapon(); w.FireMode = fm; Assert.Equal(fm, w.FireMode); } }
    [Fact] public void ProjectileType_AllValues_CanBeSet() { foreach (ProjectileType pt in Enum.GetValues<ProjectileType>()) { var w = CreateValidWeapon(); w.ProjectileType = pt; Assert.Equal(pt, w.ProjectileType); } }
    [Fact] public void WeaponMountSize_AllValues_CanBeSet() { foreach (WeaponMountSize ms in Enum.GetValues<WeaponMountSize>()) { var w = CreateValidWeapon(); w.MountSize = ms; Assert.Equal(ms, w.MountSize); } }

    [Fact] public void Validate_AccuracyZero_Valid() { var w = CreateValidWeapon(); w.Accuracy = 0; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_AccuracyOne_Valid() { var w = CreateValidWeapon(); w.Accuracy = 1.0; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_CritChanceZero_Valid() { var w = CreateValidWeapon(); w.CritChance = 0; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_CritChanceOne_Valid() { var w = CreateValidWeapon(); w.CritChance = 1.0; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_ZeroAmmoCapacity_Valid() { var w = CreateValidWeapon(); w.AmmoCapacity = 0; w.CurrentAmmo = 0; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_ZeroReloadTime_Valid() { var w = CreateValidWeapon(); w.ReloadTime = 0; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_OptimalRangeEqualsRange_Valid() { var w = CreateValidWeapon(); w.Range = 1000; w.OptimalRange = 1000; w.FalloffRange = 1000; Assert.True(w.Validate(out _)); }
    [Fact] public void Validate_StatusEffectChanceOne_Valid() { var w = CreateValidWeapon(); w.StatusEffectChance = 1.0; w.StatusEffect = "burn"; w.StatusEffectDuration = 5; Assert.True(w.Validate(out _)); }
}
