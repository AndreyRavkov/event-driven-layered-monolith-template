using AR.Core.Events;
using AR.Core.Events.Users;
using AR.Shared.Options;

namespace AR.Core.EventQueueConsumers.Users;

internal sealed class UserLoginEventQueueConsumer : IQueueConsumer
{
    private readonly DataBaseSettings _dataBaseSettings;

    public UserLoginEventQueueConsumer(DataBaseSettings dataBaseSettings) =>
        _dataBaseSettings = dataBaseSettings;

    public Type Type => typeof(UserLoginEvent);

    public Task ExecuteAsync(BaseDomainEvent message, CancellationToken ct)
    {
        var @event = (UserLoginEvent)message;
        Console.WriteLine($"{nameof(UserLoginEventQueueConsumer)}.{nameof(ExecuteAsync)}");
        return Task.CompletedTask;
    }
}