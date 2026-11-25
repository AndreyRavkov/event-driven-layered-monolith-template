using System.Diagnostics.CodeAnalysis;

namespace AR.Domain.Users;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class UserEntity : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
}