using System.Diagnostics.CodeAnalysis;

namespace AR.Shared.Options;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class JwtSettings
{
    public const string Name = nameof(JwtSettings);
    
    public required string Issuer { get; set; }
    public required string Secret { get; set; }
    public int ExpireMinutes { get; set; }
}