using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

internal class TypeJsonConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
#pragma warning disable IL2057
            return Type.GetType(reader.GetString()!);
#pragma warning restore IL2057
        }
        
        throw new NotSupportedException("Dynamic code is not enabled.");
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AssemblyQualifiedName ?? value.FullName ?? value.Name);
    }
}