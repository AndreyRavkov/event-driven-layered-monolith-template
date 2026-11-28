using AR.Domain;
using Microsoft.EntityFrameworkCore;

namespace AR.Core.Persistence.Context;

internal sealed class DbDomainContext : DbContext
{
    public DbDomainContext(DbContextOptions<DbDomainContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.HasDefaultSchema("main");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Entity).Assembly);
    }
}