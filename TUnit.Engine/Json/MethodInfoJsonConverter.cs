using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

internal class MethodInfoJsonConverter : JsonConverter<MethodInfo>
{
    public override MethodInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var serializeableMethodInfo = JsonSerializer.Deserialize(ref reader, JsonContext.Default.SerializeableMethodInfo);
        
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            return serializeableMethodInfo
                ?.Type
#pragma warning disable IL2075
                ?.GetMethod(serializeableMethodInfo.MethodName, serializeableMethodInfo.GenericCount,
                    serializeableMethodInfo.MethodParameterTypes);
#pragma warning restore IL2075
        }
        
        throw new NotSupportedException("Dynamic code is not enabled.");
    }

    public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new SerializeableMethodInfo
        {
            Type = value.ReflectedType,
            MethodName = value.Name,
            GenericCount = value.ReflectedType?.GenericTypeArguments.Length ?? 0,
            MethodParameterTypes = value.GetParameters().Select(x => x.ParameterType).ToArray(),
        }, JsonContext.Default.SerializeableMethodInfo);
    }

    internal record SerializeableMethodInfo
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? Type { get; init; }
        public required string MethodName { get; init; }
        public required int GenericCount { get; init; }
        public required Type[] MethodParameterTypes { get; init; }
    }
}