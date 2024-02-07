using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.TestAdapter.Extensions;

public static class SerializationExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        ReferenceHandler = ReferenceHandler.Preserve
    };
    
    public static string? SerializeArgumentsSafely(this object?[]? arguments)
    {
        if (arguments is null)
        {
            return null;
        }
        
        return JsonSerializer.Serialize(arguments, Options);
    }
    
    public static object?[]? DeserializeArgumentsSafely(this string? argumentsJson)
    {
        if (argumentsJson == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<object?[]>(argumentsJson, Options);
    }
}