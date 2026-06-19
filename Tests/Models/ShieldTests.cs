using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

public class ShieldTests
{
    private Shield CreateValidShield(string id = "shld_std", string name = "Standard Shield Gen")
    {
        return new Shield
        {
            Id = id, Name = name, Type = EquipmentType.Shield, Size = EquipmentSize.Medium,
            MountType = MountType.Internal, Rarity = EquipmentRarity.Common, Manufacturer = "ShieldCorp",
            Description = "Standard energy shield", BasePrice = 10000, Mass = 5.0, PowerDraw = 15,
            HeatGeneration = 2, MinimumShipSize = ShipSize.Small, Capacity = 200, RechargeRate = 20,
            RechargeDelay = 3, DamageResistance = 0.1, KineticResistance = 0.15, EnergyResistance = 0.2,
            ExplosiveResistance = 0.05, ThermalResistance = 0.1, EMPResistance = 0.3,
            ShieldType = ShieldType.Standard, EnergyPerSecond = 5, EnergyPerDamage = 0.1,
            IsActive = true, CurrentCharge = 200, TimeSinceDamage = 5, Efficiency = 1.0,
            RequiredReactorOutput = 10, HeatPerSecond = 2, HeatPerDamage = 0.5, RechargeBoost = 1.0
        };
    }

