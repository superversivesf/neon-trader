using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

public class EngineTests
{
    private Engine CreateValidEngine(string id = "eng_fusion", string name = "Fusion Torch Mk I")
    {
        return new Engine
        {
            Id = id, Name = name, Type = EquipmentType.Engine, Size = EquipmentSize.Medium,
            MountType = MountType.Internal, Rarity = EquipmentRarity.Common, Manufacturer = "DriveCorp",
            Description = "Standard fusion engine", BasePrice = 25000, Mass = 10.0, PowerDraw = 50,
            HeatGeneration = 20, MinimumShipSize = ShipSize.Small, EngineType = EngineType.Fusion,
            MaxThrust = 200000, CruiseThrustRatio = 0.3, FuelEfficiency = 2.0, MaxSpeedMultiplier = 1.5,
            AccelerationMultiplier = 1.3, TurnRateMultiplier = 1.1, WarpRange = 0, WarpCooldown = 60,
            WarpFuelCost = 10, WarpEnergyCost = 100, AfterburnerMultiplier = 2.5, AfterburnerFuelMultiplier = 6.0,
            AfterburnerHeat = 60, ManeuveringThrust = 15000, ReverseThrustRatio = 0.5, RequiredReactorOutput = 80,
            HeatAtMaxThrust = 120, HeatAtCruise = 25, SpoolUpTime = 1.5, SpoolDownTime = 0.8,
            CurrentThrustLevel = 0, IsAfterburnerActive = false, IsWarping = false, WarpChargeProgress = 0,
            WarpChargeTime = 5, TimeSinceWarp = 120, FuelType = "Hydrogen"
        };
    }

