using Microsoft.Extensions.Logging;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeonTrader.Systems;

/// <summary>
/// Save/Load system for Neon Trader.
/// Manages save slots as JSON files, handles auto-save timers,
/// and provides version tracking for future migration.
/// </summary>
public sealed class SaveSystem : IGameSystem
{
    private readonly ILogger<SaveSystem> _logger;
    private GameState _gameState = null!;
    private IEventBus _eventBus = null!;
    private bool _isRunning;

    // Subscriptions to dispose on shutdown
    private IDisposable? _saveRequestedSubscription;
    private IDisposable? _loadRequestedSubscription;
    private IDisposable? _shutdownSubscription;

    // Auto-save state
    private float _autoSaveAccumulator;
    private bool _autoSaveEnabled;
    private int _autoSaveIntervalMinutes;
    private const string AutoSaveSlotName = "autosave";

    // Save directory
    private string _saveDirectory = string.Empty;

    // Current save format version — increment when save structure changes
    private const int SaveFormatVersion = 1;

    // Game version string for metadata
    private const string GameVersion = "1.0.0";

    // IGameSystem implementation
    public string SystemId => "savesystem";
    public int Priority => 90; // Infrastructure system, runs late
    public bool IsRunning => _isRunning;

    public SaveSystem(ILogger<SaveSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the save system: create save directory, subscribe to events.
    /// </summary>
    public Task InitializeAsync(GameState gameState, IEventBus eventBus, CancellationToken cancellationToken = default)
    {
        _gameState = gameState;
        _eventBus = eventBus;
        _saveDirectory = Path.Combine(AppContext.BaseDirectory, "saves");

        // Ensure save directory exists
        try
        {
            Directory.CreateDirectory(_saveDirectory);
            _logger.LogInformation("Save directory initialized: {Directory}", _saveDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create save directory: {Directory}", _saveDirectory);
            throw;
        }

        // Read auto-save settings from game state
        _autoSaveEnabled = _gameState.Settings.AutoSave;
        _autoSaveIntervalMinutes = _gameState.Settings.AutoSaveIntervalMinutes;
        _autoSaveAccumulator = 0f;

        // Subscribe to save/load request events
        _saveRequestedSubscription = _eventBus.Subscribe<SaveRequestedEvent>(OnSaveRequested);
        _loadRequestedSubscription = _eventBus.Subscribe<LoadRequestedEvent>(OnLoadRequested);
        _shutdownSubscription = _eventBus.Subscribe<GameShutdownEvent>(_ => { /* handled in ShutdownAsync */ });

        _isRunning = true;
        _logger.LogInformation("SaveSystem initialized (auto-save: {Enabled}, interval: {Interval}min)",
            _autoSaveEnabled, _autoSaveIntervalMinutes);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Update the save system each logic tick. Handles auto-save timer.
    /// </summary>
    public Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        if (!_isRunning || !_autoSaveEnabled) return Task.CompletedTask;

        _autoSaveAccumulator += deltaTime;

        // Check if auto-save interval has elapsed (convert minutes to seconds)
        var intervalSeconds = _autoSaveIntervalMinutes * 60f;
        if (_autoSaveAccumulator >= intervalSeconds)
        {
            _autoSaveAccumulator = 0f;

            // Fire-and-forget auto-save (don't block the game loop)
            _ = Task.Run(async () =>
            {
                try
                {
                    await SaveGameAsync(AutoSaveSlotName, cancellationToken);
                    _logger.LogDebug("Auto-save completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-save failed");
                }
            }, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shutdown the save system. Performs a final auto-save and cleans up subscriptions.
    /// </summary>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;

        // Final auto-save on shutdown
        if (_autoSaveEnabled)
        {
            try
            {
                await SaveGameAsync(AutoSaveSlotName, cancellationToken);
                _logger.LogInformation("Final auto-save completed on shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final auto-save failed during shutdown");
            }
        }

        // Dispose event subscriptions
        _saveRequestedSubscription?.Dispose();
        _loadRequestedSubscription?.Dispose();
        _shutdownSubscription?.Dispose();

        _logger.LogInformation("SaveSystem shutdown complete");
    }

    // ─── Event Handlers ───────────────────────────────────────────

    /// <summary>
    /// Handle a save request from the UI or another system.
    /// </summary>
    private void OnSaveRequested(SaveRequestedEvent evt)
    {
        _logger.LogInformation("Save requested: {SaveName}", evt.SaveName);

        _ = Task.Run(async () =>
        {
            try
            {
                await SaveGameAsync(evt.SaveName);
                _eventBus.Publish(new RefreshUIEvent { Region = "save" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save failed for slot: {SaveName}", evt.SaveName);
                _eventBus.Publish(new GameErrorEvent
                {
                    Message = $"Failed to save game: {ex.Message}",
                    Exception = ex,
                    SourceSystem = SystemId
                });
            }
        });
    }

    /// <summary>
    /// Handle a load request from the UI or another system.
    /// </summary>
    private void OnLoadRequested(LoadRequestedEvent evt)
    {
        _logger.LogInformation("Load requested: {SaveName}", evt.SaveName);

        _ = Task.Run(async () =>
        {
            try
            {
                await LoadGameAsync(evt.SaveName);
                _eventBus.Publish(new RefreshUIEvent()); // Full UI refresh after load
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load failed for slot: {SaveName}", evt.SaveName);
                _eventBus.Publish(new GameErrorEvent
                {
                    Message = $"Failed to load game: {ex.Message}",
                    Exception = ex,
                    SourceSystem = SystemId
                });
            }
        });
    }

    // ─── Public API ───────────────────────────────────────────────

    /// <summary>
    /// Save the current game state to a named slot.
    /// </summary>
    /// <param name="saveName">Name of the save slot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            throw new ArgumentException("Save name cannot be empty", nameof(saveName));

        // Sanitize the save name for use as a filename
        var safeName = SanitizeSaveName(saveName);
        var filePath = GetSaveFilePath(safeName);

        // Build the save data structure
        var saveData = BuildSaveData(safeName);

        // Serialize to JSON with indentation for readability
        var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

        // Write atomically: write to temp file, then rename
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);

        // Atomic rename (overwrites existing)
        File.Move(tempPath, filePath, overwrite: true);

        _logger.LogInformation("Game saved to slot: {SaveName} ({Bytes} bytes)", saveName, json.Length);
    }

    /// <summary>
    /// Load a game state from a named slot.
    /// </summary>
    /// <param name="saveName">Name of the save slot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LoadGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            throw new ArgumentException("Save name cannot be empty", nameof(saveName));

        var safeName = SanitizeSaveName(saveName);
        var filePath = GetSaveFilePath(safeName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {saveName}", filePath);

        // Read the save file
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var saveData = JObject.Parse(json);

        // Validate and migrate if needed
        var metadata = saveData["metadata"] as JObject;
        if (metadata == null)
            throw new InvalidDataException("Save file is missing metadata");

        var fileVersion = metadata["saveFormatVersion"]?.ToObject<int>() ?? 0;
        if (fileVersion > SaveFormatVersion)
            throw new InvalidDataException(
                $"Save file version {fileVersion} is newer than supported version {SaveFormatVersion}");

        // Apply migration if the save file is from an older version
        var migratedData = MigrateSaveData(saveData, fileVersion);

        // Deserialize game state
        var gameStateData = migratedData["gameState"] as JObject;
        if (gameStateData == null)
            throw new InvalidDataException("Save file is missing gameState data");

        _gameState.Deserialize(gameStateData);

        // Reset auto-save accumulator after load
        _autoSaveAccumulator = 0f;

        // Update auto-save settings from loaded state
        _autoSaveEnabled = _gameState.Settings.AutoSave;
        _autoSaveIntervalMinutes = _gameState.Settings.AutoSaveIntervalMinutes;

        _logger.LogInformation("Game loaded from slot: {SaveName} (version {Version})",
            saveName, fileVersion);
    }

    /// <summary>
    /// Delete a save slot.
    /// </summary>
    /// <param name="saveName">Name of the save slot to delete</param>
    public bool DeleteSave(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            throw new ArgumentException("Save name cannot be empty", nameof(saveName));

        var safeName = SanitizeSaveName(saveName);
        var filePath = GetSaveFilePath(safeName);

        if (!File.Exists(filePath))
            return false;

        File.Delete(filePath);
        _logger.LogInformation("Save deleted: {SaveName}", saveName);
        return true;
    }

    /// <summary>
    /// List all available save slots with metadata.
    /// </summary>
    /// <returns>List of save slot info, sorted by last modified (newest first)</returns>
    public List<SaveSlotInfo> ListSaves()
    {
        var saves = new List<SaveSlotInfo>();

        if (!Directory.Exists(_saveDirectory))
            return saves;

        foreach (var filePath in Directory.GetFiles(_saveDirectory, "*.json"))
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileInfo = new FileInfo(filePath);

                // Read just the metadata portion for quick listing
                var json = File.ReadAllText(filePath);
                var saveData = JObject.Parse(json);
                var metadata = saveData["metadata"] as JObject;

                saves.Add(new SaveSlotInfo
                {
                    SaveName = fileName,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    CreatedAt = metadata?["createdAt"]?.ToObject<DateTime>() ?? fileInfo.CreationTimeUtc,
                    UpdatedAt = metadata?["updatedAt"]?.ToObject<DateTime>() ?? fileInfo.LastWriteTimeUtc,
                    PlayerName = metadata?["playerName"]?.ToString() ?? "Unknown",
                    CurrentLocation = metadata?["currentLocation"]?.ToString() ?? "Unknown",
                    Credits = metadata?["credits"]?.ToObject<long>() ?? 0,
                    PlayTime = metadata?["playTime"] != null
                        ? TimeSpan.Parse(metadata["playTime"]!.ToString())
                        : TimeSpan.Zero,
                    SaveFormatVersion = metadata?["saveFormatVersion"]?.ToObject<int>() ?? 0,
                    GameVersion = metadata?["gameVersion"]?.ToString() ?? "Unknown"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read save metadata for: {File}", filePath);
                // Include corrupted saves in the list so the UI can show them
                saves.Add(new SaveSlotInfo
                {
                    SaveName = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    IsCorrupted = true,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Sort by last modified, newest first
        saves.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
        return saves;
    }

    /// <summary>
    /// Get detailed info for a specific save slot.
    /// </summary>
    public SaveSlotInfo? GetSaveInfo(string saveName)
    {
        var safeName = SanitizeSaveName(saveName);
        var filePath = GetSaveFilePath(safeName);

        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var saveData = JObject.Parse(json);
            var metadata = saveData["metadata"] as JObject;
            var fileInfo = new FileInfo(filePath);

            return new SaveSlotInfo
            {
                SaveName = safeName,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                CreatedAt = metadata?["createdAt"]?.ToObject<DateTime>() ?? fileInfo.CreationTimeUtc,
                UpdatedAt = metadata?["updatedAt"]?.ToObject<DateTime>() ?? fileInfo.LastWriteTimeUtc,
                PlayerName = metadata?["playerName"]?.ToString() ?? "Unknown",
                CurrentLocation = metadata?["currentLocation"]?.ToString() ?? "Unknown",
                Credits = metadata?["credits"]?.ToObject<long>() ?? 0,
                PlayTime = metadata?["playTime"] != null
                    ? TimeSpan.Parse(metadata["playTime"]!.ToString())
                    : TimeSpan.Zero,
                SaveFormatVersion = metadata?["saveFormatVersion"]?.ToObject<int>() ?? 0,
                GameVersion = metadata?["gameVersion"]?.ToString() ?? "Unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read save info for: {SaveName}", saveName);
            return new SaveSlotInfo
            {
                SaveName = safeName,
                FilePath = filePath,
                IsCorrupted = true,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Check if a save slot exists.
    /// </summary>
    public bool SaveExists(string saveName)
    {
        var safeName = SanitizeSaveName(saveName);
        return File.Exists(GetSaveFilePath(safeName));
    }

    /// <summary>
    /// Get the total number of save slots.
    /// </summary>
    public int SaveCount
    {
        get
        {
            if (!Directory.Exists(_saveDirectory)) return 0;
            return Directory.GetFiles(_saveDirectory, "*.json").Length;
        }
    }

    /// <summary>
    /// Update auto-save settings at runtime.
    /// </summary>
    public void UpdateAutoSaveSettings(bool enabled, int intervalMinutes)
    {
        _autoSaveEnabled = enabled;
        _autoSaveIntervalMinutes = Math.Max(1, intervalMinutes); // Minimum 1 minute
        _autoSaveAccumulator = 0f; // Reset accumulator on settings change

        // Also update the game state settings so they persist on next save
        _gameState.Settings.AutoSave = enabled;
        _gameState.Settings.AutoSaveIntervalMinutes = _autoSaveIntervalMinutes;

        _logger.LogInformation("Auto-save settings updated: Enabled={Enabled}, Interval={Interval}min",
            enabled, _autoSaveIntervalMinutes);
    }

    // ─── Private Helpers ──────────────────────────────────────────

    /// <summary>
    /// Build the complete save data structure from current game state.
    /// </summary>
    private JObject BuildSaveData(string saveName)
    {
        var now = DateTime.UtcNow;
        var existingMetadata = TryReadExistingMetadata(saveName);
        var createdAt = existingMetadata?.createdAt ?? now;

        // Calculate play time
        var playTime = _gameState.Statistics.TotalPlayTime;
        if (existingMetadata?.playTime != null)
        {
            // Add elapsed time since last save
            var elapsed = now - (existingMetadata.Value.updatedAt ?? createdAt);
            playTime = existingMetadata.Value.playTime + elapsed;
        }

        var metadata = new JObject
        {
            ["saveFormatVersion"] = SaveFormatVersion,
            ["gameVersion"] = GameVersion,
            ["saveName"] = saveName,
            ["createdAt"] = createdAt.ToString("o"),
            ["updatedAt"] = now.ToString("o"),
            ["playerName"] = _gameState.PlayerName,
            ["currentLocation"] = _gameState.CurrentLocation,
            ["credits"] = _gameState.Credits,
            ["playTime"] = playTime.ToString()
        };

        return new JObject
        {
            ["metadata"] = metadata,
            ["gameState"] = _gameState.Serialize()
        };
    }

    /// <summary>
    /// Try to read existing metadata for a save slot (for preserving createdAt).
    /// </summary>
    private (DateTime createdAt, DateTime? updatedAt, TimeSpan playTime)? TryReadExistingMetadata(string saveName)
    {
        var filePath = GetSaveFilePath(saveName);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var saveData = JObject.Parse(json);
            var metadata = saveData["metadata"] as JObject;
            if (metadata == null) return null;

            var createdAt = metadata["createdAt"]?.ToObject<DateTime>() ?? DateTime.UtcNow;
            var updatedAt = metadata["updatedAt"]?.ToObject<DateTime>();
            var playTime = metadata["playTime"] != null
                ? TimeSpan.Parse(metadata["playTime"]!.ToString())
                : TimeSpan.Zero;

            return (createdAt, updatedAt, playTime);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Migrate save data from an older format version to the current version.
    /// Add migration steps here as the save format evolves.
    /// </summary>
    private JObject MigrateSaveData(JObject saveData, int fileVersion)
    {
        if (fileVersion == SaveFormatVersion)
            return saveData; // No migration needed

        var migrated = saveData.DeepClone() as JObject ?? saveData;

        // Version 0 → 1 migration placeholder
        // Example: if (fileVersion == 0) { /* add new fields */ }

        _logger.LogInformation("Save data migrated from version {From} to {To}",
            fileVersion, SaveFormatVersion);

        return migrated;
    }

    /// <summary>
    /// Get the full file path for a save slot.
    /// </summary>
    private string GetSaveFilePath(string safeName)
    {
        return Path.Combine(_saveDirectory, safeName + ".json");
    }

    /// <summary>
    /// Sanitize a save name for use as a filename.
    /// Replaces invalid characters with underscores.
    /// </summary>
    private static string SanitizeSaveName(string name)
    {
        // Replace characters that are invalid in filenames
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Also replace spaces with underscores for cleaner filenames
        sanitized = sanitized.Replace(' ', '_');

        // Ensure non-empty
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "unnamed_save";

        return sanitized;
    }
}

/// <summary>
/// Information about a save slot for display in the load game menu.
/// </summary>
public sealed class SaveSlotInfo
{
    /// <summary>Name of the save slot</summary>
    public string SaveName { get; init; } = string.Empty;

    /// <summary>Full file path to the save file</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>File size in bytes</summary>
    public long FileSize { get; init; }

    /// <summary>When the save was first created</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>When the save was last updated</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>Player name at time of save</summary>
    public string PlayerName { get; init; } = "Unknown";

    /// <summary>Current location at time of save</summary>
    public string CurrentLocation { get; init; } = "Unknown";

    /// <summary>Credits at time of save</summary>
    public long Credits { get; init; }

    /// <summary>Total play time at time of save</summary>
    public TimeSpan PlayTime { get; init; }

    /// <summary>Save format version (for migration tracking)</summary>
    public int SaveFormatVersion { get; init; }

    /// <summary>Game version that created this save</summary>
    public string GameVersion { get; init; } = "Unknown";

    /// <summary>Whether the save file is corrupted/unreadable</summary>
    public bool IsCorrupted { get; init; }

    /// <summary>Error message if corrupted</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Formatted display string for UI</summary>
    public string DisplayString =>
        IsCorrupted
            ? $"{SaveName} [CORRUPTED: {ErrorMessage}]"
            : $"{SaveName} | {PlayerName} | {CurrentLocation} | {Credits:N0} cr | {PlayTime:hh\\:mm} | {UpdatedAt:yyyy-MM-dd HH:mm}";
}
