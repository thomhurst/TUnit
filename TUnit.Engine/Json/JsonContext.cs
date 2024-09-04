using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    IgnoreReadOnlyProperties = true,
    IgnoreReadOnlyFields = true,
    Converters = [ typeof(TypeJsonConverter), typeof(MethodInfoJsonConverter), typeof(JsonStringEnumConverter) ],
    GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(TestSessionJson))]
[JsonSerializable(typeof(TestJson))]
internal partial class JsonContext : JsonSerializerContext;