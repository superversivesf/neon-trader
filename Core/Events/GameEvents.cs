namespace NeonTrader.Core.Events;

/// <summary>
/// Base class for all game events
/// </summary>
public abstract record GameEvent
{
    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional correlation ID for tracking related events
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Event fired when the game starts
/// </summary>
public sealed record GameStartedEvent : GameEvent;

/// <summary>
/// Event fired when the game is shutting down
/// </summary>
public sealed record GameShutdownEvent : GameEvent;

/// <summary>
/// Event fired when the player moves to a new location
/// </summary>
public sealed record LocationChangedEvent : GameEvent
{
    public required string PreviousLocation { get; init; }
    public required string NewLocation { get; init; }
}

/// <summary>
/// Event fired when game time advances
/// </summary>
public sealed record TimeAdvancedEvent : GameEvent
{
    public required DateTime NewTime { get; init; }
    public required TimeSpan DeltaTime { get; init; }
}

/// <summary>
/// Event fired when the player's credits change
/// </summary>
public sealed record CreditsChangedEvent : GameEvent
{
    public required long PreviousCredits { get; init; }
    public required long NewCredits { get; init; }
    public required long Delta { get; init; }
}

/// <summary>
/// Event fired when market prices update
/// </summary>
public sealed record MarketUpdatedEvent : GameEvent
{
    public required string CommodityId { get; init; }
    public required decimal PreviousPrice { get; init; }
    public required decimal NewPrice { get; init; }
}

/// <summary>
/// Event fired when a trade is executed
/// </summary>
public sealed record TradeExecutedEvent : GameEvent
{
    public required string CommodityId { get; init; }
    public required int Quantity { get; init; }
    public required decimal PricePerUnit { get; init; }
    public required bool IsBuy { get; init; }
    public required long TotalCost { get; init; }
}

/// <summary>
/// Event fired when cargo changes
/// </summary>
public sealed record CargoChangedEvent : GameEvent
{
    public required string CommodityId { get; init; }
    public required int PreviousQuantity { get; init; }
    public required int NewQuantity { get; init; }
}

/// <summary>
/// Event fired when a system initializes
/// </summary>
public sealed record SystemInitializedEvent : GameEvent
{
    public required string SystemId { get; init; }
}

/// <summary>
/// Event fired when a system shuts down
/// </summary>
public sealed record SystemShutdownEvent : GameEvent
{
    public required string SystemId { get; init; }
}

/// <summary>
/// Event fired when an error occurs
/// </summary>
public sealed record GameErrorEvent : GameEvent
{
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
    public required string SourceSystem { get; init; }
}

/// <summary>
/// Event fired when the player takes damage
/// </summary>
public sealed record PlayerDamagedEvent : GameEvent
{
    public required int Damage { get; init; }
    public required int RemainingHealth { get; init; }
    public required string Source { get; init; }
}

/// <summary>
/// Event fired when the player's ship is upgraded
/// </summary>
public sealed record ShipUpgradedEvent : GameEvent
{
    public required string UpgradeId { get; init; }
    public required long Cost { get; init; }
}

/// <summary>
/// Event fired when a new mission is available
/// </summary>
public sealed record MissionAvailableEvent : GameEvent
{
    public required string MissionId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Event fired when a mission is completed
/// </summary>
public sealed record MissionCompletedEvent : GameEvent
{
    public required string MissionId { get; init; }
    public required long Reward { get; init; }
}

/// <summary>
/// Event fired to request a UI refresh
/// </summary>
public sealed record RefreshUIEvent : GameEvent
{
    public string? Region { get; init; }
}

/// <summary>
/// Event fired when a save is requested
/// </summary>
public sealed record SaveRequestedEvent : GameEvent
{
    public required string SaveName { get; init; }
}

/// <summary>
/// Event fired when a load is requested
/// </summary>
public sealed record LoadRequestedEvent : GameEvent
{
    public required string SaveName { get; init; }
}