using System.Text.Json;
using System.Text.Json.Serialization;

namespace AR.Infrastructure.Json;

public static class JsonProvider
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
    };
    
    public static string Serialize<T>(T @object) => JsonSerializer.Serialize(@object, typeof(T), Options);

    public static T? Deserialize<T>(string value, out bool result)
    {
        var @object = default(T);
        try
        {
            @object = JsonSerializer.Deserialize<T>(value, Options);
            result = true;
        }
        catch
        {
            result = false;
        }

        return @object;
    }

    public static T? Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, Options);

    public static T DeserializeOrThrow<T>(string value)
    {
        var result = Deserialize<T>(value);
        return result ?? throw new InvalidOperationException($"Unable to convert '{value}' to type {typeof(T).Name}");
    }
}