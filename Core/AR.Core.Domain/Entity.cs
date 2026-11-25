using AR.Core.Events;

namespace AR.Domain;

public class Entity
{
    public IList<BaseDomainEvent> DomainEvents { get; } = [];

    public void ClearDomainEvents() => DomainEvents.Clear();
}

public class AuditableEntity : Entity
{
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? ChangeSource { get; set; }
    public bool Active { get; set; } = true;
}