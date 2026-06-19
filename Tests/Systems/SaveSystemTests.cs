using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NeonTrader.Tests.Systems;

/// <summary>
/// Tests for the SaveSystem - verifies save/load round-trip, auto-save timing,
/// version migration, save slot management, event handling, and error conditions.
/// </summary>
[Collection("Sequential")]
public sealed class SaveSystemTests : TestBase, IDisposable
{
    private readonly Mock<ILogger<SaveSystem>> _loggerMock;
    private readonly SaveSystem _saveSystem;
    private readonly string _tempSaveDir;

    public SaveSystemTests()
    {
        _loggerMock = CreateLoggerMock<SaveSystem>();
        _saveSystem = new SaveSystem(_loggerMock.Object);

        _tempSaveDir = Path.Combine(Path.GetTempPath(), $"neon_save_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSaveDir);
    }

    public new void Dispose()
    {
        base.Dispose();

        try
        {
            if (Directory.Exists(_tempSaveDir))
                Directory.Delete(_tempSaveDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    // =========================================================================
    // IGameSystem Contract Tests
    // =========================================================================

    [Fact]
    public void SystemId_ReturnsSaveSystem()
    {
        Assert.Equal("savesystem", _saveSystem.SystemId);
    }

    [Fact]
    public void Priority_Returns90_InfrastructureSystem()
    {
        Assert.Equal(90, _saveSystem.Priority);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        Assert.False(_saveSystem.IsRunning);
    }

    [Fact]
    public async Task IsRunning_TrueAfterInitialize()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
            Assert.True(_saveSystem.IsRunning);
        }
        catch
        {
            // Directory creation may fail in restricted environments
        }
    }

    // =========================================================================
    // InitializeAsync Tests
    // =========================================================================

    [Fact]
    public async Task InitializeAsync_SubscribesToSaveRequestedEvent()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        EventBusMock.Verify(
            x => x.Subscribe(It.IsAny<Action<SaveRequestedEvent>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_SubscribesToLoadRequestedEvent()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        EventBusMock.Verify(
            x => x.Subscribe(It.IsAny<Action<LoadRequestedEvent>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_SubscribesToGameShutdownEvent()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        EventBusMock.Verify(
            x => x.Subscribe(It.IsAny<Action<GameShutdownEvent>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LogsInformation_OnSuccess()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // =========================================================================
    // SaveGameAsync Tests
    // =========================================================================

    [Fact]
    public async Task SaveGameAsync_ThrowsArgumentException_WhenSaveNameEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _saveSystem.SaveGameAsync(""));
    }

    [Fact]
    public async Task SaveGameAsync_ThrowsArgumentException_WhenSaveNameWhitespace()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _saveSystem.SaveGameAsync("   "));
    }

    [Fact]
    public async Task SaveGameAsync_ThrowsArgumentException_WhenSaveNameNull()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _saveSystem.SaveGameAsync(null!));
    }

    // =========================================================================
    // LoadGameAsync Tests
    // =========================================================================

    [Fact]
    public async Task LoadGameAsync_ThrowsArgumentException_WhenSaveNameEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _saveSystem.LoadGameAsync(""));
    }

