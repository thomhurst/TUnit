using System.Text.Json;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions.Json;

/// <summary>
/// Source-generated assertions for JsonElement type checking.
/// </summary>
file static partial class JsonElementAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a JSON object", InlineMethodBody = true)]
    public static bool IsObject(this JsonElement value)
        => value.ValueKind == JsonValueKind.Object;

    [GenerateAssertion(ExpectationMessage = "to be a JSON array", InlineMethodBody = true)]
    public static bool IsArray(this JsonElement value)
        => value.ValueKind == JsonValueKind.Array;

    [GenerateAssertion(ExpectationMessage = "to be a JSON string", InlineMethodBody = true)]
    public static bool IsString(this JsonElement value)
        => value.ValueKind == JsonValueKind.String;

    [GenerateAssertion(ExpectationMessage = "to be a JSON number", InlineMethodBody = true)]
    public static bool IsNumber(this JsonElement value)
        => value.ValueKind == JsonValueKind.Number;

    [GenerateAssertion(ExpectationMessage = "to be a JSON boolean", InlineMethodBody = true)]
    public static bool IsBoolean(this JsonElement value)
        => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False;

    [GenerateAssertion(ExpectationMessage = "to be JSON null", InlineMethodBody = true)]
    public static bool IsNull(this JsonElement value)
        => value.ValueKind == JsonValueKind.Null;

    [GenerateAssertion(ExpectationMessage = "to not be JSON null", InlineMethodBody = true)]
    public static bool IsNotNull(this JsonElement value)
        => value.ValueKind != JsonValueKind.Null;

    [GenerateAssertion(ExpectationMessage = "to have property '{propertyName}'", InlineMethodBody = true)]
    public static bool HasProperty(this JsonElement value, string propertyName)
        => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out _);

    [GenerateAssertion(ExpectationMessage = "to not have property '{propertyName}'", InlineMethodBody = true)]
    public static bool DoesNotHaveProperty(this JsonElement value, string propertyName)
        => value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out _);
}
