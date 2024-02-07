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
    
    public static object?[]? DeserializeArgumentsSafely(this string? argumentsJson, string[] typeNames)
    {
        if (argumentsJson == null)
        {
            return null;
        }

        var argumentsArray = JsonSerializer.Deserialize<object?[]>(argumentsJson, Options);

        return argumentsArray?.Select((x, i) => FromJsonElement(x, typeNames[i])).ToArray();
    }

    private static object? FromJsonElement(object obj, string typeName)
    {
        if (obj is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.Number:
                    return jsonElement.GetInt64();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return obj;
            }
        }

        return obj;
    }
}