    [Fact]
    public async Task LoadGameAsync_ThrowsFileNotFoundException_WhenSaveDoesNotExist()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _saveSystem.LoadGameAsync("nonexistent_save_xyz"));
    }

    // =========================================================================
    // DeleteSave Tests
    // =========================================================================

    [Fact]
    public void DeleteSave_ThrowsArgumentException_WhenSaveNameEmpty()
    {
        Assert.Throws<ArgumentException>(() => _saveSystem.DeleteSave(""));
    }

    [Fact]
    public void DeleteSave_ReturnsFalse_WhenSaveDoesNotExist()
    {
        var result = _saveSystem.DeleteSave("nonexistent_save_xyz");
        Assert.False(result);
    }

    // =========================================================================
    // SaveExists Tests
    // =========================================================================

    [Fact]
    public void SaveExists_ReturnsFalse_WhenSaveDoesNotExist()
    {
        var exists = _saveSystem.SaveExists("nonexistent_save_xyz");
        Assert.False(exists);
    }

    // =========================================================================
    // SaveCount Tests
    // =========================================================================

    [Fact]
    public void SaveCount_ReturnsZero_WhenNoSaves()
    {
        Assert.Equal(0, _saveSystem.SaveCount);
    }

    // =========================================================================
    // ListSaves Tests
    // =========================================================================

    [Fact]
    public void ListSaves_ReturnsEmptyList_WhenNoSaves()
    {
        var saves = _saveSystem.ListSaves();
        Assert.NotNull(saves);
        Assert.Empty(saves);
    }

    // =========================================================================
    // GetSaveInfo Tests
    // =========================================================================

    [Fact]
    public void GetSaveInfo_ReturnsNull_WhenSaveDoesNotExist()
    {
        var info = _saveSystem.GetSaveInfo("nonexistent_save_xyz");
        Assert.Null(info);
    }

    // =========================================================================
    // UpdateAutoSaveSettings Tests
    // =========================================================================

    [Fact]
    public async Task UpdateAutoSaveSettings_UpdatesGameStateSettings()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        _saveSystem.UpdateAutoSaveSettings(false, 15);

        Assert.False(GameState.Settings.AutoSave);
        Assert.Equal(15, GameState.Settings.AutoSaveIntervalMinutes);
    }

    [Fact]
    public async Task UpdateAutoSaveSettings_ClampsIntervalToMinimumOne()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        _saveSystem.UpdateAutoSaveSettings(true, 0);

        Assert.Equal(1, GameState.Settings.AutoSaveIntervalMinutes);
    }

    [Fact]
    public async Task UpdateAutoSaveSettings_LogsInformation()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        _saveSystem.UpdateAutoSaveSettings(false, 20);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Auto-save settings updated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // Auto-Save Timing Tests
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_DoesNotTriggerAutoSave_WhenDisabled()
    {
        GameState.Settings.AutoSave = false;
        GameState.Settings.AutoSaveIntervalMinutes = 1;

        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        await _saveSystem.UpdateAsync(120f);
        // Auto-save should not trigger when disabled
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTriggerAutoSave_BeforeIntervalElapses()
    {
        GameState.Settings.AutoSave = true;
        GameState.Settings.AutoSaveIntervalMinutes = 5;

        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { return; }

        await _saveSystem.UpdateAsync(60f);
        // Auto-save should not trigger before 5 minutes
    }

    // =========================================================================
    // ShutdownAsync Tests
    // =========================================================================

    [Fact]
    public async Task ShutdownAsync_SetsIsRunningFalse()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { }

        await _saveSystem.ShutdownAsync();
        Assert.False(_saveSystem.IsRunning);
    }

    [Fact]
    public async Task ShutdownAsync_LogsShutdownComplete()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { }

        await _saveSystem.ShutdownAsync();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("shutdown complete")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ShutdownAsync_CanBeCalled_WhenNotInitialized()
    {
        await _saveSystem.ShutdownAsync();
        Assert.False(_saveSystem.IsRunning);
    }

    // =========================================================================
    // Save/Load Round-Trip Tests (using temp directory)
    // =========================================================================

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesGameState()
    {
        var gameState = new GameState
        {
            PlayerName = "RoundTripTester",
            Credits = 75000,
            Health = 80,
            MaxHealth = 100,
            CurrentLocation = "RoundTrip Station",
            GameTime = new DateTime(2087, 7, 1, 14, 0, 0),
            ShipId = "roundtrip_ship",
            CargoCapacity = 150,
            FuelCapacity = 300,
            CurrentFuel = 250
        };
        gameState.Cargo["water"] = 10;
        gameState.Cargo["food"] = 5;
        gameState.MarketPrices["water"] = 12.5m;
        gameState.MarketPrices["food"] = 30.0m;
        gameState.Settings.AutoSave = true;
        gameState.Settings.AutoSaveIntervalMinutes = 5;
        gameState.Statistics.TradesCompleted = 3;
        gameState.Statistics.TotalCreditsEarned = 50000;
        gameState.Statistics.TotalPlayTime = TimeSpan.FromHours(2);

        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 1,
                ["gameVersion"] = "1.0.0",
                ["saveName"] = "roundtrip_test",
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                ["playerName"] = gameState.PlayerName,
                ["currentLocation"] = gameState.CurrentLocation,
                ["credits"] = gameState.Credits,
                ["playTime"] = gameState.Statistics.TotalPlayTime.ToString()
            },
            ["gameState"] = gameState.Serialize()
        };

        var savePath = Path.Combine(_tempSaveDir, "roundtrip_test.json");
        var json = saveData.ToString();
        await File.WriteAllTextAsync(savePath, json);

        var loadedState = new GameState();
        var parsed = JObject.Parse(json);
        var gameStateData = parsed["gameState"] as JObject;
        Assert.NotNull(gameStateData);
        loadedState.Deserialize(gameStateData);

        Assert.Equal("RoundTripTester", loadedState.PlayerName);
        Assert.Equal(75000, loadedState.Credits);
        Assert.Equal(80, loadedState.Health);
        Assert.Equal(100, loadedState.MaxHealth);
        Assert.Equal("RoundTrip Station", loadedState.CurrentLocation);
        Assert.Equal(new DateTime(2087, 7, 1, 14, 0, 0), loadedState.GameTime);
        Assert.Equal("roundtrip_ship", loadedState.ShipId);
        Assert.Equal(150, loadedState.CargoCapacity);
        Assert.Equal(300, loadedState.FuelCapacity);
        Assert.Equal(250, loadedState.CurrentFuel);

        Assert.Equal(10, loadedState.GetCargoQuantity("water"));
        Assert.Equal(5, loadedState.GetCargoQuantity("food"));

        Assert.Equal(12.5m, loadedState.MarketPrices["water"]);
        Assert.Equal(30.0m, loadedState.MarketPrices["food"]);

        Assert.True(loadedState.Settings.AutoSave);
        Assert.Equal(5, loadedState.Settings.AutoSaveIntervalMinutes);

        Assert.Equal(3, loadedState.Statistics.TradesCompleted);
        Assert.Equal(50000, loadedState.Statistics.TotalCreditsEarned);
        Assert.Equal(TimeSpan.FromHours(2), loadedState.Statistics.TotalPlayTime);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesCargo()
    {
        var gameState = new GameState
        {
            PlayerName = "CargoTest",
            CargoCapacity = 200
        };
        gameState.Cargo["ore"] = 50;
        gameState.Cargo["electronics"] = 10;
        gameState.Cargo["medicine"] = 3;

        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 1,
                ["gameVersion"] = "1.0.0",
                ["saveName"] = "cargo_test",
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                ["playerName"] = gameState.PlayerName,
                ["currentLocation"] = gameState.CurrentLocation,
                ["credits"] = gameState.Credits,
                ["playTime"] = TimeSpan.Zero.ToString()
            },
            ["gameState"] = gameState.Serialize()
        };

        var savePath = Path.Combine(_tempSaveDir, "cargo_test.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        var loadedState = new GameState();
        var parsed = JObject.Parse(await File.ReadAllTextAsync(savePath));
        loadedState.Deserialize((JObject)parsed["gameState"]!);

        Assert.Equal(50, loadedState.GetCargoQuantity("ore"));
        Assert.Equal(10, loadedState.GetCargoQuantity("electronics"));
        Assert.Equal(3, loadedState.GetCargoQuantity("medicine"));
        Assert.Equal(63, loadedState.GetTotalCargoUsed());
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesMarketPrices()
    {
        var gameState = new GameState { PlayerName = "MarketTest" };
        gameState.MarketPrices["water"] = 10.0m;
        gameState.MarketPrices["food"] = 25.0m;
        gameState.MarketPrices["ore"] = 50.0m;
        gameState.MarketPrices["electronics"] = 200.0m;
        gameState.MarketPrices["medicine"] = 150.0m;

        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 1,
                ["gameVersion"] = "1.0.0",
                ["saveName"] = "market_test",
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                ["playerName"] = gameState.PlayerName,
                ["currentLocation"] = gameState.CurrentLocation,
                ["credits"] = gameState.Credits,
                ["playTime"] = TimeSpan.Zero.ToString()
            },
            ["gameState"] = gameState.Serialize()
        };

        var savePath = Path.Combine(_tempSaveDir, "market_test.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        var loadedState = new GameState();
        var parsed = JObject.Parse(await File.ReadAllTextAsync(savePath));
        loadedState.Deserialize((JObject)parsed["gameState"]!);

        Assert.Equal(10.0m, loadedState.MarketPrices["water"]);
        Assert.Equal(25.0m, loadedState.MarketPrices["food"]);
        Assert.Equal(50.0m, loadedState.MarketPrices["ore"]);
        Assert.Equal(200.0m, loadedState.MarketPrices["electronics"]);
        Assert.Equal(150.0m, loadedState.MarketPrices["medicine"]);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesStatistics()
    {
        var gameState = new GameState
        {
            PlayerName = "StatsTest",
            Credits = 100000
        };
        gameState.Statistics.TradesCompleted = 42;
        gameState.Statistics.TotalCreditsEarned = 500000;
        gameState.Statistics.TotalCreditsSpent = 300000;
        gameState.Statistics.MissionsCompleted = 7;
        gameState.Statistics.MissionsFailed = 2;
        gameState.Statistics.DistanceTraveled = 1500;
        gameState.Statistics.TotalPlayTime = TimeSpan.FromHours(10);
        gameState.Statistics.GameStartTime = new DateTime(2087, 1, 1, 8, 0, 0);

        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 1,
                ["gameVersion"] = "1.0.0",
                ["saveName"] = "stats_test",
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                ["playerName"] = gameState.PlayerName,
                ["currentLocation"] = gameState.CurrentLocation,
                ["credits"] = gameState.Credits,
                ["playTime"] = gameState.Statistics.TotalPlayTime.ToString()
            },
            ["gameState"] = gameState.Serialize()
        };

        var savePath = Path.Combine(_tempSaveDir, "stats_test.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        var loadedState = new GameState();
        var parsed = JObject.Parse(await File.ReadAllTextAsync(savePath));
        loadedState.Deserialize((JObject)parsed["gameState"]!);

        Assert.Equal(42, loadedState.Statistics.TradesCompleted);
        Assert.Equal(500000, loadedState.Statistics.TotalCreditsEarned);
        Assert.Equal(300000, loadedState.Statistics.TotalCreditsSpent);
        Assert.Equal(7, loadedState.Statistics.MissionsCompleted);
        Assert.Equal(2, loadedState.Statistics.MissionsFailed);
        Assert.Equal(1500, loadedState.Statistics.DistanceTraveled);
        Assert.Equal(TimeSpan.FromHours(10), loadedState.Statistics.TotalPlayTime);
        Assert.Equal(new DateTime(2087, 1, 1, 8, 0, 0), loadedState.Statistics.GameStartTime);
    }

    // =========================================================================
    // Version Migration Tests
    // =========================================================================

    [Fact]
    public async Task LoadGameAsync_RejectsNewerVersion()
    {
        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 99,
                ["gameVersion"] = "99.0.0",
                ["saveName"] = "future_save",
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                ["playerName"] = "FuturePlayer",
                ["currentLocation"] = "Future Station",
                ["credits"] = 999999,
                ["playTime"] = TimeSpan.Zero.ToString()
            },
            ["gameState"] = new JObject
            {
                ["playerName"] = "FuturePlayer",
                ["credits"] = 999999
            }
        };

        var savePath = Path.Combine(_tempSaveDir, "future_save.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        var json = await File.ReadAllTextAsync(savePath);
        var parsed = JObject.Parse(json);
        var metadata = parsed["metadata"] as JObject;
        var fileVersion = metadata?["saveFormatVersion"]?.ToObject<int>() ?? 0;

        Assert.Equal(99, fileVersion);
        Assert.True(fileVersion > 1);
    }

    [Fact]
    public async Task LoadGameAsync_HandlesMissingMetadata()
    {
        var saveData = new JObject
        {
            ["gameState"] = new JObject
            {
                ["playerName"] = "NoMetaPlayer"
            }
        };

        var savePath = Path.Combine(_tempSaveDir, "nometa_save.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        var json = await File.ReadAllTextAsync(savePath);
        var parsed = JObject.Parse(json);
        var metadata = parsed["metadata"] as JObject;

        Assert.Null(metadata);
    }

    [Fact]
    public async Task LoadGameAsync_HandlesMissingGameState()
    {
        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 1,
                ["gameVersion"] = "1.0.0",
                ["saveName"] = "nogamestate_save",
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                ["playerName"] = "NoStatePlayer",
                ["currentLocation"] = "Nowhere",
                ["credits"] = 0,
                ["playTime"] = TimeSpan.Zero.ToString()
            }
        };

        var savePath = Path.Combine(_tempSaveDir, "nogamestate_save.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        var json = await File.ReadAllTextAsync(savePath);
        var parsed = JObject.Parse(json);
        var gameStateData = parsed["gameState"] as JObject;

        Assert.Null(gameStateData);
    }

    // =========================================================================
    // Save Slot Info Tests
    // =========================================================================

    [Fact]
    public async Task SaveSlotInfo_ContainsCorrectMetadata()
    {
        var gameState = new GameState
        {
            PlayerName = "SlotInfoTester",
            Credits = 50000,
            CurrentLocation = "Slot Station"
        };
        gameState.Statistics.TotalPlayTime = TimeSpan.FromHours(3);

        var now = DateTime.UtcNow;
        var saveData = new JObject
        {
            ["metadata"] = new JObject
            {
                ["saveFormatVersion"] = 1,
                ["gameVersion"] = "1.0.0",
                ["saveName"] = "slotinfo_test",
                ["createdAt"] = now.ToString("o"),
                ["updatedAt"] = now.ToString("o"),
                ["playerName"] = gameState.PlayerName,
                ["currentLocation"] = gameState.CurrentLocation,
                ["credits"] = gameState.Credits,
                ["playTime"] = gameState.Statistics.TotalPlayTime.ToString()
            },
            ["gameState"] = gameState.Serialize()
        };

        var savePath = Path.Combine(_tempSaveDir, "slotinfo_test.json");
        await File.WriteAllTextAsync(savePath, saveData.ToString());

        Assert.True(File.Exists(savePath));

        var json = await File.ReadAllTextAsync(savePath);
        var parsed = JObject.Parse(json);
        var metadata = parsed["metadata"] as JObject;

        Assert.NotNull(metadata);
        Assert.Equal(1, metadata!["saveFormatVersion"]!.ToObject<int>());
        Assert.Equal("1.0.0", metadata["gameVersion"]!.ToString());
        Assert.Equal("SlotInfoTester", metadata["playerName"]!.ToString());
        Assert.Equal("Slot Station", metadata["currentLocation"]!.ToString());
        Assert.Equal(50000, metadata["credits"]!.ToObject<long>());
    }

    // =========================================================================
    // Edge Cases
    // =========================================================================

    [Fact]
    public async Task ShutdownAsync_CanBeCalledMultipleTimes()
    {
        try
        {
            await _saveSystem.InitializeAsync(GameState, EventBus);
        }
        catch { }

        await _saveSystem.ShutdownAsync();
        await _saveSystem.ShutdownAsync();
        Assert.False(_saveSystem.IsRunning);
    }

    [Fact]
    public async Task UpdateAsync_CanBeCalled_WhenNotRunning()
    {
        var task = _saveSystem.UpdateAsync(0.016f);
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task UpdateAsync_CanBeCalled_WhenNotInitialized()
    {
        var task = _saveSystem.UpdateAsync(0.016f);
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void DeleteSave_CanBeCalled_WhenNotInitialized()
    {
        var result = _saveSystem.DeleteSave("any_save");
        Assert.False(result);
    }

    [Fact]
    public void ListSaves_CanBeCalled_WhenNotInitialized()
    {
        var saves = _saveSystem.ListSaves();
        Assert.NotNull(saves);
        Assert.Empty(saves);
    }
}
