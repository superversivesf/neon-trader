using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Interfaces;
using NeonTrader.Core.Events;
using NeonTrader.Models;
using NeonTrader.Systems;
using Terminal.Gui;

// Test 15: DataLoader init BEFORE Application.Init()
public static class MinimalTest15
{
    public static async Task Main()
    {
        Console.Error.WriteLine("=== Starting Test 15 ===");

        // Do ALL async work BEFORE Application.Init()
        var services = new ServiceCollection();
        services.AddSingleton<GameState>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<DataLoader>();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        var provider = services.BuildServiceProvider();

        var gameState = provider.GetRequiredService<GameState>();
        var eventBus = provider.GetRequiredService<IEventBus>();
        var dataLoader = provider.GetRequiredService<DataLoader>();

        Console.Error.WriteLine("Calling InitializeAsync with await (BEFORE Application.Init)...");
        try
        {
            await dataLoader.InitializeAsync(gameState, eventBus, CancellationToken.None);
            Console.Error.WriteLine("DataLoader init SUCCESS");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DataLoader init FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        // NOW init Terminal.Gui
        Console.Error.WriteLine("Calling Application.Init()...");
        Application.Init();

        var top = Application.Top;
        var testLabel = new Label("TEST 15: Init BEFORE Terminal.Gui — press Esc to quit")
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = 1,
            ColorScheme = Colors.Menu
        };
        top.Add(testLabel);

        top.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };

        Console.Error.WriteLine("=== About to call Application.Run() ===");
        Application.Run();
        Application.Shutdown();
    }
}
