using AR.Core.Events;
using AR.Core.Events.Users;
using AR.Core.Persistence.Repositories.Users;
using AR.Infrastructure.Queues;
using JetBrains.Annotations;

namespace AR.Core.EventHandlers.Users;

[UsedImplicitly]
internal sealed class UserLoginEventHandler : IEventHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IChannelQueue _channelQueue;

    public UserLoginEventHandler(IUserRepository userRepository, IChannelQueue channelQueue)
    {
        _userRepository = userRepository;
        _channelQueue = channelQueue;
    }

    public Type EventType => typeof(UserLoginEvent);

    public async Task PreCommitHandlerAsync(BaseDomainEvent @event, CancellationToken ct = default)
    {
        var users = await _userRepository.GetAsync();
        var obj = (UserLoginEvent)@event;
        Console.WriteLine($"|In DB there are {users.Count} users. Processing event {obj.Type.Name}");
    }

    public ValueTask PostCommitHandlerAsync(BaseDomainEvent @event, CancellationToken ct = default) =>
        _channelQueue.AddAsync(@event, ct);
}