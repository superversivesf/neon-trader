using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;

namespace NeonTrader.Systems;

/// <summary>
/// DataLoader system - loads all JSON data files into their respective registries at startup.
/// This system must initialize first (Priority 0) so all other systems have access to game data.
/// </summary>
public sealed class DataLoader : IGameSystem
{
    private readonly ILogger<DataLoader> _logger;
    private GameState? _gameState;
    private IEventBus? _eventBus;
    private bool _isRunning;

    /// <summary>
    /// Unique system identifier
    /// </summary>
    public string SystemId => "DataLoader";

    /// <summary>
    /// Highest priority - must initialize before all other systems
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// Whether the system is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    public DataLoader(ILogger<DataLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the system - load all JSON data files into registries
    /// </summary>
    public async Task InitializeAsync(
        GameState gameState,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        _gameState = gameState;
        _eventBus = eventBus;
        _isRunning = true;

        _logger.LogInformation("DataLoader initializing - loading game data files...");

        try
        {
            // Load data files in dependency order
            // Commodities first (no dependencies)
            await LoadCommoditiesAsync(cancellationToken);
            
            // Factions (no dependencies on other registries)
            await LoadFactionsAsync(cancellationToken);
            
            // Skills (no dependencies on other registries)
            await LoadSkillsAsync(cancellationToken);
            
            // Equipment (may reference faction IDs for requirements)
            await LoadEquipmentAsync(cancellationToken);
            
            // Ship classes (may reference equipment IDs for default loadouts)
            await LoadShipClassesAsync(cancellationToken);
            
            // Planets last (references faction IDs, economy types)
            await LoadPlanetsAsync(cancellationToken);

            _logger.LogInformation(
                "DataLoader initialized successfully. " +
                "Commodities: {CommodityCount}, Factions: {FactionCount}, " +
                "Skills: {SkillCount}, Equipment: {EquipmentCount}, " +
                "ShipClasses: {ShipClassCount}, Planets: {PlanetCount}",
                CommodityRegistry.All.Count,
                FactionRegistry.All.Count,
                SkillRegistry.All.Count,
                EquipmentRegistry.All.Count,
                ShipClassRegistry.All.Count,
                PlanetRegistry.All.Count);

            _eventBus.Publish(new DataLoadedEvent
            {
                CommodityCount = CommodityRegistry.All.Count,
                FactionCount = FactionRegistry.All.Count,
                SkillCount = SkillRegistry.All.Count,
                EquipmentCount = EquipmentRegistry.All.Count,
                ShipClassCount = ShipClassRegistry.All.Count,
                PlanetCount = PlanetRegistry.All.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "DataLoader failed to initialize");
            _isRunning = false;
            throw;
        }
    }

    /// <summary>
    /// Update - no-op for DataLoader (data is static after load)
    /// </summary>
    public Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shutdown - no-op for DataLoader
    /// </summary>
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        _logger.LogInformation("DataLoader shutdown");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads commodities from Data/Commodities.json into CommodityRegistry
    /// </summary>
    private async Task LoadCommoditiesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading commodities...");
        var json = await ReadDataFileAsync("Data/Commodities.json", cancellationToken);
        CommodityRegistry.LoadFromJson(json);
        _logger.LogInformation("Loaded {Count} commodities", CommodityRegistry.All.Count);
    }

    /// <summary>
    /// Loads factions from Data/Factions.json into FactionRegistry
    /// </summary>
    private async Task LoadFactionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading factions...");
        var json = await ReadDataFileAsync("Data/Factions.json", cancellationToken);
        FactionRegistry.LoadFromJson(json);
        _logger.LogInformation("Loaded {Count} factions", FactionRegistry.All.Count);
    }

    /// <summary>
    /// Loads skills from Data/Skills.json into SkillRegistry
    /// </summary>
    private async Task LoadSkillsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading skills...");
        var json = await ReadDataFileAsync("Data/Skills.json", cancellationToken);
        SkillRegistry.LoadFromJson(json);
        _logger.LogInformation("Loaded {Count} skill definitions", SkillRegistry.All.Count);
    }

    /// <summary>
    /// Loads equipment from Data/Equipment.json into EquipmentRegistry
    /// </summary>
    private async Task LoadEquipmentAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading equipment...");
        var json = await ReadDataFileAsync("Data/Equipment.json", cancellationToken);
        EquipmentRegistry.LoadFromJson(json);
        _logger.LogInformation("Loaded {Count} equipment items", EquipmentRegistry.All.Count);
    }

    /// <summary>
    /// Loads ship classes from Data/Ships.json into ShipClassRegistry
    /// </summary>
    private async Task LoadShipClassesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading ship classes...");
        var json = await ReadDataFileAsync("Data/Ships.json", cancellationToken);
        ShipClassRegistry.LoadFromJson(json);
        _logger.LogInformation("Loaded {Count} ship classes", ShipClassRegistry.All.Count);
    }

    /// <summary>
    /// Loads planets from Data/Planets.json into PlanetRegistry
    /// </summary>
    private async Task LoadPlanetsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading planets...");
        var json = await ReadDataFileAsync("Data/Planets.json", cancellationToken);
        PlanetRegistry.LoadFromJson(json);
        _logger.LogInformation("Loaded {Count} planets", PlanetRegistry.All.Count);
    }

    /// <summary>
    /// Reads a data file from disk asynchronously
    /// </summary>
    private static async Task<string> ReadDataFileAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);

        // Fallback: if not found relative to BaseDirectory, try current directory
        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Data file not found: {relativePath}. " +
                $"Searched: {Path.Combine(AppContext.BaseDirectory, relativePath)} " +
                $"and {Path.Combine(Directory.GetCurrentDirectory(), relativePath)}");
        }

        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }
}

/// <summary>
/// Event fired when all data files have been loaded into registries
/// </summary>
public sealed record DataLoadedEvent : GameEvent
{
    /// <summary>Number of commodities loaded</summary>
    public int CommodityCount { get; init; }

    /// <summary>Number of factions loaded</summary>
    public int FactionCount { get; init; }

    /// <summary>Number of skill definitions loaded</summary>
    public int SkillCount { get; init; }

    /// <summary>Number of equipment items loaded</summary>
    public int EquipmentCount { get; init; }

    /// <summary>Number of ship classes loaded</summary>
    public int ShipClassCount { get; init; }

    /// <summary>Number of planets loaded</summary>
    public int PlanetCount { get; init; }
}
