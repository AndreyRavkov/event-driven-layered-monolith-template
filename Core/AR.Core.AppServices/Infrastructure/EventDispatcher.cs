using AR.Core.EventHandlers;
using AR.Core.Events;

namespace AR.Core.AppServices.Infrastructure;

internal interface IEventDispatcher
{
    Task DispatchPreEventsAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default);
    Task DispatchPostEventsAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default);
}

internal sealed class EventDispatcher : IEventDispatcher
{
    private readonly IEnumerable<IEventHandler> _eventHandlers;

    public EventDispatcher(IEnumerable<IEventHandler> eventHandlers) => _eventHandlers = eventHandlers;

    public async Task DispatchPreEventsAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            var eventHandler = _eventHandlers.FirstOrDefault(x => x.EventType == @event.Type) 
                               ?? throw new NotSupportedException($"Event of type {@event.Type} not supported");
            await eventHandler.PreCommitHandlerAsync(@event, ct);
        }
    }

    public async Task DispatchPostEventsAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            var eventHandler = _eventHandlers.FirstOrDefault(x => x.EventType == @event.Type)
                               ?? throw new NotSupportedException($"Event of type {@event.Type} not supported");
            await eventHandler.PostCommitHandlerAsync(@event, ct);
        }
    }
}