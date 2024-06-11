using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

public class TypeJsonConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return Type.GetType(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AssemblyQualifiedName ?? value.FullName ?? value.Name);
    }
}