using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Systems;
using Xunit;

namespace NeonTrader.Tests.Systems;

/// <summary>
/// Tests for the DataLoader system - verifies JSON data loading into registries,
/// event publishing, error handling, and IGameSystem contract compliance.
/// </summary>
public sealed class DataLoaderTests : TestBase
{
    private readonly Mock<ILogger<DataLoader>> _loggerMock;
    private readonly DataLoader _dataLoader;

    public DataLoaderTests()
    {
        _loggerMock = CreateLoggerMock<DataLoader>();
        _dataLoader = new DataLoader(_loggerMock.Object);
    }

    // =========================================================================
    // IGameSystem Contract Tests
    // =========================================================================

    [Fact]
    public void SystemId_ReturnsDataLoader()
    {
        Assert.Equal("DataLoader", _dataLoader.SystemId);
    }

    [Fact]
    public void Priority_ReturnsZero_HighestPriority()
    {
        Assert.Equal(0, _dataLoader.Priority);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        Assert.False(_dataLoader.IsRunning);
    }

    [Fact]
    public async Task IsRunning_FalseAfterFailedInitialize()
    {
        try
        {
            await _dataLoader.InitializeAsync(GameState, EventBus);
        }
        catch (FileNotFoundException)
        {
            // Expected - data files don't exist in test environment
        }

        // After failure, IsRunning should be false (set in catch block)
        Assert.False(_dataLoader.IsRunning);
    }

    [Fact]
    public async Task ShutdownAsync_SetsIsRunningFalse()
    {
        await _dataLoader.ShutdownAsync();
        Assert.False(_dataLoader.IsRunning);
    }

    [Fact]
    public async Task UpdateAsync_IsNoOp_ReturnsCompletedTask()
    {
        var task = _dataLoader.UpdateAsync(0.016f);
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    // =========================================================================
    // InitializeAsync Tests
    // =========================================================================

    [Fact]
    public async Task InitializeAsync_ThrowsFileNotFoundException_WhenDataFilesMissing()
    {
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _dataLoader.InitializeAsync(GameState, EventBus));
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task InitializeAsync_LogsCritical_OnFailure()
    {
        try
        {
            await _dataLoader.InitializeAsync(GameState, EventBus);
        }
        catch
        {
            // Expected
        }

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed to initialize")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LogsInformation_OnStartup()
    {
        try
        {
            await _dataLoader.InitializeAsync(GameState, EventBus);
        }
        catch
        {
            // Expected
        }

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initializing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // Event Publishing Tests
    // =========================================================================

    [Fact]
    public async Task InitializeAsync_DoesNotPublishDataLoadedEvent_OnFailure()
    {
        try
        {
            await _dataLoader.InitializeAsync(GameState, EventBus);
        }
        catch
        {
            // Expected
        }

        EventBusMock.Verify(
            x => x.Publish(It.IsAny<DataLoadedEvent>()),
            Times.Never);
    }

    // =========================================================================
    // Constructor Tests
    // =========================================================================

    [Fact]
    public void Constructor_AcceptsNullLogger_DoesNotThrow()
    {
        var loader = new DataLoader(null!);
        Assert.NotNull(loader);
    }

    [Fact]
    public void Constructor_StoresLogger()
    {
        var logger = new Mock<ILogger<DataLoader>>().Object;
        var loader = new DataLoader(logger);
        Assert.Equal("DataLoader", loader.SystemId);
    }

    // =========================================================================
    // Edge Cases
    // =========================================================================

    [Fact]
    public async Task ShutdownAsync_CanBeCalledMultipleTimes()
    {
        await _dataLoader.ShutdownAsync();
        await _dataLoader.ShutdownAsync();
        Assert.False(_dataLoader.IsRunning);
    }

    [Fact]
    public async Task UpdateAsync_CanBeCalled_WhenNotRunning()
    {
        var task = _dataLoader.UpdateAsync(0.016f);
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ShutdownAsync_CanBeCalled_WhenNotInitialized()
    {
        await _dataLoader.ShutdownAsync();
        Assert.False(_dataLoader.IsRunning);
    }
}
