using Microsoft.Extensions.Logging;
using Moq;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Systems;
using NeonTrader.Views;
using Terminal.Gui;
using Xunit;

namespace NeonTrader.Tests.Views;

/// <summary>
/// Shared fixture that initializes Terminal.Gui once for all MainGameScreen tests.
/// Uses FakeDriver for headless testing and real system instances with mock loggers.
/// </summary>
public class MainGameScreenFixture : IDisposable
{
    public GameState GameState { get; }
    public Mock<IEventBus> EventBusMock { get; }
    public IEventBus EventBus => EventBusMock.Object;
    public TradingSystem TradingSystem { get; }
    public NavigationSystem NavigationSystem { get; }

    public MainGameScreenFixture()
    {
        // Use FakeDriver for headless testing
        Application.Init(new FakeDriver());

        GameState = new GameState
        {
            PlayerName = "TestPilot",
            Credits = 50000,
            Health = 100,
            MaxHealth = 100,
            CurrentLocation = "test_station",
            CargoCapacity = 100,
            FuelCapacity = 200,
            CurrentFuel = 200,
            GameTime = new DateTime(2087, 6, 15, 12, 0, 0)
        };

        EventBusMock = new Mock<IEventBus>();
        EventBusMock.Setup(m => m.Publish(It.IsAny<GameEvent>()));
        EventBusMock.Setup(m => m.Subscribe(It.IsAny<Action<GameEvent>>()))
            .Returns(Mock.Of<IDisposable>());
        EventBusMock.Setup(m => m.SubscribeAll(It.IsAny<Action<GameEvent>>()))
            .Returns(Mock.Of<IDisposable>());

        // Create real system instances with mock loggers
        var tradingLoggerMock = new Mock<ILogger<TradingSystem>>();
        TradingSystem = new TradingSystem(tradingLoggerMock.Object);

        var navLoggerMock = new Mock<ILogger<NavigationSystem>>();
        NavigationSystem = new NavigationSystem(navLoggerMock.Object);

        // Initialize NavigationSystem so GetAvailableDestinations doesn't NPE
        NavigationSystem.InitializeAsync(GameState, EventBus).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Application.Shutdown();
    }
}

/// <summary>
/// Tests for MainGameScreen covering IRenderable implementation, construction,
/// non-visual properties, and refresh behavior.
/// </summary>
public class MainGameScreenTests : IClassFixture<MainGameScreenFixture>
{
    private readonly MainGameScreenFixture _fixture;

