using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;

namespace NeonTrader.Tests;

/// <summary>
/// Base class for all NeonTrader tests providing common setup utilities.
/// </summary>
public abstract class TestBase : IDisposable
{
    /// <summary>
    /// Shared GameState instance with default values for testing.
    /// </summary>
    protected GameState GameState { get; private set; }

    /// <summary>
    /// Mocked IEventBus for verifying event publishing and subscriptions.
    /// </summary>
    protected Mock<IEventBus> EventBusMock { get; private set; }

    /// <summary>
    /// The mocked IEventBus instance (shortcut for EventBusMock.Object).
    /// </summary>
    protected IEventBus EventBus => EventBusMock.Object;

    /// <summary>
    /// List of disposables to clean up after each test.
    /// </summary>
    private readonly List<IDisposable> _disposables = new();

    protected TestBase()
    {
        GameState = CreateDefaultGameState();
        EventBusMock = CreateEventBusMock();
    }

    /// <summary>
    /// Creates a GameState with sensible default values for testing.
    /// Override in derived classes to customize.
    /// </summary>
    protected virtual GameState CreateDefaultGameState()
    {
        return new GameState
        {
            PlayerName = "TestPilot",
            Credits = 50000,
            Health = 100,
            MaxHealth = 100,
            CurrentLocation = "Test Station Alpha",
            PreviousLocation = "",
            GameTime = new DateTime(2087, 6, 15, 12, 0, 0),
            ShipId = "test_ship",
            CargoCapacity = 100,
            FuelCapacity = 200,
            CurrentFuel = 200
        };
    }

    /// <summary>
    /// Creates a default Mock&lt;IEventBus&gt; with all methods set up.
    /// </summary>
    protected virtual Mock<IEventBus> CreateEventBusMock()
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
    /// Creates a Mock&lt;ILogger&lt;T&gt;&gt; that can be used for any system under test.
    /// </summary>
    /// <typeparam name="T">The category type for the logger</typeparam>
    protected static Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Register a disposable to be cleaned up when the test ends.
    /// </summary>
    protected void RegisterDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    /// <summary>
    /// Clean up all registered disposables and mocks.
    /// </summary>
    public virtual void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();

        EventBusMock.Reset();
    }
}
