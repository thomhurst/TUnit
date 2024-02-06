using System.Text.Json;

namespace TUnit.TestAdapter.Extensions;

public static class SerializationExtensions
{
    public static string? SerializeArgumentsSafely(this object?[]? arguments)
    {
        if (arguments is null)
        {
            return null;
        }
        
        return JsonSerializer.Serialize(arguments);
    }
    
    public static object?[]? DeserializeArgumentsSafely(this string? argumentsJson)
    {
        if (argumentsJson == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<object?[]>(argumentsJson);
    }
}