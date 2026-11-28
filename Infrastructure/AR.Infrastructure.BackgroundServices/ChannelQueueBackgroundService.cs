using AR.Core.EventQueueConsumers;
using AR.Infrastructure.Queues;
using AR.NetExt.Utils.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AR.Infrastructure.BackgroundServices;

internal sealed class ChannelQueueBackgroundService : BackgroundService
{
    private readonly IChannelQueue _channelQueue;
    private readonly ILogger<ChannelQueueBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ChannelQueueBackgroundService(IChannelQueue channelQueue,
                                         ILogger<ChannelQueueBackgroundService> logger,
                                         IServiceScopeFactory serviceScopeFactory)
    {
        _channelQueue = channelQueue;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var eventConsumers = scope.ServiceProvider.GetServices<IQueueConsumer>().AsReadOnlyListExt();
        if (!eventConsumers.Any())
        {
            _logger.LogError($"{nameof(ChannelQueueBackgroundService)} has no {nameof(IQueueConsumer)} implementations.");
            // stop background service
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            while (_channelQueue.Reader.TryRead(out var @event))
            {
                _logger.LogInformation("Start processing event {TypeName}", @event.Type.Name);

                var handler = eventConsumers.FirstOrDefault(x => x.Type == @event.Type);
                if (handler is not null)
                {
                    bool result = true;
                    try
                    {
                        await handler.ExecuteAsync(@event, ct);
                    }
                    catch (Exception exception)
                    {
                        result = false;
                        _logger.LogError(exception,
                                         "Process event '{TypeName}' by '{Name}' failed. Process event finished",
                                         @event.Type.Name,
                                         handler.Type.Name);
                    }

                    if (result)
                    {
                        _logger.LogInformation("Finish processing event '{TypeName}' successfully.", @event.Type.Name);
                    }
                }
                else
                {
                    _logger.LogError("Handler not found for event type '{TypeName}'. Process event finished.", @event.Type.Name);
                }
            }

            if (!ct.IsCancellationRequested)
            {
                // sleep for 5 seconds if there are no messages
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    public override Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation($"{nameof(ChannelQueueBackgroundService)} is starting.");
        return base.StartAsync(ct);
    }

    public override Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation($"{nameof(ChannelQueueBackgroundService)} is stopped.");
        return base.StopAsync(ct);
    }
}
