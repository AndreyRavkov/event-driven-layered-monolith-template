namespace AR.Shared.Options;

public class CorsSettings
{
    public const string Name = nameof(CorsSettings);

    public required string Origins { get; set; }
    public required string Methods { get; set; }
    
    public string[] GetOrigins() => Origins.Split(';');
    public string[] GetMethods() => Methods.Split(';');
}