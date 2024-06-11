using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

internal class MethodInfoJsonConverter : JsonConverter<MethodInfo>
{
    public override MethodInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var serializeableMethodInfo = JsonSerializer.Deserialize<SerializeableMethodInfo>(ref reader, options);

        return serializeableMethodInfo
            ?.Type
            ?.GetMethod(serializeableMethodInfo.MethodName, serializeableMethodInfo.GenericCount, serializeableMethodInfo.MethodParameterTypes);
    }

    public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new SerializeableMethodInfo
        {
            Type = value.ReflectedType,
            MethodName = value.Name,
            GenericCount = value.ReflectedType?.GenericTypeArguments.Length ?? 0,
            MethodParameterTypes = value.GetParameters().Select(x => x.ParameterType).ToArray(),
        }, options);
    }

    internal record SerializeableMethodInfo
    {
        public Type? Type { get; init; }
        public required string MethodName { get; init; }
        public required int GenericCount { get; init; }
        public required Type[] MethodParameterTypes { get; init; }
    }
}