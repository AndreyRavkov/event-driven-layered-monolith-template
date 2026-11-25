using AR.Core.Events;

namespace AR.Core.EventQueueConsumers;

public interface IQueueConsumer
{
    Type Type { get; }
    
    Task ExecuteAsync(BaseDomainEvent message, CancellationToken ct);
}