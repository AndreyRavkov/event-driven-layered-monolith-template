using AR.Core.Persistence.Context;

namespace AR.Core.AppServices.Infrastructure;

internal static class Try
{
    internal static async Task CommitAsync(this IUnitOfWork unitOfWork,
                                           Action action,
                                           IEventDispatcher? eventDispatcher = null)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync();

        try
        {
            action();

            var events = unitOfWork.GetDomainEvents();
            if (eventDispatcher is not null && events.Any())
            {
                await eventDispatcher.DispatchPreEventsAsync(events);
            }

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
            
            if (eventDispatcher is not null && events.Any())
            {
                await  eventDispatcher.DispatchPostEventsAsync(events);
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    internal static async Task CommitAsync(this IUnitOfWork unitOfWork,
                                           Func<Task> action,
                                           IEventDispatcher? eventDispatcher = null)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync();

        try
        {
            await action();

            var events = unitOfWork.GetDomainEvents();
            if (eventDispatcher is not null && events.Any())
            {
                await eventDispatcher.DispatchPreEventsAsync(events);
            }

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            if (eventDispatcher is not null && events.Any())
            {
                await  eventDispatcher.DispatchPostEventsAsync(events);
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    internal static async Task<T> CommitAsync<T>(this IUnitOfWork unitOfWork,
                                                 Func<Task<T>> action,
                                                 IEventDispatcher? eventDispatcher = null)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync();

        try
        {
            var result = await action();

            var events = unitOfWork.GetDomainEvents();
            if (eventDispatcher is not null && events.Any())
            {
                await eventDispatcher.DispatchPreEventsAsync(events);
            }

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            if (eventDispatcher is not null && events.Any())
            {
                await  eventDispatcher.DispatchPostEventsAsync(events);
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}