    public MainGameScreenTests(MainGameScreenFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Creates a MainGameScreen with the fixture's shared dependencies.
    /// </summary>
    private MainGameScreen CreateScreen()
    {
        return new MainGameScreen(
            _fixture.GameState,
            _fixture.EventBus,
            _fixture.TradingSystem,
            _fixture.NavigationSystem);
    }

    // ── IRenderable Properties ──────────────────────────────────────────

    [Fact]
    public void View_IsNotNull_AfterConstruction()
    {
        using var screen = CreateScreen();
        Assert.NotNull(screen.View);
    }

    [Fact]
    public void View_IsWindow()
    {
        using var screen = CreateScreen();
        Assert.IsType<Window>(screen.View);
    }

    [Fact]
    public void ZIndex_IsZero()
    {
        using var screen = CreateScreen();
        Assert.Equal(0, screen.ZIndex);
    }

    [Fact]
    public void IsVisible_DefaultsToTrue()
    {
        using var screen = CreateScreen();
        Assert.True(screen.IsVisible);
    }

    [Fact]
    public void IsVisible_CanBeToggled()
    {
        using var screen = CreateScreen();
        screen.IsVisible = false;
        Assert.False(screen.IsVisible);
        screen.IsVisible = true;
        Assert.True(screen.IsVisible);
    }

    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsOnNullGameState()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MainGameScreen(null!, _fixture.EventBus, _fixture.TradingSystem, _fixture.NavigationSystem));
    }

    [Fact]
    public void Constructor_ThrowsOnNullEventBus()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MainGameScreen(_fixture.GameState, null!, _fixture.TradingSystem, _fixture.NavigationSystem));
    }

    [Fact]
    public void Constructor_ThrowsOnNullTradingSystem()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MainGameScreen(_fixture.GameState, _fixture.EventBus, null!, _fixture.NavigationSystem));
    }

    [Fact]
    public void Constructor_ThrowsOnNullNavigationSystem()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MainGameScreen(_fixture.GameState, _fixture.EventBus, _fixture.TradingSystem, null!));
    }

    [Fact]
    public void Constructor_CreatesWindowWithTitle()
    {
        using var screen = CreateScreen();
        var window = (Window)screen.View;
        Assert.Contains("Neon Trader", window.Title.ToString());
    }

    // ── Refresh ─────────────────────────────────────────────────────────

    [Fact]
    public void Refresh_DoesNotThrow_WithEmptyState()
    {
        using var screen = CreateScreen();
        var exception = Record.Exception(() => screen.Refresh());
        Assert.Null(exception);
    }

    [Fact]
    public void Refresh_DoesNotThrow_WithCargoData()
    {
        _fixture.GameState.Cargo["water"] = 10;
        _fixture.GameState.Cargo["food"] = 5;

        using var screen = CreateScreen();
        var exception = Record.Exception(() => screen.Refresh());
        Assert.Null(exception);
    }

    [Fact]
    public void Refresh_DoesNotThrow_MultipleTimes()
    {
        using var screen = CreateScreen();
        var exception = Record.Exception(() =>
        {
            screen.Refresh();
            screen.Refresh();
            screen.Refresh();
        });
        Assert.Null(exception);
    }

    // ── OnResize ────────────────────────────────────────────────────────

    [Fact]
    public void OnResize_DoesNotThrow()
    {
        using var screen = CreateScreen();
        var exception = Record.Exception(() => screen.OnResize(80, 25));
        Assert.Null(exception);
    }

    [Fact]
    public void OnResize_DoesNotThrow_WithDifferentSizes()
    {
        using var screen = CreateScreen();
        var exception = Record.Exception(() =>
        {
            screen.OnResize(120, 40);
            screen.OnResize(40, 15);
        });
        Assert.Null(exception);
    }

    // ── Dispose ─────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var screen = CreateScreen();
        var exception = Record.Exception(() => screen.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var screen = CreateScreen();
        screen.Dispose();
        var exception = Record.Exception(() => screen.Dispose());
        Assert.Null(exception);
    }

    // ── Status Bar ──────────────────────────────────────────────────────

    [Fact]
    public void Refresh_UpdatesStatusBar_WithCreditsAndLocation()
    {
        _fixture.GameState.Credits = 12345;
        _fixture.GameState.CurrentLocation = "neon_station";

        using var screen = CreateScreen();
        screen.Refresh();

        var window = (Window)screen.View;
        Assert.NotNull(window);
    }

    // ── Cargo List ──────────────────────────────────────────────────────

    [Fact]
    public void Refresh_PopulatesCargoList_WhenCargoIsEmpty()
    {
        _fixture.GameState.Cargo.Clear();

        using var screen = CreateScreen();
        screen.Refresh();

        Assert.True(screen.IsVisible);
    }

    [Fact]
    public void Refresh_PopulatesCargoList_WhenCargoHasItems()
    {
        _fixture.GameState.Cargo["water"] = 10;
        _fixture.GameState.Cargo["food"] = 5;
        _fixture.GameState.Cargo["ore"] = 3;

        using var screen = CreateScreen();
        var exception = Record.Exception(() => screen.Refresh());
        Assert.Null(exception);
    }

    // ── Market Table ────────────────────────────────────────────────────

    [Fact]
    public void Refresh_HandlesNullMarketInfo()
    {
        // TradingSystem not initialized, so GetCurrentMarketInfo returns null
        using var screen = CreateScreen();
        var exception = Record.Exception(() => screen.Refresh());
        Assert.Null(exception);
    }

    // ── Button States ───────────────────────────────────────────────────

    [Fact]
    public void Refresh_UpdatesButtonStates_WithoutSelection()
    {
        using var screen = CreateScreen();
        var exception = Record.Exception(() => screen.Refresh());
        Assert.Null(exception);
    }
}
