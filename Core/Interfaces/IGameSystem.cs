using NeonTrader.Core;
using NeonTrader.Core.Events;

namespace NeonTrader.Core.Interfaces;

/// <summary>
/// Base interface for all game systems.
/// Game systems are independent components that handle specific game functionality
/// (rendering, input, market simulation, save/load, etc.)
/// </summary>
public interface IGameSystem
{
    /// <summary>
    /// Unique identifier for this system
    /// </summary>
    string SystemId { get; }

    /// <summary>
    /// Priority for system initialization/update order (lower = earlier)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Initialize the system. Called once at startup.
    /// </summary>
    /// <param name="gameState">Shared game state</param>
    /// <param name="eventBus">Event bus for inter-system communication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(GameState gameState, IEventBus eventBus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the system. Called every game loop iteration.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown the system. Called once at game exit.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the system is currently running
    /// </summary>
    bool IsRunning { get; }
}