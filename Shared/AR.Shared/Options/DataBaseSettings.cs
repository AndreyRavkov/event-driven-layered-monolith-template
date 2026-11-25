using System.Diagnostics.CodeAnalysis;

namespace AR.Shared.Options;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class DataBaseSettings
{
    public const string Name = nameof(DataBaseSettings);
    
    public required string MainDb { get; set; }
}