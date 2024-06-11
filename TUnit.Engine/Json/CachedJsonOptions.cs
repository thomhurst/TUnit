using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

internal static class CachedJsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new TypeJsonConverter(), new MethodInfoJsonConverter(), new JsonStringEnumConverter() },
        WriteIndented = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
    };
}