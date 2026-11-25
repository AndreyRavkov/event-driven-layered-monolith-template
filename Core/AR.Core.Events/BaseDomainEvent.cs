namespace AR.Core.Events;

public abstract class BaseDomainEvent
{
    public abstract Type Type { get; }
    public DateTime OccuredOn { get; init; } = DateTime.UtcNow;
}