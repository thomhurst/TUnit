using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TUnit.Engine.Json;

internal static class CachedJsonOptions
{
    public static readonly JsonSerializerOptions Instance = CreateDefaultOptions();
    
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        return new()
        {
            TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
                ? new DefaultJsonTypeInfoResolver()
                : SerializationModeOptionsContext.Default
        };
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    IgnoreReadOnlyProperties = true,
    IgnoreReadOnlyFields = true,
    Converters = [ typeof(TypeJsonConverter), typeof(MethodInfoJsonConverter), typeof(JsonStringEnumConverter) ],
    GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(JsonOutput))]
internal partial class SerializationModeOptionsContext : JsonSerializerContext
{
}