    [Fact] public void Validate_ValidEngine_ReturnsTrue() { var e = CreateValidEngine(); Assert.True(e.Validate(out var err)); Assert.Empty(err); }
    [Fact] public void Validate_ZeroMaxThrust_ReturnsFalse() { var e = CreateValidEngine(); e.MaxThrust = 0; Assert.False(e.Validate(out var err)); Assert.Contains("thrust", err); }
    [Fact] public void Validate_NegativeMaxThrust_ReturnsFalse() { var e = CreateValidEngine(); e.MaxThrust = -100; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_CruiseThrustRatioOutOfRange_ReturnsFalse() { var e = CreateValidEngine(); e.CruiseThrustRatio = 1.5; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_NegativeCruiseThrustRatio_ReturnsFalse() { var e = CreateValidEngine(); e.CruiseThrustRatio = -0.1; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_ZeroFuelEfficiency_ReturnsFalse() { var e = CreateValidEngine(); e.FuelEfficiency = 0; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_ZeroMaxSpeedMultiplier_ReturnsFalse() { var e = CreateValidEngine(); e.MaxSpeedMultiplier = 0; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_NegativeWarpRange_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = -1; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_NegativeWarpCooldown_ReturnsFalse() { var e = CreateValidEngine(); e.WarpCooldown = -1; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_NegativeWarpFuelCost_ReturnsFalse() { var e = CreateValidEngine(); e.WarpFuelCost = -1; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_NegativeWarpEnergyCost_ReturnsFalse() { var e = CreateValidEngine(); e.WarpEnergyCost = -1; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_AfterburnerMultiplierTooLow_ReturnsFalse() { var e = CreateValidEngine(); e.AfterburnerMultiplier = 0.5; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_AfterburnerFuelMultiplierTooLow_ReturnsFalse() { var e = CreateValidEngine(); e.AfterburnerFuelMultiplier = 0.5; Assert.False(e.Validate(out var err)); }
    [Fact] public void Validate_NegativeRequiredReactorOutput_ReturnsFalse() { var e = CreateValidEngine(); e.RequiredReactorOutput = -1; Assert.False(e.Validate(out var err)); }

    [Fact] public void GetEffectiveThrust_AtFullThrust_ReturnsMaxThrust() { var e = CreateValidEngine(); e.MaxThrust = 200000; e.CurrentThrustLevel = 1.0; e.Condition = 1.0; Assert.Equal(200000, e.GetEffectiveThrust()); }
    [Fact] public void GetEffectiveThrust_AtCruise_ReturnsCruiseThrust() { var e = CreateValidEngine(); e.MaxThrust = 200000; e.CurrentThrustLevel = 0.3; e.Condition = 1.0; Assert.Equal(60000, e.GetEffectiveThrust()); }
    [Fact] public void GetEffectiveThrust_WithAfterburner_MultipliesThrust() { var e = CreateValidEngine(); e.MaxThrust = 200000; e.CurrentThrustLevel = 1.0; e.AfterburnerMultiplier = 2.5; e.IsAfterburnerActive = true; e.Condition = 1.0; Assert.Equal(500000, e.GetEffectiveThrust()); }
    [Fact] public void GetEffectiveThrust_WithReducedCondition() { var e = CreateValidEngine(); e.MaxThrust = 200000; e.CurrentThrustLevel = 1.0; e.Condition = 0.5; Assert.Equal(100000, e.GetEffectiveThrust()); }
    [Fact] public void GetEffectiveThrust_ZeroThrust_ReturnsZero() { var e = CreateValidEngine(); e.MaxThrust = 200000; e.CurrentThrustLevel = 0; Assert.Equal(0, e.GetEffectiveThrust()); }

    [Fact] public void GetEffectiveFuelConsumption_AtFullThrust() { var e = CreateValidEngine(); e.FuelEfficiency = 2.0; e.CurrentThrustLevel = 1.0; e.Condition = 1.0; Assert.Equal(0.5, e.GetEffectiveFuelConsumption()); }
    [Fact] public void GetEffectiveFuelConsumption_WithAfterburner() { var e = CreateValidEngine(); e.FuelEfficiency = 2.0; e.CurrentThrustLevel = 1.0; e.AfterburnerFuelMultiplier = 6.0; e.IsAfterburnerActive = true; e.Condition = 1.0; Assert.Equal(3.0, e.GetEffectiveFuelConsumption()); }
    [Fact] public void GetEffectiveFuelConsumption_WithReducedCondition() { var e = CreateValidEngine(); e.FuelEfficiency = 2.0; e.CurrentThrustLevel = 1.0; e.Condition = 0.5; Assert.Equal(1.0, e.GetEffectiveFuelConsumption()); }

    [Fact] public void GetEffectiveHeat_AtCruise() { var e = CreateValidEngine(); e.HeatAtCruise = 25; e.HeatAtMaxThrust = 120; e.CurrentThrustLevel = 0.3; e.Condition = 1.0; Assert.Equal(53.5, e.GetEffectiveHeat()); }
    [Fact] public void GetEffectiveHeat_AtMaxThrust() { var e = CreateValidEngine(); e.HeatAtCruise = 25; e.HeatAtMaxThrust = 120; e.CurrentThrustLevel = 1.0; e.Condition = 1.0; Assert.Equal(120, e.GetEffectiveHeat()); }
    [Fact] public void GetEffectiveHeat_WithAfterburner() { var e = CreateValidEngine(); e.HeatAtMaxThrust = 120; e.AfterburnerHeat = 60; e.IsAfterburnerActive = true; e.Condition = 1.0; Assert.Equal(72, e.GetEffectiveHeat()); }
    [Fact] public void GetEffectiveHeat_ZeroThrust() { var e = CreateValidEngine(); e.HeatAtCruise = 25; e.HeatAtMaxThrust = 120; e.CurrentThrustLevel = 0; e.Condition = 1.0; Assert.Equal(25, e.GetEffectiveHeat()); }

    [Fact] public void GetEffectiveMaxSpeed_AppliesMultiplier() { var e = CreateValidEngine(); e.MaxSpeedMultiplier = 1.5; e.Condition = 1.0; Assert.Equal(150, e.GetEffectiveMaxSpeed(100)); }
    [Fact] public void GetEffectiveMaxSpeed_WithReducedCondition() { var e = CreateValidEngine(); e.MaxSpeedMultiplier = 1.5; e.Condition = 0.5; Assert.Equal(75, e.GetEffectiveMaxSpeed(100)); }
    [Fact] public void GetEffectiveAcceleration_AppliesMultiplier() { var e = CreateValidEngine(); e.AccelerationMultiplier = 1.3; e.Condition = 1.0; Assert.Equal(65, e.GetEffectiveAcceleration(50)); }
    [Fact] public void GetEffectiveTurnRate_AppliesMultiplier() { var e = CreateValidEngine(); e.TurnRateMultiplier = 1.1; e.Condition = 1.0; Assert.Equal(99.0, e.GetEffectiveTurnRate(90), 0.001); }

    [Fact] public void SetThrustLevel_ValidValue_SetsCorrectly() { var e = CreateValidEngine(); e.SetThrustLevel(0.7); Assert.Equal(0.7, e.CurrentThrustLevel); }
    [Fact] public void SetThrustLevel_ClampedToZero() { var e = CreateValidEngine(); e.SetThrustLevel(-0.5); Assert.Equal(0, e.CurrentThrustLevel); }
    [Fact] public void SetThrustLevel_ClampedToOne() { var e = CreateValidEngine(); e.SetThrustLevel(1.5); Assert.Equal(1.0, e.CurrentThrustLevel); }

    [Fact] public void ActivateAfterburner_WithSufficientThrust_ReturnsTrue() { var e = CreateValidEngine(); e.CurrentThrustLevel = 0.8; Assert.True(e.ActivateAfterburner()); Assert.True(e.IsAfterburnerActive); }
    [Fact] public void ActivateAfterburner_WithInsufficientThrust_ReturnsFalse() { var e = CreateValidEngine(); e.CurrentThrustLevel = 0.3; Assert.False(e.ActivateAfterburner()); Assert.False(e.IsAfterburnerActive); }
    [Fact] public void ActivateAfterburner_AtBoundary_ReturnsTrue() { var e = CreateValidEngine(); e.CurrentThrustLevel = 0.5; Assert.True(e.ActivateAfterburner()); }
    [Fact] public void DeactivateAfterburner_SetsInactive() { var e = CreateValidEngine(); e.IsAfterburnerActive = true; e.DeactivateAfterburner(); Assert.False(e.IsAfterburnerActive); }

    [Fact] public void HasWarpDrive_WithWarpRange_ReturnsTrue() { var e = CreateValidEngine(); e.WarpRange = 10; Assert.True(e.HasWarpDrive); }
    [Fact] public void HasWarpDrive_WithoutWarpRange_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = 0; Assert.False(e.HasWarpDrive); }
    [Fact] public void StartWarp_WithWarpDrive_ReturnsTrue() { var e = CreateValidEngine(); e.WarpRange = 10; e.TimeSinceWarp = 120; Assert.True(e.StartWarp()); Assert.True(e.IsWarping); Assert.Equal(0, e.WarpChargeProgress); }
    [Fact] public void StartWarp_WithoutWarpDrive_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = 0; Assert.False(e.StartWarp()); }
    [Fact] public void StartWarp_AlreadyWarping_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = 10; e.IsWarping = true; Assert.False(e.StartWarp()); }
    [Fact] public void StartWarp_DuringCooldown_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = 10; e.WarpCooldown = 60; e.TimeSinceWarp = 30; Assert.False(e.StartWarp()); }
    [Fact] public void UpdateWarp_ChargesOverTime() { var e = CreateValidEngine(); e.WarpRange = 10; e.WarpChargeTime = 5; e.IsWarping = true; e.WarpChargeProgress = 0; Assert.False(e.UpdateWarp(2.5, 2000, 200)); Assert.Equal(0.5, e.WarpChargeProgress); }
    [Fact] public void UpdateWarp_CompletesWarp() { var e = CreateValidEngine(); e.WarpRange = 10; e.WarpChargeTime = 5; e.IsWarping = true; e.WarpChargeProgress = 0; Assert.True(e.UpdateWarp(5.0, 2000, 200)); Assert.False(e.IsWarping); Assert.Equal(0, e.WarpChargeProgress); Assert.Equal(0, e.TimeSinceWarp); }
    [Fact] public void UpdateWarp_InsufficientEnergy_CancelsWarp() { var e = CreateValidEngine(); e.WarpRange = 10; e.WarpEnergyCost = 100; e.IsWarping = true; Assert.False(e.UpdateWarp(1.0, 50, 200)); Assert.False(e.IsWarping); }
    [Fact] public void UpdateWarp_InsufficientFuel_CancelsWarp() { var e = CreateValidEngine(); e.WarpRange = 10; e.WarpFuelCost = 10; e.IsWarping = true; Assert.False(e.UpdateWarp(1.0, 2000, 5)); Assert.False(e.IsWarping); }
    [Fact] public void UpdateWarp_NotWarping_ReturnsFalse() { var e = CreateValidEngine(); e.IsWarping = false; Assert.False(e.UpdateWarp(1.0, 2000, 200)); }
    [Fact] public void CancelWarp_ResetsState() { var e = CreateValidEngine(); e.IsWarping = true; e.WarpChargeProgress = 0.7; e.CancelWarp(); Assert.False(e.IsWarping); Assert.Equal(0, e.WarpChargeProgress); }
    [Fact] public void CanWarpDistance_WithinRange_ReturnsTrue() { var e = CreateValidEngine(); e.WarpRange = 10; Assert.True(e.CanWarpDistance(5)); }
    [Fact] public void CanWarpDistance_AtRange_ReturnsTrue() { var e = CreateValidEngine(); e.WarpRange = 10; Assert.True(e.CanWarpDistance(10)); }
    [Fact] public void CanWarpDistance_BeyondRange_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = 10; Assert.False(e.CanWarpDistance(15)); }
    [Fact] public void CanWarpDistance_NoWarpDrive_ReturnsFalse() { var e = CreateValidEngine(); e.WarpRange = 0; Assert.False(e.CanWarpDistance(5)); }
    [Fact] public void GetWarpFuelCost_CalculatesCorrectly() { var e = CreateValidEngine(); e.WarpFuelCost = 10; Assert.Equal(50, e.GetWarpFuelCost(5)); }
    [Fact] public void GetWarpEnergyCost_CalculatesCorrectly() { var e = CreateValidEngine(); e.WarpEnergyCost = 100; Assert.Equal(500, e.GetWarpEnergyCost(5)); }
    [Fact] public void Update_AdvancesTimeSinceWarp() { var e = CreateValidEngine(); e.TimeSinceWarp = 10; e.Update(5.0); Assert.Equal(15, e.TimeSinceWarp); }

    [Fact] public void Clone_CreatesDeepCopy() { var e = CreateValidEngine(); e.Tags.Add("military"); e.StatModifiers["speed"] = 1.2; e.ThrottleEfficiencyCurve[0.5] = 0.9; var clone = (Engine)e.Clone(); Assert.Equal(e.Id, clone.Id); Assert.Equal(e.Name, clone.Name); Assert.Equal(e.EngineType, clone.EngineType); Assert.Equal(e.MaxThrust, clone.MaxThrust); Assert.Equal(e.FuelEfficiency, clone.FuelEfficiency); Assert.Equal(e.MaxSpeedMultiplier, clone.MaxSpeedMultiplier); Assert.Equal(e.WarpRange, clone.WarpRange); Assert.Equal(e.FuelType, clone.FuelType); Assert.Contains("military", clone.Tags); Assert.Equal(1.2, clone.StatModifiers["speed"]); Assert.Equal(0.9, clone.ThrottleEfficiencyCurve[0.5]); }
    [Fact] public void Clone_IsIndependent() { var e = CreateValidEngine(); var clone = (Engine)e.Clone(); clone.MaxThrust = 999999; clone.ThrottleEfficiencyCurve[0.8] = 0.5; Assert.NotEqual(999999, e.MaxThrust); Assert.False(e.ThrottleEfficiencyCurve.ContainsKey(0.8)); }

    [Fact] public void Serialize_ProducesValidJson() { var e = CreateValidEngine(); var json = e.Serialize(); Assert.Equal("eng_fusion", json["id"]!.ToString()); Assert.Equal("Fusion Torch Mk I", json["name"]!.ToString()); Assert.Equal("Engine", json["type"]!.ToString()); Assert.Equal("Fusion", json["engineType"]!.ToString()); Assert.Equal(200000, json["maxThrust"]!.ToObject<double>()); Assert.Equal(0.3, json["cruiseThrustRatio"]!.ToObject<double>()); Assert.Equal(2.0, json["fuelEfficiency"]!.ToObject<double>()); Assert.Equal(1.5, json["maxSpeedMultiplier"]!.ToObject<double>()); Assert.Equal("Hydrogen", json["fuelType"]!.ToString()); }
    [Fact] public void Deserialize_RestoresAllProperties() { var original = CreateValidEngine(); original.Tags.Add("test_tag"); original.ThrottleEfficiencyCurve[0.3] = 0.85; var json = original.Serialize(); var restored = new Engine(); restored.Deserialize(json); Assert.Equal(original.Id, restored.Id); Assert.Equal(original.Name, restored.Name); Assert.Equal(original.EngineType, restored.EngineType); Assert.Equal(original.MaxThrust, restored.MaxThrust); Assert.Equal(original.FuelEfficiency, restored.FuelEfficiency); Assert.Equal(original.FuelType, restored.FuelType); Assert.Contains("test_tag", restored.Tags); Assert.Equal(0.85, restored.ThrottleEfficiencyCurve[0.3]); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var e = CreateValidEngine("eng_rt", "Round Trip"); e.Tags.Add("tag1"); e.ThrottleEfficiencyCurve[0.7] = 0.92; var json = e.Serialize(); var restored = new Engine(); restored.Deserialize(json); Assert.Equal(e.Id, restored.Id); Assert.Equal(e.Name, restored.Name); Assert.Equal(e.EngineType, restored.EngineType); Assert.Contains("tag1", restored.Tags); Assert.Equal(0.92, restored.ThrottleEfficiencyCurve[0.7]); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var e = CreateValidEngine("eng_save", "Save Test"); Assert.Equal("engine_eng_save", e.SaveId); }
    [Fact] public void SaveVersion_IsOne() { Assert.Equal(1, new Engine().SaveVersion); }

    [Fact] public void DefaultValues_AreSensible() { var e = new Engine(); Assert.Equal(EngineType.Chemical, e.EngineType); Assert.Equal(100000, e.MaxThrust); Assert.Equal(0.3, e.CruiseThrustRatio); Assert.Equal(1.0, e.FuelEfficiency); Assert.Equal(1.0, e.MaxSpeedMultiplier); Assert.Equal(1.0, e.AccelerationMultiplier); Assert.Equal(1.0, e.TurnRateMultiplier); Assert.Equal(0, e.WarpRange); Assert.Equal(60, e.WarpCooldown); Assert.Equal(10, e.WarpFuelCost); Assert.Equal(100, e.WarpEnergyCost); Assert.Equal(2.0, e.AfterburnerMultiplier); Assert.Equal(5.0, e.AfterburnerFuelMultiplier); Assert.Equal(50, e.AfterburnerHeat); Assert.Equal(10000, e.ManeuveringThrust); Assert.Equal(0.5, e.ReverseThrustRatio); Assert.Equal(50, e.RequiredReactorOutput); Assert.Equal(100, e.HeatAtMaxThrust); Assert.Equal(20, e.HeatAtCruise); Assert.Equal(1, e.SpoolUpTime); Assert.Equal(0.5, e.SpoolDownTime); Assert.Equal(0, e.CurrentThrustLevel); Assert.False(e.IsAfterburnerActive); Assert.False(e.IsWarping); Assert.Equal(0, e.WarpChargeProgress); Assert.Equal(5, e.WarpChargeTime); Assert.Equal(0, e.TimeSinceWarp); Assert.Equal("Hydrogen", e.FuelType); }

    [Fact] public void EngineType_AllValues_CanBeSet() { foreach (EngineType et in Enum.GetValues<EngineType>()) { var e = CreateValidEngine(); e.EngineType = et; Assert.Equal(et, e.EngineType); } }

    [Fact] public void Validate_CruiseThrustRatioZero_Valid() { var e = CreateValidEngine(); e.CruiseThrustRatio = 0; Assert.True(e.Validate(out _)); }
    [Fact] public void Validate_CruiseThrustRatioOne_Valid() { var e = CreateValidEngine(); e.CruiseThrustRatio = 1.0; Assert.True(e.Validate(out _)); }
    [Fact] public void Validate_ZeroWarpRange_Valid() { var e = CreateValidEngine(); e.WarpRange = 0; Assert.True(e.Validate(out _)); }
    [Fact] public void Validate_ZeroWarpCooldown_Valid() { var e = CreateValidEngine(); e.WarpCooldown = 0; Assert.True(e.Validate(out _)); }
    [Fact] public void Validate_AfterburnerMultiplierOne_Valid() { var e = CreateValidEngine(); e.AfterburnerMultiplier = 1.0; Assert.True(e.Validate(out _)); }
    [Fact] public void Validate_AfterburnerFuelMultiplierOne_Valid() { var e = CreateValidEngine(); e.AfterburnerFuelMultiplier = 1.0; Assert.True(e.Validate(out _)); }
    [Fact] public void Validate_ZeroRequiredReactorOutput_Valid() { var e = CreateValidEngine(); e.RequiredReactorOutput = 0; Assert.True(e.Validate(out _)); }
    [Fact] public void StartWarp_AtCooldownBoundary_ReturnsTrue() { var e = CreateValidEngine(); e.WarpRange = 10; e.WarpCooldown = 60; e.TimeSinceWarp = 60; Assert.True(e.StartWarp()); }
}
