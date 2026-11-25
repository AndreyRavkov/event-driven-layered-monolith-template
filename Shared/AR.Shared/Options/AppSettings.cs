using System.Diagnostics.CodeAnalysis;

namespace AR.Shared.Options;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class AppSettings
{
    public const string Name = nameof(AppSettings);
    
    public long MaxRequestBodySizeBytes { get; set; }
}
