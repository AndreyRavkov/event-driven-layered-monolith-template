using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AR.Domain;

public abstract class EntityMap<T> : IEntityTypeConfiguration<T> where T : Entity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        ConfigureProperties(builder);
        
        builder.Ignore(x => x.DomainEvents);
    }
    
    protected abstract void ConfigureProperties(EntityTypeBuilder<T> builder);
}

public abstract class AuditableEntityMap<T> : EntityMap<T>, IEntityTypeConfiguration<T> where T : AuditableEntity
{
    public new void Configure(EntityTypeBuilder<T> builder)
    {
        ConfigureProperties(builder);
        base.Configure(builder);
        
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired(false);
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").IsRequired(false);
        builder.Property(x => x.CreatedOn).HasColumnName("created_on").IsRequired(false);
        builder.Property(x => x.UpdatedOn).HasColumnName("updated_on").IsRequired(false);
        builder.Property(x => x.ChangeSource).HasColumnName("change_source").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true).IsRequired();
    }
}