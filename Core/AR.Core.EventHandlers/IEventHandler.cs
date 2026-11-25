using AR.Core.Events;

namespace AR.Core.EventHandlers;

public interface IEventHandler
{
    Type EventType { get; }
    
    Task PreCommitHandlerAsync(BaseDomainEvent @event, CancellationToken ct = default);
    
    ValueTask PostCommitHandlerAsync(BaseDomainEvent @event, CancellationToken ct = default);
}