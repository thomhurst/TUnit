using System.Text.Json;

namespace TUnit.Engine.Json;

internal static class CachedJsonOptions
{
    public static readonly JsonSerializerOptions Instance = new JsonSerializerOptions
    {
        Converters = { new TypeJsonConverter(), new MethodInfoJsonConverter() }
    };
}