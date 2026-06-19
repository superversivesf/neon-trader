using NeonTrader.Models;
using Xunit;

namespace NeonTrader.Tests.Models;

public class CargoHoldTests
{
    private CargoHold CreateValidCargoHold(string id = "ch_std", string name = "Standard Cargo Bay")
    {
        return new CargoHold
        {
            Id = id, Name = name, Type = EquipmentType.CargoExpansion, Size = EquipmentSize.Medium,
            MountType = MountType.Internal, Rarity = EquipmentRarity.Common, Manufacturer = "CargoCorp",
            Description = "Standard cargo expansion bay", BasePrice = 5000, Mass = 3.0, PowerDraw = 2,
            HeatGeneration = 1, MinimumShipSize = ShipSize.Small, CapacityBonus = 50,
            Specialization = CargoSpecialization.General, HasTemperatureControl = false,
            TemperatureRange = (-20, 40), HasHazmatContainment = false, HasRadiationShielding = false,
            HasSecureStorage = false, HasAutomatedLoading = false, LoadingSpeedMultiplier = 1.0,
            CompressionRatio = 1.0, MassPenalty = 0, PowerDrawActive = 0
        };
    }

    [Fact] public void Validate_ValidCargoHold_ReturnsTrue() { var ch = CreateValidCargoHold(); Assert.True(ch.Validate(out var e)); Assert.Empty(e); }
    [Fact] public void Validate_NegativeCapacityBonus_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.CapacityBonus = -1; Assert.False(ch.Validate(out var e)); Assert.Contains("Capacity", e); }
    [Fact] public void Validate_ZeroLoadingSpeedMultiplier_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.LoadingSpeedMultiplier = 0; Assert.False(ch.Validate(out var e)); Assert.Contains("Loading", e); }
    [Fact] public void Validate_NegativeLoadingSpeedMultiplier_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.LoadingSpeedMultiplier = -1; Assert.False(ch.Validate(out var e)); }
    [Fact] public void Validate_CompressionRatioTooLow_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.CompressionRatio = 0.5; Assert.False(ch.Validate(out var e)); Assert.Contains("Compression", e); }
    [Fact] public void Validate_NegativeMassPenalty_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.MassPenalty = -1; Assert.False(ch.Validate(out var e)); Assert.Contains("Mass", e); }

    [Fact] public void EffectiveCapacityBonus_FullCondition_ReturnsFullBonus() { var ch = CreateValidCargoHold(); ch.CapacityBonus = 50; ch.Condition = 1.0; Assert.Equal(50, ch.EffectiveCapacityBonus); }
    [Fact] public void EffectiveCapacityBonus_HalfCondition_ReturnsHalfBonus() { var ch = CreateValidCargoHold(); ch.CapacityBonus = 50; ch.Condition = 0.5; Assert.Equal(25, ch.EffectiveCapacityBonus); }
    [Fact] public void EffectiveCapacityBonus_ZeroCondition_ReturnsZero() { var ch = CreateValidCargoHold(); ch.CapacityBonus = 50; ch.Condition = 0; Assert.Equal(0, ch.EffectiveCapacityBonus); }

    [Fact] public void CanStoreCommodity_LegalGeneral_ReturnsTrue() { var ch = CreateValidCargoHold(); Assert.True(ch.CanStoreCommodity(CommodityCategory.Ore, CommodityLegality.Legal, new HashSet<string>())); }
    [Fact] public void CanStoreCommodity_Illegal_WithoutSecureStorage_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.HasSecureStorage = false; Assert.False(ch.CanStoreCommodity(CommodityCategory.Illegal, CommodityLegality.Illegal, new HashSet<string>())); }
    [Fact] public void CanStoreCommodity_Illegal_WithSecureStorage_ReturnsTrue() { var ch = CreateValidCargoHold(); ch.HasSecureStorage = true; Assert.True(ch.CanStoreCommodity(CommodityCategory.Illegal, CommodityLegality.Illegal, new HashSet<string>())); }
    [Fact] public void CanStoreCommodity_Restricted_WithoutSecureStorage_ReturnsTrue() { var ch = CreateValidCargoHold(); ch.HasSecureStorage = false; Assert.True(ch.CanStoreCommodity(CommodityCategory.Weapons, CommodityLegality.Restricted, new HashSet<string>())); }
    [Fact] public void CanStoreCommodity_Radioactive_WithoutShielding_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.HasRadiationShielding = false; Assert.False(ch.CanStoreCommodity(CommodityCategory.Ore, CommodityLegality.Legal, new HashSet<string> { "radioactive" })); }
    [Fact] public void CanStoreCommodity_Radioactive_WithShielding_ReturnsTrue() { var ch = CreateValidCargoHold(); ch.HasRadiationShielding = true; Assert.True(ch.CanStoreCommodity(CommodityCategory.Ore, CommodityLegality.Legal, new HashSet<string> { "radioactive" })); }
    [Fact] public void CanStoreCommodity_Hazardous_WithoutContainment_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.HasHazmatContainment = false; Assert.False(ch.CanStoreCommodity(CommodityCategory.Medical, CommodityLegality.Legal, new HashSet<string> { "hazardous" })); }
    [Fact] public void CanStoreCommodity_Hazardous_WithContainment_ReturnsTrue() { var ch = CreateValidCargoHold(); ch.HasHazmatContainment = true; Assert.True(ch.CanStoreCommodity(CommodityCategory.Medical, CommodityLegality.Legal, new HashSet<string> { "hazardous" })); }
    [Fact] public void CanStoreCommodity_Perishable_WithoutTemperatureControl_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.HasTemperatureControl = false; Assert.False(ch.CanStoreCommodity(CommodityCategory.Organics, CommodityLegality.Legal, new HashSet<string> { "perishable" })); }
    [Fact] public void CanStoreCommodity_Perishable_WithTemperatureControl_ReturnsTrue() { var ch = CreateValidCargoHold(); ch.HasTemperatureControl = true; Assert.True(ch.CanStoreCommodity(CommodityCategory.Organics, CommodityLegality.Legal, new HashSet<string> { "perishable" })); }
    [Fact] public void CanStoreCommodity_IllegalRadioactive_WithoutBoth_ReturnsFalse() { var ch = CreateValidCargoHold(); ch.HasSecureStorage = false; ch.HasRadiationShielding = false; Assert.False(ch.CanStoreCommodity(CommodityCategory.Illegal, CommodityLegality.Illegal, new HashSet<string> { "radioactive" })); }
    [Fact] public void CanStoreCommodity_IllegalRadioactive_WithBoth_ReturnsTrue() { var ch = CreateValidCargoHold(); ch.HasSecureStorage = true; ch.HasRadiationShielding = true; Assert.True(ch.CanStoreCommodity(CommodityCategory.Illegal, CommodityLegality.Illegal, new HashSet<string> { "radioactive" })); }

    [Fact] public void GetStorageEfficiency_GeneralSpecialization_ReturnsOne() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.General; Assert.Equal(1.0, ch.GetStorageEfficiency(CommodityCategory.Ore, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_OreSpecialization_OreCategory_ReturnsBonus() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Ore; Assert.Equal(1.2, ch.GetStorageEfficiency(CommodityCategory.Ore, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_OreSpecialization_NonOreCategory_ReturnsOne() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Ore; Assert.Equal(1.0, ch.GetStorageEfficiency(CommodityCategory.Tech, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_OrganicsSpecialization_OrganicsCategory_ReturnsBonus() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Organics; Assert.Equal(1.2, ch.GetStorageEfficiency(CommodityCategory.Organics, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_TechSpecialization_TechCategory_ReturnsBonus() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Tech; Assert.Equal(1.2, ch.GetStorageEfficiency(CommodityCategory.Tech, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_LuxurySpecialization_LuxuryCategory_ReturnsBonus() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Luxury; Assert.Equal(1.2, ch.GetStorageEfficiency(CommodityCategory.Luxury, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_WeaponsSpecialization_WeaponsCategory_ReturnsBonus() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Weapons; Assert.Equal(1.2, ch.GetStorageEfficiency(CommodityCategory.Weapons, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_MedicalSpecialization_MedicalCategory_ReturnsBonus() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Medical; Assert.Equal(1.2, ch.GetStorageEfficiency(CommodityCategory.Medical, new HashSet<string>())); }
    [Fact] public void GetStorageEfficiency_WithCompressibleTag_AppliesCompression() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Ore; ch.CompressionRatio = 1.5; Assert.Equal(1.8, ch.GetStorageEfficiency(CommodityCategory.Ore, new HashSet<string> { "compressible" }), 0.0001); }
    [Fact] public void GetStorageEfficiency_SpecializationAndCompressible_Stacks() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Ore; ch.CompressionRatio = 1.5; Assert.Equal(1.8, ch.GetStorageEfficiency(CommodityCategory.Ore, new HashSet<string> { "compressible" }), 0.0001); }

    [Fact] public void GetEffectiveCargoMass_GeneralEfficiency_ReturnsBaseMass() { var ch = CreateValidCargoHold(); Assert.Equal(100, ch.GetEffectiveCargoMass(100, CommodityCategory.Ore, new HashSet<string>())); }
    [Fact] public void GetEffectiveCargoMass_WithSpecialization_ReducesMass() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Ore; Assert.Equal(100, ch.GetEffectiveCargoMass(120, CommodityCategory.Ore, new HashSet<string>())); }
    [Fact] public void GetEffectiveCargoMass_WithCompression_ReducesMass() { var ch = CreateValidCargoHold(); ch.Specialization = CargoSpecialization.Ore; ch.CompressionRatio = 2.0; Assert.Equal(100, ch.GetEffectiveCargoMass(240, CommodityCategory.Ore, new HashSet<string> { "compressible" })); }

    [Fact] public void Clone_CreatesDeepCopy() { var ch = CreateValidCargoHold(); ch.Tags.Add("military"); ch.StatModifiers["cargo"] = 1.3; var clone = (CargoHold)ch.Clone(); Assert.Equal(ch.Id, clone.Id); Assert.Equal(ch.Name, clone.Name); Assert.Equal(ch.CapacityBonus, clone.CapacityBonus); Assert.Equal(ch.Specialization, clone.Specialization); Assert.Equal(ch.HasTemperatureControl, clone.HasTemperatureControl); Assert.Equal(ch.TemperatureRange, clone.TemperatureRange); Assert.Equal(ch.HasHazmatContainment, clone.HasHazmatContainment); Assert.Equal(ch.HasRadiationShielding, clone.HasRadiationShielding); Assert.Equal(ch.HasSecureStorage, clone.HasSecureStorage); Assert.Equal(ch.HasAutomatedLoading, clone.HasAutomatedLoading); Assert.Equal(ch.LoadingSpeedMultiplier, clone.LoadingSpeedMultiplier); Assert.Equal(ch.CompressionRatio, clone.CompressionRatio); Assert.Equal(ch.MassPenalty, clone.MassPenalty); Assert.Equal(ch.PowerDrawActive, clone.PowerDrawActive); Assert.Contains("military", clone.Tags); Assert.Equal(1.3, clone.StatModifiers["cargo"]); }
    [Fact] public void Clone_IsIndependent() { var ch = CreateValidCargoHold(); var clone = (CargoHold)ch.Clone(); clone.CapacityBonus = 999; clone.Specialization = CargoSpecialization.Weapons; Assert.NotEqual(999, ch.CapacityBonus); Assert.NotEqual(CargoSpecialization.Weapons, ch.Specialization); }

    [Fact] public void Serialize_ProducesValidJson() { var ch = CreateValidCargoHold(); var json = ch.Serialize(); Assert.Equal("ch_std", json["id"]!.ToString()); Assert.Equal("Standard Cargo Bay", json["name"]!.ToString()); Assert.Equal("CargoExpansion", json["type"]!.ToString()); Assert.Equal(50, json["capacityBonus"]!.ToObject<int>()); Assert.Equal("General", json["specialization"]!.ToString()); Assert.False(json["hasTemperatureControl"]!.ToObject<bool>()); Assert.Equal(-20, json["temperatureRangeMin"]!.ToObject<int>()); Assert.Equal(40, json["temperatureRangeMax"]!.ToObject<int>()); Assert.False(json["hasHazmatContainment"]!.ToObject<bool>()); Assert.False(json["hasRadiationShielding"]!.ToObject<bool>()); Assert.False(json["hasSecureStorage"]!.ToObject<bool>()); Assert.False(json["hasAutomatedLoading"]!.ToObject<bool>()); Assert.Equal(1.0, json["loadingSpeedMultiplier"]!.ToObject<double>()); Assert.Equal(1.0, json["compressionRatio"]!.ToObject<double>()); Assert.Equal(0, json["massPenalty"]!.ToObject<double>()); Assert.Equal(0, json["powerDrawActive"]!.ToObject<double>()); }
    [Fact] public void Deserialize_RestoresAllProperties() { var original = CreateValidCargoHold(); original.Tags.Add("test_tag"); original.HasTemperatureControl = true; original.TemperatureRange = (-30, 50); original.HasSecureStorage = true; var json = original.Serialize(); var restored = new CargoHold(); restored.Deserialize(json); Assert.Equal(original.Id, restored.Id); Assert.Equal(original.Name, restored.Name); Assert.Equal(original.CapacityBonus, restored.CapacityBonus); Assert.Equal(original.Specialization, restored.Specialization); Assert.Equal(original.HasTemperatureControl, restored.HasTemperatureControl); Assert.Equal(original.TemperatureRange, restored.TemperatureRange); Assert.Equal(original.HasSecureStorage, restored.HasSecureStorage); Assert.Contains("test_tag", restored.Tags); }
    [Fact] public void SerializeDeserialize_RoundTrip_PreservesData() { var ch = CreateValidCargoHold("ch_rt", "Round Trip"); ch.Tags.Add("tag1"); ch.HasHazmatContainment = true; ch.HasRadiationShielding = true; ch.HasAutomatedLoading = true; ch.LoadingSpeedMultiplier = 2.0; ch.CompressionRatio = 1.5; var json = ch.Serialize(); var restored = new CargoHold(); restored.Deserialize(json); Assert.Equal(ch.Id, restored.Id); Assert.Equal(ch.Name, restored.Name); Assert.Equal(ch.CapacityBonus, restored.CapacityBonus); Assert.True(restored.HasHazmatContainment); Assert.True(restored.HasRadiationShielding); Assert.True(restored.HasAutomatedLoading); Assert.Equal(2.0, restored.LoadingSpeedMultiplier); Assert.Equal(1.5, restored.CompressionRatio); Assert.Contains("tag1", restored.Tags); }

    [Fact] public void SaveId_IsCorrectlyFormatted() { var ch = CreateValidCargoHold("ch_save", "Save Test"); Assert.Equal("cargohold_ch_save", ch.SaveId); }
    [Fact] public void SaveVersion_IsOne() { Assert.Equal(1, new CargoHold().SaveVersion); }

    [Fact] public void DefaultValues_AreSensible() { var ch = new CargoHold(); Assert.Equal(10, ch.CapacityBonus); Assert.Equal(CargoSpecialization.General, ch.Specialization); Assert.False(ch.HasTemperatureControl); Assert.Equal((-20, 40), ch.TemperatureRange); Assert.False(ch.HasHazmatContainment); Assert.False(ch.HasRadiationShielding); Assert.False(ch.HasSecureStorage); Assert.False(ch.HasAutomatedLoading); Assert.Equal(1.0, ch.LoadingSpeedMultiplier); Assert.Equal(1.0, ch.CompressionRatio); Assert.Equal(0, ch.MassPenalty); Assert.Equal(0, ch.PowerDrawActive); }

    [Fact] public void CargoSpecialization_AllValues_CanBeSet() { foreach (CargoSpecialization cs in Enum.GetValues<CargoSpecialization>()) { var ch = CreateValidCargoHold(); ch.Specialization = cs; Assert.Equal(cs, ch.Specialization); } }

    [Fact] public void Validate_ZeroCapacityBonus_Valid() { var ch = CreateValidCargoHold(); ch.CapacityBonus = 0; Assert.True(ch.Validate(out _)); }
    [Fact] public void Validate_CompressionRatioOne_Valid() { var ch = CreateValidCargoHold(); ch.CompressionRatio = 1.0; Assert.True(ch.Validate(out _)); }
    [Fact] public void Validate_CompressionRatioLarge_Valid() { var ch = CreateValidCargoHold(); ch.CompressionRatio = 10.0; Assert.True(ch.Validate(out _)); }
    [Fact] public void Validate_ZeroMassPenalty_Valid() { var ch = CreateValidCargoHold(); ch.MassPenalty = 0; Assert.True(ch.Validate(out _)); }
    [Fact] public void CanStoreCommodity_MultipleTags_AllRequired() { var ch = CreateValidCargoHold(); ch.HasRadiationShielding = true; ch.HasHazmatContainment = false; Assert.False(ch.CanStoreCommodity(CommodityCategory.Medical, CommodityLegality.Legal, new HashSet<string> { "radioactive", "hazardous" })); }
    [Fact] public void CanStoreCommodity_MultipleTags_AllSatisfied() { var ch = CreateValidCargoHold(); ch.HasRadiationShielding = true; ch.HasHazmatContainment = true; ch.HasTemperatureControl = true; Assert.True(ch.CanStoreCommodity(CommodityCategory.Medical, CommodityLegality.Legal, new HashSet<string> { "radioactive", "hazardous", "perishable" })); }
}
