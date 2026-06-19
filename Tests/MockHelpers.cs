using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;

namespace NeonTrader.Tests;

/// <summary>
/// Factory methods for creating common mock objects used across tests.
/// </summary>
public static class MockHelpers
{
    /// <summary>
    /// Creates a Mock&lt;IGameSystem&gt; with the specified system ID and priority.
    /// All async methods return completed tasks by default.
    /// </summary>
    /// <param name="systemId">Unique system identifier</param>
    /// <param name="priority">Initialization/update priority (lower = earlier)</param>
    /// <param name="isRunning">Whether the system is initially running</param>
    public static Mock<IGameSystem> CreateGameSystemMock(
        string systemId = "TestSystem",
        int priority = 100,
        bool isRunning = true)
    {
        var mock = new Mock<IGameSystem>();

        mock.Setup(m => m.SystemId).Returns(systemId);
        mock.Setup(m => m.Priority).Returns(priority);
        mock.Setup(m => m.IsRunning).Returns(isRunning);

        mock.Setup(m => m.InitializeAsync(
                It.IsAny<GameState>(),
                It.IsAny<IEventBus>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(m => m.UpdateAsync(
                It.IsAny<float>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(m => m.ShutdownAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    /// Creates a Mock&lt;IEventBus&gt; that captures published events for verification.
    /// </summary>
    public static Mock<IEventBus> CreateEventBusMock()
    {
        var mock = new Mock<IEventBus>();

        mock.Setup(m => m.Publish(It.IsAny<GameEvent>()));
        mock.Setup(m => m.Subscribe(It.IsAny<Action<GameEvent>>()))
            .Returns(Mock.Of<IDisposable>());
        mock.Setup(m => m.SubscribeAll(It.IsAny<Action<GameEvent>>()))
            .Returns(Mock.Of<IDisposable>());

        return mock;
    }

    /// <summary>
    /// Creates a Mock&lt;ILogger&lt;T&gt;&gt; for the specified category type.
    /// </summary>
    /// <typeparam name="T">The category type for the logger</typeparam>
    public static Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Creates a GameState pre-configured for trading tests with populated market prices.
    /// </summary>
    public static GameState CreateTradingGameState()
    {
        var state = new GameState
        {
            PlayerName = "TraderJoe",
            Credits = 100000,
            CurrentLocation = "Trade Hub Prime",
            CargoCapacity = 200,
            FuelCapacity = 300,
            CurrentFuel = 300
        };

        // Populate some market prices for testing
        state.MarketPrices["water"] = 10.0m;
        state.MarketPrices["food"] = 25.0m;
        state.MarketPrices["ore"] = 50.0m;
        state.MarketPrices["electronics"] = 200.0m;
        state.MarketPrices["medicine"] = 150.0m;

        return state;
    }

    /// <summary>
    /// Creates a GameState pre-configured for combat tests with low health.
    /// </summary>
    public static GameState CreateCombatGameState()
    {
        return new GameState
        {
            PlayerName = "FighterPilot",
            Credits = 25000,
            Health = 50,
            MaxHealth = 100,
            CurrentLocation = "Combat Zone Delta",
            ShipId = "combat_ship",
            CargoCapacity = 50,
            FuelCapacity = 150,
            CurrentFuel = 100
        };
    }

    /// <summary>
    /// Creates a GameState pre-configured for navigation tests with known locations.
    /// </summary>
    public static GameState CreateNavigationGameState()
    {
        return new GameState
        {
            PlayerName = "Navigator",
            Credits = 30000,
            CurrentLocation = "Neon Station",
            PreviousLocation = "Old Dock",
            FuelCapacity = 500,
            CurrentFuel = 400
        };
    }
}
