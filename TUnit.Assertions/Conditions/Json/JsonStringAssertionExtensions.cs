using System.Text.Json;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Json;

/// <summary>
/// Source-generated assertions for validating JSON strings.
/// </summary>
public static partial class JsonStringAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be valid JSON")]
    public static AssertionResult IsValidJson(this string value)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            return AssertionResult.Passed;
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed($"is not valid JSON: {ex.Message}");
        }
    }

    [GenerateAssertion(ExpectationMessage = "to not be valid JSON")]
    public static AssertionResult IsNotValidJson(this string value)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            return AssertionResult.Failed("is valid JSON");
        }
        catch (JsonException)
        {
            return AssertionResult.Passed;
        }
    }

    [GenerateAssertion(ExpectationMessage = "to be a valid JSON object")]
    public static AssertionResult IsValidJsonObject(this string value)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return AssertionResult.Passed;
            }
            return AssertionResult.Failed($"is a {doc.RootElement.ValueKind}, not an Object");
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed($"is not valid JSON: {ex.Message}");
        }
    }

    [GenerateAssertion(ExpectationMessage = "to be a valid JSON array")]
    public static AssertionResult IsValidJsonArray(this string value)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return AssertionResult.Passed;
            }
            return AssertionResult.Failed($"is a {doc.RootElement.ValueKind}, not an Array");
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed($"is not valid JSON: {ex.Message}");
        }
    }
}
