namespace AR.Core.Events.Users;

public sealed class UserLoginEvent : BaseDomainEvent
{
    public override Type Type => typeof(UserLoginEvent);

    public int Id { get; set; } = 1001;
}