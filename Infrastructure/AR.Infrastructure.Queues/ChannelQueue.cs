using System.Threading.Channels;
using AR.Core.Events;

namespace AR.Infrastructure.Queues;

public interface IChannelQueue
{
    ChannelReader<BaseDomainEvent> Reader { get; }
    ValueTask AddAsync(BaseDomainEvent @event, CancellationToken ct = default);
    ValueTask AddAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default);
}

internal sealed class ChannelQueue : IChannelQueue
{
    private readonly Channel<BaseDomainEvent> _eventQueue = Channel.CreateUnbounded<BaseDomainEvent>();
    
    public ChannelReader<BaseDomainEvent> Reader => _eventQueue.Reader;

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