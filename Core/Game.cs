using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeonTrader.Core.Interfaces;
using NeonTrader.Core.Events;
using Terminal.Gui;
using NeonTrader.Views;

namespace NeonTrader.Core;

/// <summary>
/// Main game class that manages the game loop, systems, and overall game state.
/// </summary>
public sealed class Game : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Game> _logger;
    private readonly GameState _gameState;
    private readonly IEventBus _eventBus;
    private readonly List<IGameSystem> _systems = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    // Terminal.Gui integration
    private readonly List<IRenderable> _screens = new();
    private IRenderable? _currentScreen;
    
    // Screen references (set after construction via DI)
    private MainGameScreen? _mainGameScreen;
    private StationScreen? _stationScreen;
    private CharacterScreen? _characterScreen;
    
    // Game loop timing
    private const double TargetUIFrameTime = 1.0 / 60.0; // 60 FPS
    private const double TargetLogicFrameTime = 1.0 / 10.0; // 10 logic ticks per second
    private DateTime _lastUIFrameTime;
    private DateTime _lastLogicFrameTime;
    private double _accumulatedUITime;
    private double _accumulatedLogicTime;
    
    // State
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Current game state
    /// </summary>
    public GameState State => _gameState;

    /// <summary>
    /// Event bus for system communication
    /// </summary>
    public IEventBus EventBus => _eventBus;

    /// <summary>
    /// Whether the game is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initialize a new game instance
    /// </summary>
    public Game(
        IServiceProvider serviceProvider,
        ILogger<Game> logger,
        GameState gameState,
        IEventBus eventBus)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _gameState = gameState;
        _eventBus = eventBus;
        
        _lastUIFrameTime = DateTime.UtcNow;
        _lastLogicFrameTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Register a game system
    /// </summary>
    public void RegisterSystem(IGameSystem system)
    {
        _systems.Add(system);
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        _logger.LogInformation("Registered system: {SystemId} (priority: {Priority})", system.SystemId, system.Priority);
    }

    /// <summary>
    /// Register the main game screen
    /// </summary>
    public void RegisterMainGameScreen(MainGameScreen screen)
    {
        _mainGameScreen = screen;
    }

    /// <summary>
    /// Register the station screen
    /// </summary>
    public void RegisterStationScreen(StationScreen screen)
    {
        _stationScreen = screen;
    }

    /// <summary>
    /// Register the character screen
    /// </summary>
    public void RegisterCharacterScreen(CharacterScreen screen)
    {
        _characterScreen = screen;
    }

    /// <summary>
    /// Push a screen onto the navigation stack and make it active
    /// </summary>
    public void PushScreen(IRenderable screen)
    {
        if (_currentScreen != null)
        {
            _currentScreen.IsVisible = false;
        }
        
        _currentScreen = screen;
        _currentScreen.IsVisible = true;
        _currentScreen.Refresh();
        
        _logger.LogInformation("Screen pushed: {ScreenType}", screen.GetType().Name);
    }

    /// <summary>
    /// Pop the current screen and return to the previous one
    /// </summary>
    public void PopScreen()
    {
        if (_currentScreen != null)
        {
            _currentScreen.IsVisible = false;
        }
        
        _currentScreen = _mainGameScreen;
        if (_currentScreen != null)
        {
            _currentScreen.IsVisible = true;
            _currentScreen.Refresh();
        }
        
        _logger.LogInformation("Screen popped to main");
    }

    /// <summary>
    /// Navigate to a specific screen by type
    /// </summary>
    public void NavigateTo(string screenName)
    {
        switch (screenName.ToLowerInvariant())
        {
            case "main":
            case "maingamescreen":
                if (_mainGameScreen != null) PushScreen(_mainGameScreen);
                break;
            case "station":
            case "stationscreen":
                if (_stationScreen != null) PushScreen(_stationScreen);
                break;
            case "character":
            case "characterscreen":
                if (_characterScreen != null) PushScreen(_characterScreen);
                break;
            default:
                _logger.LogWarning("Unknown screen: {ScreenName}", screenName);
                break;
        }
    }

    /// <summary>
    /// Set up global keybindings for screen navigation and game actions
    /// </summary>
    private void SetupKeybindings()
    {
        var top = Application.Top;
        if (top == null) return;

        // F1 — Main Game Screen
        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.F1)
            {
                args.Handled = true;
                NavigateTo("main");
            }
        };

        // F2 — Station Screen
        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.F2)
            {
                args.Handled = true;
                NavigateTo("station");
            }
        };

        // F3 — Character Screen
        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.F3)
            {
                args.Handled = true;
                NavigateTo("character");
            }
        };

        // F5 — Quick Save
        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.F5)
            {
                args.Handled = true;
                _eventBus.Publish(new SaveRequestedEvent { SaveName = "quicksave" });
            }
        };

        // F9 — Quick Load
        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.F9)
            {
                args.Handled = true;
                _eventBus.Publish(new LoadRequestedEvent { SaveName = "quicksave" });
            }
        };

        // Esc — Quit
        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };
    }

    /// <summary>
    /// Run the game using Terminal.Gui's Application.Run().
    /// Initializes systems, sets up the GUI, and runs the main event loop.
    /// Blocks until the user quits (Esc) or Application.RequestStop() is called.
    /// </summary>
    public void Run()
    {
        _isRunning = true;

        try
        {
            InitializeSystemsAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
            _eventBus.Publish(new GameStartedEvent());

            var top = Application.Top;
            
            // Set up global keybindings (Esc to quit, F-keys for navigation)
            SetupKeybindings();
            
            // Diagnostic: add a simple label to verify rendering works
            var testLabel = new Label("Neon Trader — press Esc to quit")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1,
                ColorScheme = Colors.Menu
            };
            top.Add(testLabel);
            
            // Add main screen below the label (if it was constructed successfully)
            if (_mainGameScreen != null)
            {
                _mainGameScreen.View.Y = 1;
                _mainGameScreen.View.Height = Dim.Fill() - 1;
                top.Add(_mainGameScreen.View);
            }
            else
            {
                // Fallback: show a message if the main screen failed to load
                var fallbackLabel = new Label("Main screen failed to load. Press Esc to quit.")
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = 1,
                    ColorScheme = Colors.Error
                };
                top.Add(fallbackLabel);
            }

            Application.Run();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Game crashed");
            throw;
        }
        finally
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Run the game asynchronously (legacy — prefer Run() for Terminal.Gui integration).
    /// Uses the internal game loop without Terminal.Gui.
    /// </summary>
    public async Task RunAsync()
    {
        _logger.LogInformation("Starting Neon Trader (async mode)...");
        _isRunning = true;

        try
        {
            // Initialize all systems
            await InitializeSystemsAsync(_cancellationTokenSource.Token);
            
            // Fire game started event
            _eventBus.Publish(new GameStartedEvent());
            
            _logger.LogInformation("Game initialized. Starting main loop...");
            
            // Main game loop
            await MainLoopAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Game loop crashed");
            _eventBus.Publish(new GameErrorEvent
            {
                Message = "Game loop crashed",
                Exception = ex,
                SourceSystem = "Game"
            });
            throw;
        }
        finally
        {
            await ShutdownAsync();
        }
    }

    /// <summary>
    /// Initialize all registered systems
    /// </summary>
    private async Task InitializeSystemsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing {Count} game systems...", _systems.Count);
        
        foreach (var system in _systems)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            try
            {
                await system.InitializeAsync(_gameState, _eventBus, cancellationToken);
                _eventBus.Publish(new SystemInitializedEvent { SystemId = system.SystemId });
                _logger.LogInformation("System initialized: {SystemId}", system.SystemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize system: {SystemId}", system.SystemId);
                _eventBus.Publish(new GameErrorEvent
                {
                    Message = $"Failed to initialize system: {system.SystemId}",
                    Exception = ex,
                    SourceSystem = system.SystemId
                });
                throw;
            }
        }
    }

    /// <summary>
    /// Main game loop with separate UI and logic tick rates
    /// </summary>
    private async Task MainLoopAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var uiDeltaTime = (now - _lastUIFrameTime).TotalSeconds;
            var logicDeltaTime = (now - _lastLogicFrameTime).TotalSeconds;
            
            _lastUIFrameTime = now;
            _lastLogicFrameTime = now;
            
            _accumulatedUITime += uiDeltaTime;
            _accumulatedLogicTime += logicDeltaTime;

            // Run logic updates at fixed rate
            while (_accumulatedLogicTime >= TargetLogicFrameTime)
            {
                await UpdateLogicAsync((float)TargetLogicFrameTime, cancellationToken);
                _accumulatedLogicTime -= TargetLogicFrameTime;
            }

            // Run UI updates at fixed rate
            while (_accumulatedUITime >= TargetUIFrameTime)
            {
                await UpdateUIAsync((float)TargetUIFrameTime, cancellationToken);
                _accumulatedUITime -= TargetUIFrameTime;
            }

            // Small sleep to prevent busy waiting
            var nextUIFrame = _lastUIFrameTime.AddSeconds(TargetUIFrameTime);
            var nextLogicFrame = _lastLogicFrameTime.AddSeconds(TargetLogicFrameTime);
            var nextFrame = nextUIFrame < nextLogicFrame ? nextUIFrame : nextLogicFrame;
            var sleepTime = nextFrame - DateTime.UtcNow;
            
            if (sleepTime > TimeSpan.Zero)
            {
                await Task.Delay(sleepTime, cancellationToken);
            }
            
            // Yield to allow other tasks to run
            await Task.Yield();
        }
    }

    /// <summary>
    /// Update game logic (runs at ~10 Hz)
    /// </summary>
    private async Task UpdateLogicAsync(float deltaTime, CancellationToken cancellationToken)
    {
        foreach (var system in _systems.Where(s => s.IsRunning))
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            try
            {
                await system.UpdateAsync(deltaTime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in logic update for system: {SystemId}", system.SystemId);
                _eventBus.Publish(new GameErrorEvent
                {
                    Message = $"Error in logic update: {ex.Message}",
                    Exception = ex,
                    SourceSystem = system.SystemId
                });
            }
        }

        // Advance game time
        AdvanceGameTime(deltaTime);
    }

    /// <summary>
    /// Update UI (runs at ~60 Hz)
    /// </summary>
    private async Task UpdateUIAsync(float deltaTime, CancellationToken cancellationToken)
    {
        // UI updates are handled by Terminal.Gui's event loop
        // This is a placeholder for any non-Terminal.Gui UI updates
        await Task.CompletedTask;
    }

    /// <summary>
    /// Advance the game time
    /// </summary>
    private void AdvanceGameTime(float realDeltaTime)
    {
        var gameDeltaTime = TimeSpan.FromSeconds(realDeltaTime * _gameState.TimeScale.TotalSeconds);
        var previousTime = _gameState.GameTime;
        _gameState.GameTime = _gameState.GameTime.Add(gameDeltaTime);
        
        _eventBus.Publish(new TimeAdvancedEvent
        {
            NewTime = _gameState.GameTime,
            DeltaTime = gameDeltaTime
        });
    }

    /// <summary>
    /// Request the game to stop
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Stop requested");
        _isRunning = false;
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Shutdown all systems and clean up
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (!_isRunning) return;
        
        _logger.LogInformation("Shutting down game...");
        _isRunning = false;
        
        // Fire shutdown event
        _eventBus.Publish(new GameShutdownEvent());
        
        // Shutdown systems in reverse priority order
        var shutdownTasks = _systems
            .OrderByDescending(s => s.Priority)
            .Select(s => ShutdownSystemAsync(s));
        
        await Task.WhenAll(shutdownTasks);
        
        _eventBus.Publish(new SystemShutdownEvent { SystemId = "All" });
        _logger.LogInformation("Game shutdown complete");
    }

    /// <summary>
    /// Shutdown a single system
    /// </summary>
    private async Task ShutdownSystemAsync(IGameSystem system)
    {
        try
        {
            await system.ShutdownAsync(_cancellationTokenSource.Token);
            _eventBus.Publish(new SystemShutdownEvent { SystemId = system.SystemId });
            _logger.LogInformation("System shutdown: {SystemId}", system.SystemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down system: {SystemId}", system.SystemId);
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _eventBus.Dispose();
    }
}