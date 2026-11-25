using AR.Domain;
using AR.Infrastructure.Providers;
using AR.NetExt.Utils.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Core.Persistence.Interceptors;

internal sealed class UpdateAuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public UpdateAuditableEntityInterceptor(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                                                                          InterceptionResult<int> result,
                                                                          CancellationToken ct = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void UpdateAuditableEntities(DbContext context)
    {
        var utcNow = DateTime.UtcNow;

        var entityEntries = context.ChangeTracker
                                   .Entries<AuditableEntity>()
                                   .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
                                   .AsReadOnlyListExt();

        if (entityEntries.Any())
        {
            using var scope = _serviceProvider.CreateScope();

            var executionContextProvider = scope.ServiceProvider.GetRequiredService<IExecutionContextProvider>();

            foreach (var entry in entityEntries)
            {
                SetCurrentPropertyValue(entry, nameof(AuditableEntity.UpdatedOn), utcNow);
                SetCurrentPropertyValue(entry, nameof(AuditableEntity.UpdatedBy), executionContextProvider.UserId);
                SetCurrentPropertyValue(entry, nameof(AuditableEntity.ChangeSource), executionContextProvider.ChangedSource);

                if (entry.State == EntityState.Added)
                {
                    SetCurrentPropertyValue(entry, nameof(AuditableEntity.CreatedOn), utcNow);
                    SetCurrentPropertyValue(entry, nameof(AuditableEntity.CreatedBy), executionContextProvider.UserId);
                }
            }
        }
    }

    private void SetCurrentPropertyValue(EntityEntry entry, string propertyName, object value) =>
        entry.Property(propertyName).CurrentValue = value;
}