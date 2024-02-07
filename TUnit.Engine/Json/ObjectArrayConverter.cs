using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

internal sealed class ObjectArrayConverter : JsonConverter<object?[]?>
{
    public override object?[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            return null;
        }
        
        var values = new List<object?>();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var type = reader.GetString()!;
                    reader.Read();
                    var obj = JsonSerializer.Deserialize(ref reader, Type.GetType(type)!);
                    values.Add(obj);
                    break;
                
                case JsonTokenType.Null:
                    values.Add(null);
                    reader.Read();
                    break;
            }
        }
        
        return values.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, object?[]? values, JsonSerializerOptions options)
    {
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }
        
        if (values is null)
        {
            return;
        }

        var serializableValues = new List<object?>();
        
        foreach (var value in values)
        {
            serializableValues.Add(value?.GetType().AssemblyQualifiedName);
            serializableValues.Add(value);
        }
        
        JsonSerializer.Serialize(writer, serializableValues);
    }
}