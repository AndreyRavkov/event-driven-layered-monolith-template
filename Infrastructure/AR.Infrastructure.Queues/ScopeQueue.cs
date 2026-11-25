using System.Threading.Channels;
using AR.Core.Events;

namespace AR.Infrastructure.Queues;

public interface IScopeQueue
{
    IReadOnlyList<BaseDomainEvent> GetEvents();
    ValueTask AddAsync(BaseDomainEvent @event, CancellationToken ct = default);
    ValueTask AddAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default);
}

internal sealed class ScopeQueue : IScopeQueue
{
    private readonly Channel<BaseDomainEvent> _eventQueue = Channel.CreateUnbounded<BaseDomainEvent>();

    public IReadOnlyList<BaseDomainEvent> GetEvents()
    {
        var events = new List<BaseDomainEvent>();
        while (_eventQueue.Reader.TryRead(out var @event))
        {
            events.Add(@event);
        }
        return events;
    }

    public ValueTask AddAsync(BaseDomainEvent @event, CancellationToken ct = default) =>
        AddAsync([@event], ct);

    public async ValueTask AddAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            await _eventQueue.Writer.WriteAsync(@event, ct);
        }
    }
}