    [Fact] public void Validate_ValidShield_ReturnsTrue() { var s = CreateValidShield(); Assert.True(s.Validate(out var e)); Assert.Empty(e); }
    [Fact] public void Validate_ZeroCapacity_ReturnsFalse() { var s = CreateValidShield(); s.Capacity = 0; Assert.False(s.Validate(out var e)); Assert.Contains("capacity", e); }
    [Fact] public void Validate_NegativeCapacity_ReturnsFalse() { var s = CreateValidShield(); s.Capacity = -10; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_NegativeRechargeRate_ReturnsFalse() { var s = CreateValidShield(); s.RechargeRate = -1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_NegativeRechargeDelay_ReturnsFalse() { var s = CreateValidShield(); s.RechargeDelay = -1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_DamageResistanceOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.DamageResistance = 1.5; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_KineticResistanceOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.KineticResistance = -0.1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_EnergyResistanceOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.EnergyResistance = 1.5; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_ExplosiveResistanceOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.ExplosiveResistance = -0.5; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_ThermalResistanceOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.ThermalResistance = 2.0; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_EMPResistanceOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.EMPResistance = -0.1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_NegativeEnergyPerSecond_ReturnsFalse() { var s = CreateValidShield(); s.EnergyPerSecond = -1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_NegativeEnergyPerDamage_ReturnsFalse() { var s = CreateValidShield(); s.EnergyPerDamage = -0.1; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_CurrentChargeExceedsCapacity_ReturnsFalse() { var s = CreateValidShield(); s.Capacity = 100; s.CurrentCharge = 150; Assert.False(s.Validate(out var e)); Assert.Contains("charge", e); }
    [Fact] public void Validate_NegativeCurrentCharge_ReturnsFalse() { var s = CreateValidShield(); s.CurrentCharge = -5; Assert.False(s.Validate(out var e)); }
    [Fact] public void Validate_EfficiencyOutOfRange_ReturnsFalse() { var s = CreateValidShield(); s.Efficiency = 1.5; Assert.False(s.Validate(out var e)); }

    [Fact] public void EffectiveCapacity_CalculatesCorrectly() { var s = CreateValidShield(); s.Capacity = 200; s.Condition = 1.0; s.Efficiency = 1.0; Assert.Equal(200, s.EffectiveCapacity); }
    [Fact] public void EffectiveCapacity_WithReducedCondition() { var s = CreateValidShield(); s.Capacity = 200; s.Condition = 0.5; s.Efficiency = 1.0; Assert.Equal(100, s.EffectiveCapacity); }
    [Fact] public void EffectiveCapacity_WithReducedEfficiency() { var s = CreateValidShield(); s.Capacity = 200; s.Condition = 1.0; s.Efficiency = 0.8; Assert.Equal(160, s.EffectiveCapacity); }
    [Fact] public void EffectiveRechargeRate_CalculatesCorrectly() { var s = CreateValidShield(); s.RechargeRate = 20; s.Condition = 1.0; s.Efficiency = 1.0; Assert.Equal(20, s.EffectiveRechargeRate); }
    [Fact] public void EffectiveRechargeRate_WithReducedCondition() { var s = CreateValidShield(); s.RechargeRate = 20; s.Condition = 0.5; s.Efficiency = 1.0; Assert.Equal(10, s.EffectiveRechargeRate); }

    [Fact] public void GetResistance_Kinetic_ReturnsKineticResistance() { var s = CreateValidShield(); s.KineticResistance = 0.3; Assert.Equal(0.3, s.GetResistance(DamageType.Kinetic)); }
    [Fact] public void GetResistance_Energy_ReturnsEnergyResistance() { var s = CreateValidShield(); s.EnergyResistance = 0.4; Assert.Equal(0.4, s.GetResistance(DamageType.Energy)); }
    [Fact] public void GetResistance_Explosive_ReturnsExplosiveResistance() { var s = CreateValidShield(); s.ExplosiveResistance = 0.2; Assert.Equal(0.2, s.GetResistance(DamageType.Explosive)); }
    [Fact] public void GetResistance_Thermal_ReturnsThermalResistance() { var s = CreateValidShield(); s.ThermalResistance = 0.25; Assert.Equal(0.25, s.GetResistance(DamageType.Thermal)); }
    [Fact] public void GetResistance_EMP_ReturnsEMPResistance() { var s = CreateValidShield(); s.EMPResistance = 0.5; Assert.Equal(0.5, s.GetResistance(DamageType.EMP)); }
    [Fact] public void GetResistance_UnknownType_ReturnsBaseResistance() { var s = CreateValidShield(); s.DamageResistance = 0.15; Assert.Equal(0.15, s.GetResistance(DamageType.Quantum)); }
    [Fact] public void GetResistance_WithHarmonicBonus() { var s = CreateValidShield(); s.KineticResistance = 0.2; s.HarmonicBonuses[DamageType.Kinetic] = 0.1; Assert.Equal(0.3, s.GetResistance(DamageType.Kinetic), 0.0001); }
    [Fact] public void GetResistance_ClampedToOne() { var s = CreateValidShield(); s.KineticResistance = 0.8; s.HarmonicBonuses[DamageType.Kinetic] = 0.5; Assert.Equal(1.0, s.GetResistance(DamageType.Kinetic)); }
    [Fact] public void GetResistance_WithReducedCondition() { var s = CreateValidShield(); s.KineticResistance = 0.4; s.Condition = 0.5; Assert.Equal(0.2, s.GetResistance(DamageType.Kinetic)); }

    [Fact] public void AbsorbDamage_ActiveShield_ReducesDamage() { var s = CreateValidShield(); s.CurrentCharge = 200; s.KineticResistance = 0.2; var remaining = s.AbsorbDamage(100, DamageType.Kinetic); Assert.Equal(20, remaining); Assert.Equal(120, s.CurrentCharge); Assert.Equal(0, s.TimeSinceDamage); }
    [Fact] public void AbsorbDamage_InactiveShield_PassesAllDamage() { var s = CreateValidShield(); s.IsActive = false; s.CurrentCharge = 200; var remaining = s.AbsorbDamage(100, DamageType.Kinetic); Assert.Equal(100, remaining); Assert.Equal(200, s.CurrentCharge); }
    [Fact] public void AbsorbDamage_BrokenShield_PassesAllDamage() { var s = CreateValidShield(); s.CurrentCharge = 0; var remaining = s.AbsorbDamage(100, DamageType.Kinetic); Assert.Equal(100, remaining); }
    [Fact] public void AbsorbDamage_OverwhelmsShield() { var s = CreateValidShield(); s.CurrentCharge = 50; s.DamageResistance = 0; var remaining = s.AbsorbDamage(100, DamageType.Kinetic); Assert.Equal(50, remaining); Assert.Equal(0, s.CurrentCharge); Assert.True(s.IsBroken); }
    [Fact] public void AbsorbDamage_ShieldBreak_PenalizesRechargeDelay() { var s = CreateValidShield(); s.CurrentCharge = 30; s.RechargeDelay = 3; s.DamageResistance = 0; var remaining = s.AbsorbDamage(100, DamageType.Kinetic); Assert.True(s.IsBroken); Assert.True(s.TimeSinceDamage < 0); }
    [Fact] public void AbsorbDamage_WithHighResistance_MitigatesMost() { var s = CreateValidShield(); s.CurrentCharge = 500; s.KineticResistance = 0.9; var remaining = s.AbsorbDamage(100, DamageType.Kinetic); Assert.True(remaining >= 89 && remaining <= 91); Assert.True(s.CurrentCharge >= 489 && s.CurrentCharge <= 491); }
    [Fact] public void AbsorbDamage_ResetsTimeSinceDamage() { var s = CreateValidShield(); s.TimeSinceDamage = 10; s.AbsorbDamage(10, DamageType.Kinetic); Assert.Equal(0, s.TimeSinceDamage); }

    [Fact] public void Recharge_AfterDelay_RechargesShield() { var s = CreateValidShield(); s.CurrentCharge = 100; s.RechargeRate = 20; s.RechargeDelay = 3; s.TimeSinceDamage = 5; s.EnergyPerSecond = 1; s.Recharge(1.0, 100); Assert.Equal(120, s.CurrentCharge); }
    [Fact] public void Recharge_DuringDelay_DoesNotRecharge() { var s = CreateValidShield(); s.CurrentCharge = 100; s.RechargeRate = 20; s.RechargeDelay = 3; s.TimeSinceDamage = 1; s.Recharge(1.0, 100); Assert.Equal(100, s.CurrentCharge); }
    [Fact] public void Recharge_InactiveShield_DoesNotRecharge() { var s = CreateValidShield(); s.IsActive = false; s.CurrentCharge = 100; s.TimeSinceDamage = 5; s.Recharge(1.0, 100); Assert.Equal(100, s.CurrentCharge); }
    [Fact] public void Recharge_BrokenShield_DoesNotRecharge() { var s = CreateValidShield(); s.CurrentCharge = 0; s.TimeSinceDamage = 5; s.Recharge(1.0, 100); Assert.Equal(0, s.CurrentCharge); }
    [Fact] public void Recharge_CappedAtEffectiveCapacity() { var s = CreateValidShield(); s.CurrentCharge = 195; s.RechargeRate = 20; s.RechargeDelay = 0; s.TimeSinceDamage = 5; s.EnergyPerSecond = 1; s.Recharge(1.0, 100); Assert.Equal(200, s.CurrentCharge); }
    [Fact] public void Recharge_InsufficientEnergy_ReducedRecharge() { var s = CreateValidShield(); s.CurrentCharge = 100; s.RechargeRate = 20; s.RechargeDelay = 0; s.TimeSinceDamage = 5; s.EnergyPerSecond = 10; s.Recharge(1.0, 50); Assert.Equal(105, s.CurrentCharge); }
    [Fact] public void Recharge_WithBoost_MultipliesRecharge() { var s = CreateValidShield(); s.CurrentCharge = 100; s.RechargeRate = 20; s.RechargeDelay = 0; s.TimeSinceDamage = 5; s.RechargeBoost = 2.0; s.EnergyPerSecond = 1; s.Recharge(1.0, 100); Assert.Equal(140, s.CurrentCharge); }
    [Fact] public void Recharge_AdvancesTimeSinceDamage() { var s = CreateValidShield(); s.TimeSinceDamage = 5; s.RechargeDelay = 0; s.EnergyPerSecond = 1; s.Recharge(2.0, 100); Assert.Equal(7, s.TimeSinceDamage); }

    [Fact] public void Activate_SetsActive() { var s = CreateValidShield(); s.IsActive = false; s.Activate(); Assert.True(s.IsActive); }
    [Fact] public void Activate_WithZeroCharge_SetsMinimumCharge() { var s = CreateValidShield(); s.CurrentCharge = 0; s.IsActive = false; s.Activate(); Assert.True(s.IsActive); Assert.Equal(1, s.CurrentCharge); }
    [Fact] public void Deactivate_SetsInactive() { var s = CreateValidShield(); s.IsActive = true; s.Deactivate(); Assert.False(s.IsActive); }
    [Fact] public void FullRestore_FillsCharge() { var s = CreateValidShield(); s.CurrentCharge = 50; s.RechargeDelay = 3; s.FullRestore(); Assert.Equal(200, s.CurrentCharge); Assert.Equal(3, s.TimeSinceDamage); }

    [Fact] public void ChargePercentage_Full_ReturnsOne() { var s = CreateValidShield(); s.CurrentCharge = 200; Assert.Equal(1.0, s.ChargePercentage); }
    [Fact] public void ChargePercentage_Half_ReturnsHalf() { var s = CreateValidShield(); s.CurrentCharge = 100; Assert.Equal(0.5, s.ChargePercentage); }
    [Fact] public void ChargePercentage_Zero_ReturnsZero() { var s = CreateValidShield(); s.CurrentCharge = 0; Assert.Equal(0, s.ChargePercentage); }
    [Fact] public void ChargePercentage_ZeroCapacity_ReturnsZero() { var s = CreateValidShield(); s.Capacity = 0; s.CurrentCharge = 0; Assert.Equal(0, s.ChargePercentage); }

    [Fact] public void IsBroken_ZeroCharge_ReturnsTrue() { var s = CreateValidShield(); s.CurrentCharge = 0; Assert.True(s.IsBroken); }
    [Fact] public void IsBroken_PositiveCharge_ReturnsFalse() { var s = CreateValidShield(); s.CurrentCharge = 1; Assert.False(s.IsBroken); }

    [Fact] public void Clone_CreatesDeepCopy() { var s = CreateValidShield(); s.Tags.Add("military"); s.StatModifiers["capacity"] = 1.2; s.HarmonicBonuses[DamageType.Energy] = 0.15; var clone = (Shield)s.Clone(); Assert.Equal(s.Id, clone.Id); Assert.Equal(s.Name, clone.Name); Assert.Equal(s.Capacity, clone.Capacity); Assert.Equal(s.RechargeRate, clone.RechargeRate); Assert.Equal(s.ShieldType, clone.ShieldType); Assert.Equal(s.IsActive, clone.IsActive); Assert.Equal(s.CurrentCharge, clone.CurrentCharge); Assert.Contains("military", clone.Tags); Assert.Equal(1.2, clone.StatModifiers["capacity"]); Assert.Equal(0.15, clone.HarmonicBonuses[DamageType.Energy]); }
    [Fact] public void Clone_IsIndependent() { var s = CreateValidShield(); var clone = (Shield)s.Clone(); clone.Capacity = 999; clone.HarmonicBonuses[DamageType.Kinetic] = 0.5; Assert.NotEqual(999, s.Capacity); Assert.False(s.HarmonicBonuses.ContainsKey(DamageType.Kinetic)); }

    [Fact] public void Serialize_ProducesValidJson() { var s = CreateValidShield(); var json = s.Serialize(); Assert.Equal("shld_std", json["id"]!.ToString()); Assert.Equal("Standard Shield Gen", json["name"]!.ToString()); Assert.Equal("Shield", json["type"]!.ToString()); Assert.Equal(200, json["capacity"]!.ToObject<int>()); Assert.Equal(20, json["rechargeRate"]!.ToObject<double>()); Assert.Equal(3, json["rechargeDelay"]!.ToObject<double>()); Assert.Equal("Standard", json["shieldType"]!.ToString()); Assert.True(json["isActive"]!.ToObject<bool>()); Assert.Equal(200, json["currentCharge"]!.ToObject<int>()); }
    [Fact] public void Deserialize_RestoresAllProperties() { var original = CreateValidShield(); original.Tags.Add("test_tag"); original.HarmonicBonuses[DamageType.Energy] = 0.1; var json = original.Serialize(); var restored = new Shield(); restored.Deserialize(json); Assert.Equal(original.Id, restored.Id); Assert.Equal(original.Name, restored.Name); Assert.Equal(original.Capacity, restored.Capacity); Assert.Equal(original.RechargeRate, restored.RechargeRate); Assert.Equal(original.ShieldType, restored.ShieldType); Assert.Equal(original.IsActive, restored.IsActive); Assert.Equal(original.CurrentCharge, restored.CurrentCharge); Assert.Contains("test_tag", restored.Tags); Assert.Equal(0.1, restored.HarmonicBonuses[DamageType.Energy]); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var s = CreateValidShield("shld_rt", "Round Trip"); s.Tags.Add("tag1"); s.HarmonicBonuses[DamageType.Thermal] = 0.2; var json = s.Serialize(); var restored = new Shield(); restored.Deserialize(json); Assert.Equal(s.Id, restored.Id); Assert.Equal(s.Name, restored.Name); Assert.Equal(s.Capacity, restored.Capacity); Assert.Contains("tag1", restored.Tags); Assert.Equal(0.2, restored.HarmonicBonuses[DamageType.Thermal]); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var s = CreateValidShield("shld_save", "Save Test"); Assert.Equal("shield_shld_save", s.SaveId); }
    [Fact] public void SaveVersion_IsOne() { Assert.Equal(1, new Shield().SaveVersion); }

    [Fact] public void DefaultValues_AreSensible() { var s = new Shield(); Assert.Equal(100, s.Capacity); Assert.Equal(10, s.RechargeRate); Assert.Equal(3, s.RechargeDelay); Assert.Equal(0, s.DamageResistance); Assert.Equal(0, s.KineticResistance); Assert.Equal(0, s.EnergyResistance); Assert.Equal(0, s.ExplosiveResistance); Assert.Equal(0, s.ThermalResistance); Assert.Equal(0, s.EMPResistance); Assert.Equal(ShieldType.Standard, s.ShieldType); Assert.Equal(5, s.EnergyPerSecond); Assert.Equal(0.1, s.EnergyPerDamage); Assert.True(s.IsActive); Assert.Equal(100, s.CurrentCharge); Assert.Equal(0, s.TimeSinceDamage); Assert.Equal(1.0, s.Efficiency); Assert.Equal(10, s.RequiredReactorOutput); Assert.Equal(2, s.HeatPerSecond); Assert.Equal(0.5, s.HeatPerDamage); Assert.Equal(1.0, s.RechargeBoost); }

    [Fact] public void ShieldType_AllValues_CanBeSet() { foreach (ShieldType st in Enum.GetValues<ShieldType>()) { var s = CreateValidShield(); s.ShieldType = st; Assert.Equal(st, s.ShieldType); } }

    [Fact] public void Validate_ZeroRechargeRate_Valid() { var s = CreateValidShield(); s.RechargeRate = 0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_ZeroRechargeDelay_Valid() { var s = CreateValidShield(); s.RechargeDelay = 0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_AllResistancesAtOne_Valid() { var s = CreateValidShield(); s.DamageResistance = 1.0; s.KineticResistance = 1.0; s.EnergyResistance = 1.0; s.ExplosiveResistance = 1.0; s.ThermalResistance = 1.0; s.EMPResistance = 1.0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_AllResistancesAtZero_Valid() { var s = CreateValidShield(); s.DamageResistance = 0; s.KineticResistance = 0; s.EnergyResistance = 0; s.ExplosiveResistance = 0; s.ThermalResistance = 0; s.EMPResistance = 0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_EfficiencyZero_Valid() { var s = CreateValidShield(); s.Efficiency = 0; s.CurrentCharge = 0; Assert.True(s.Validate(out _)); }
    [Fact] public void Validate_EfficiencyOne_Valid() { var s = CreateValidShield(); s.Efficiency = 1.0; Assert.True(s.Validate(out _)); }
}
