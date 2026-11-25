using System.Data;
using AR.Core.Events;
using AR.Domain;
using AR.Infrastructure.Queues;
using AR.NetExt.Utils.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AR.Core.Persistence.Context;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted);
    Task SaveChangesAsync();

    IReadOnlyList<BaseDomainEvent> GetDomainEvents();
    ValueTask AddEventAsync(BaseDomainEvent @event, CancellationToken ct = default);
    ValueTask AddEventAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default);
}

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbDomainContext _dbDomainContext;
    private readonly IScopeQueue _scopeQueue;

    public UnitOfWork(DbDomainContext dbDomainContext, IScopeQueue scopeQueue)
    {
        _dbDomainContext = dbDomainContext;
        _scopeQueue = scopeQueue;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted)
    {
        var contextTransaction = _dbDomainContext.Database.CurrentTransaction
                                 ?? await _dbDomainContext.Database.BeginTransactionAsync(isolationLevel);

        return contextTransaction;
    }

    public Task SaveChangesAsync() => _dbDomainContext.SaveChangesAsync();

    public IReadOnlyList<BaseDomainEvent> GetDomainEvents()
    {
        var trackedEntities = _dbDomainContext
                              .ChangeTracker
                              .Entries<Entity>()
                              .Select(entry => entry.Entity)
                              .Where(x => x.DomainEvents.Any())
                              .AsListExt();

        var domainEvents = trackedEntities.SelectMany(x => x.DomainEvents).AsListExt();

        // clear events
        trackedEntities.ForEach(x => x.ClearDomainEvents());

        // add & clear events
        domainEvents.AddRange(_scopeQueue.GetEvents());

        return domainEvents;
    }

    public ValueTask AddEventAsync(BaseDomainEvent @event, CancellationToken ct = default) => AddEventAsync([@event], ct);

    public ValueTask AddEventAsync(IReadOnlyList<BaseDomainEvent> events, CancellationToken ct = default) => _scopeQueue.AddAsync(events, ct);
}