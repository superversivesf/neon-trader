using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Interfaces;
using NeonTrader.Core.Events;
using NeonTrader.Models;
using NeonTrader.Systems;
using NeonTrader.Views;
using Terminal.Gui;

namespace NeonTrader;

/// <summary>
/// Entry point for the Neon Trader application.
/// Sets up dependency injection, configures services, and starts the game.
/// </summary>
public sealed class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        // Initialize Terminal.Gui console driver BEFORE any view construction
        Application.Init();
        
        // Build the host with DI container
        var host = CreateHostBuilder(args).Build();

        // Get the game instance
        var game = host.Services.GetRequiredService<Game>();
        
        // Register all game systems (sorted by priority: lower = earlier)
        var dataLoader = host.Services.GetRequiredService<DataLoader>();
        game.RegisterSystem(dataLoader);
        
        var saveSystem = host.Services.GetRequiredService<SaveSystem>();
        game.RegisterSystem(saveSystem);
        
        var tradingSystem = host.Services.GetRequiredService<TradingSystem>();
        game.RegisterSystem(tradingSystem);
        
        var navigationSystem = host.Services.GetRequiredService<NavigationSystem>();
        game.RegisterSystem(navigationSystem);
        
        var combatSystem = host.Services.GetRequiredService<CombatSystem>();
        game.RegisterSystem(combatSystem);
        
        var missionSystem = host.Services.GetRequiredService<MissionSystem>();
        game.RegisterSystem(missionSystem);
        
        // Register screens with the game for navigation
        var mainGameScreen = host.Services.GetRequiredService<MainGameScreen>();
        game.RegisterMainGameScreen(mainGameScreen);
        
        var stationScreen = host.Services.GetRequiredService<StationScreen>();
        game.RegisterStationScreen(stationScreen);
        
        var characterScreen = host.Services.GetRequiredService<CharacterScreen>();
        game.RegisterCharacterScreen(characterScreen);
        
        try
        {
            // Run the game with Terminal.Gui integration (blocks until quit)
            game.Run();
            return 0;
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Game crashed");
            return 1;
        }
        finally
        {
            Application.Shutdown();
        }
    }

    /// <summary>
    /// Creates the host builder with all services configured.
    /// </summary>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Core game services
                services.AddSingleton<Game>();
                services.AddSingleton<GameState>();
                services.AddSingleton<IEventBus, EventBus>();
                
                // Player model (needed by CharacterScreen)
                services.AddSingleton<Player>();
                
                // Reputation model (needed by StationScreen)
                services.AddSingleton<Reputation>();
                
                // Game systems (registered in priority order)
                services.AddSingleton<DataLoader>();       // Priority 0
                services.AddSingleton<SaveSystem>();       // Priority 10
                services.AddSingleton<TradingSystem>();    // Priority 10
                services.AddSingleton<NavigationSystem>(); // Priority 20
                services.AddSingleton<CombatSystem>();      // Priority 30
                services.AddSingleton<MissionSystem>();    // Priority 40
                
                // UI screens
                services.AddSingleton<MainGameScreen>();
                services.AddSingleton<StationScreen>();
                services.AddSingleton<CharacterScreen>();
                
                // Logging — disabled for TUI (console output corrupts Terminal.Gui rendering)
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.None);
                });
            });
}