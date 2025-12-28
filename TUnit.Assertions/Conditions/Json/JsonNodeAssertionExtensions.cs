using System.Text.Json.Nodes;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions.Json;

/// <summary>
/// Source-generated assertions for JsonNode types.
/// </summary>
public static partial class JsonNodeAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a JsonObject")]
    public static TUnit.Assertions.Core.AssertionResult IsJsonObject(this JsonNode? value)
    {
        if (value is JsonObject)
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        return TUnit.Assertions.Core.AssertionResult.Failed($"found {value?.GetType().Name ?? "null"}");
    }

    [GenerateAssertion(ExpectationMessage = "to be a JsonArray")]
    public static TUnit.Assertions.Core.AssertionResult IsJsonArray(this JsonNode? value)
    {
        if (value is JsonArray)
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        return TUnit.Assertions.Core.AssertionResult.Failed($"found {value?.GetType().Name ?? "null"}");
    }

    [GenerateAssertion(ExpectationMessage = "to be a JsonValue")]
    public static TUnit.Assertions.Core.AssertionResult IsJsonValue(this JsonNode? value)
    {
        if (value is JsonValue)
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        return TUnit.Assertions.Core.AssertionResult.Failed($"found {value?.GetType().Name ?? "null"}");
    }

    [GenerateAssertion(ExpectationMessage = "to have property '{propertyName}'")]
    public static TUnit.Assertions.Core.AssertionResult HasJsonProperty(this JsonNode? value, string propertyName)
    {
        if (value is JsonObject obj && obj.ContainsKey(propertyName))
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        if (value is not JsonObject)
        {
            return TUnit.Assertions.Core.AssertionResult.Failed($"found {value?.GetType().Name ?? "null"} instead of JsonObject");
        }
        return TUnit.Assertions.Core.AssertionResult.Failed($"property '{propertyName}' not found");
    }

    [GenerateAssertion(ExpectationMessage = "to not have property '{propertyName}'")]
    public static TUnit.Assertions.Core.AssertionResult DoesNotHaveJsonProperty(this JsonNode? value, string propertyName)
    {
        if (value is not JsonObject obj || !obj.ContainsKey(propertyName))
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        return TUnit.Assertions.Core.AssertionResult.Failed($"property '{propertyName}' was found");
    }

#if NET8_0_OR_GREATER
    [GenerateAssertion(ExpectationMessage = "to be equal to {expected}")]
    public static TUnit.Assertions.Core.AssertionResult IsDeepEqualTo(this JsonNode? value, JsonNode? expected)
    {
        if (JsonNode.DeepEquals(value, expected))
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }

        var diff = JsonDiffHelper.FindFirstDifference(value, expected);
        return TUnit.Assertions.Core.AssertionResult.Failed(
            $"differs at {diff.Path}: expected {diff.Expected} but found {diff.Actual}");
    }

    [GenerateAssertion(ExpectationMessage = "to not be equal to {expected}")]
    public static TUnit.Assertions.Core.AssertionResult IsNotDeepEqualTo(this JsonNode? value, JsonNode? expected)
    {
        if (!JsonNode.DeepEquals(value, expected))
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        return TUnit.Assertions.Core.AssertionResult.Failed("values are equal");
    }
#endif
}
