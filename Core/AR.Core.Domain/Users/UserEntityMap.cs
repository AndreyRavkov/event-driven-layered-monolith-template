using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AR.Domain.Users;

public class UserEntityMap : AuditableEntityMap<UserEntity>
{
    protected override void ConfigureProperties(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired();
    }
}