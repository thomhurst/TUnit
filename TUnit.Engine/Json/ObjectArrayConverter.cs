using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.Engine.Json;

internal sealed class ObjectArrayConverter : JsonConverter<object?[]?>
{
    public override object?[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }
        
        var valuesAndTypesArrayContainer = JsonSerializer.Deserialize<ValuesAndTypesArrayContainer>(ref reader, options);

        if (valuesAndTypesArrayContainer is null)
        {
            return null;
        }
        
        var values = new List<object?>();
        
        foreach (var valueAndType in valuesAndTypesArrayContainer.ValueAndTypes)
        {
            if (valueAndType.QualifiedTypeName is null)
            {
                values.Add(null);
                continue;
            } 
            
            if (valueAndType.Value is JsonElement jsonElement)
            {
                values.Add(jsonElement.Deserialize(Type.GetType(valueAndType.QualifiedTypeName)!));
            }
        }

        return values.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, object?[]? values, JsonSerializerOptions options)
    {
        if (values is null)
        {
            return;
        }
        
        JsonSerializer.Serialize(writer, new ValuesAndTypesArrayContainer
        {
            ValueAndTypes = values.Select(x => new ValueAndType
            {
                QualifiedTypeName = x?.GetType().AssemblyQualifiedName,
                Value = x
            }).ToArray()
        }, options);
    }
}