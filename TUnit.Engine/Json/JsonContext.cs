using System.Text.Json.Serialization;
using TUnit.Core.Enums;

namespace TUnit.Engine.Json;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    IgnoreReadOnlyProperties = true,
    IgnoreReadOnlyFields = true,
    Converters = [typeof(JsonStringEnumConverter<Status>)],
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(TestSessionJson))]
[JsonSerializable(typeof(TestJson))]
internal partial class JsonContext : JsonSerializerContext;
