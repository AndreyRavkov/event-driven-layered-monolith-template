using AR.Infrastructure.Json;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureJsonOptionsExtensions
{
    internal static void ConfigureJsonOptionsExt(this IMvcBuilder builder)
    {
        // for ASP.NET MVC
        builder.AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(JsonProvider.Options.Converters.First());
            o.JsonSerializerOptions.DefaultIgnoreCondition = JsonProvider.Options.DefaultIgnoreCondition;
            o.JsonSerializerOptions.PropertyNameCaseInsensitive = JsonProvider.Options.PropertyNameCaseInsensitive;
            o.JsonSerializerOptions.DictionaryKeyPolicy = JsonProvider.Options.DictionaryKeyPolicy;
            o.JsonSerializerOptions.AllowTrailingCommas = JsonProvider.Options.AllowTrailingCommas;
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonProvider.Options.PropertyNamingPolicy;
        });
        
        // for ASP.NET http request & response
        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.Converters.Add(JsonProvider.Options.Converters.First());
            o.SerializerOptions.DefaultIgnoreCondition = JsonProvider.Options.DefaultIgnoreCondition;
            o.SerializerOptions.PropertyNameCaseInsensitive = JsonProvider.Options.PropertyNameCaseInsensitive;
            o.SerializerOptions.DictionaryKeyPolicy = JsonProvider.Options.DictionaryKeyPolicy;
            o.SerializerOptions.AllowTrailingCommas = JsonProvider.Options.AllowTrailingCommas;
            o.SerializerOptions.PropertyNamingPolicy = JsonProvider.Options.PropertyNamingPolicy;
        });
    }
}