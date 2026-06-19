using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reactive.Disposables;

namespace NeonTrader.Core.Events;

/// <summary>
/// Simple event bus implementation for decoupled communication between game systems.
/// Uses Reactive Extensions for thread-safe event publishing and subscription.
/// </summary>
public interface IEventBus : IDisposable
{
    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    /// <typeparam name="TEvent">Type of event (must inherit from GameEvent)</typeparam>
    /// <param name="event">Event to publish</param>
    void Publish<TEvent>(TEvent @event) where TEvent : GameEvent;

    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler function</param>
    /// <returns>Disposable subscription</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEvent;

    /// <summary>
    /// Subscribe to all events
    /// </summary>
    /// <param name="handler">Handler function</param>
    /// <returns>Disposable subscription</returns>
    IDisposable SubscribeAll(Action<GameEvent> handler);
}

/// <summary>
/// Simple event bus implementation using Reactive Extensions
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, object> _subjects = new();
    private readonly Subject<GameEvent> _allEvents = new();
    private bool _disposed;

    /// <summary>
    /// Publish an event to all subscribers of that event type
    /// </summary>
    public void Publish<TEvent>(TEvent @event) where TEvent : GameEvent
    {
        if (_disposed) return;

        // Publish to type-specific subject
        var subject = GetOrCreateSubject<TEvent>();
        subject.OnNext(@event);

        // Also publish to the all-events subject
        _allEvents.OnNext(@event);
    }

    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEvent
    {
        if (_disposed) return Disposable.Empty;

        var subject = GetOrCreateSubject<TEvent>();
        return subject.Subscribe(handler);
    }

    /// <summary>
    /// Subscribe to all events
    /// </summary>
    public IDisposable SubscribeAll(Action<GameEvent> handler)
    {
        if (_disposed) return Disposable.Empty;

        return _allEvents.Subscribe(handler);
    }

    /// <summary>
    /// Get or create a subject for the given event type
    /// </summary>
    private Subject<TEvent> GetOrCreateSubject<TEvent>() where TEvent : GameEvent
    {
        return (Subject<TEvent>)_subjects.GetOrAdd(typeof(TEvent), _ => new Subject<TEvent>());
    }

    /// <summary>
    /// Dispose all subjects
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var subject in _subjects.Values)
        {
            if (subject is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _subjects.Clear();

        _allEvents.OnCompleted();
        _allEvents.Dispose();
    }
}