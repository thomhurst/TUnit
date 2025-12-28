# JSON Assertions Design

**Issue**: [#4178 - JsonElement assertions](https://github.com/thomhurst/TUnit/issues/4178)
**Date**: 2025-12-28
**Status**: Approved Design

## Summary

Add comprehensive JSON assertions to TUnit supporting both `JsonElement` and `JsonNode` type hierarchies, with runtime-appropriate features via conditional compilation.

## Problem Statement

Currently, `Assert.That(json1).IsEqualTo(json2)` performs string comparison on JSON, which:
- Fails on semantically equivalent JSON with different whitespace
- Provides unhelpful error messages that don't indicate where differences occur

Users need:
- Semantic JSON comparison (ignoring formatting)
- Detailed error messages with paths to differences (e.g., "differs at $.abc.def")

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Target Types | Both JsonElement and JsonNode | Complete coverage for all System.Text.Json users |
| Equality Logic | Built-in `DeepEquals` | Leverage .NET's tested implementation |
| Runtime Support | Per-runtime via `#if` | Provide what's available without polyfills |
| Assertion Categories | Equality, Validity, Property, Type, Array | Comprehensive without external dependencies |
| Path Queries | Deferred | JSONPath not built-in, avoid dependencies |
| Error Messages | Path-to-difference | Addresses core issue request |
| API Entry Point | `Assert.That()` extensions | Consistent with TUnit patterns |
| Package Location | Inside TUnit.Assertions | No extra package for users |
| Implementation | `[GenerateAssertion]` | Simplified code, source generator handles infrastructure |

## Runtime Availability Matrix

| Assertion | .NET 6-7 | .NET 8 | .NET 9+ |
|-----------|----------|--------|---------|
| `IsEqualTo` (JsonNode) | - | Y | Y |
| `IsEqualTo` (JsonElement) | - | - | Y |
| `IsValidJson` (string) | Y | Y | Y |
| `HasProperty` | Y | Y | Y |
| Type checks (`IsObject`, etc.) | Y | Y | Y |
| Array assertions | Y | Y | Y |

## File Structure

```
TUnit.Assertions/Conditions/Json/
├── JsonElementAssertionExtensions.cs
├── JsonNodeAssertionExtensions.cs
├── JsonStringAssertionExtensions.cs
└── JsonDiffHelper.cs
```

## Assertion Inventory

### JsonElement Assertions

```csharp
// Type checking (all runtimes)
IsObject()
IsArray()
IsString()
IsNumber()
IsBoolean()
IsNull()
IsNotNull()

// Property access (all runtimes)
HasProperty(string propertyName)
DoesNotHaveProperty(string propertyName)

// Equality (.NET 9+ only)
#if NET9_0_OR_GREATER
IsEqualTo(JsonElement expected)
IsNotEqualTo(JsonElement expected)
#endif
```

### JsonNode Assertions

```csharp
// Type checking (all runtimes)
IsObject()
IsArray()
IsValue()

// Property access (all runtimes)
HasProperty(string propertyName)
DoesNotHaveProperty(string propertyName)

// Equality (.NET 8+ only)
#if NET8_0_OR_GREATER
IsEqualTo(JsonNode? expected)
IsNotEqualTo(JsonNode? expected)
#endif
```

### JsonArray Assertions

```csharp
// All runtimes
IsEmpty()
IsNotEmpty()
HasCount(int expected)
```

### String (Raw JSON) Assertions

```csharp
// All runtimes
IsValidJson()
IsNotValidJson()
IsValidJsonObject()
IsValidJsonArray()
```

## Implementation Pattern

Using `[GenerateAssertion]` for simplified code:

```csharp
using System.Text.Json;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Json;

file static partial class JsonElementAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a JSON object", InlineMethodBody = true)]
    public static bool IsObject(this JsonElement value)
        => value.ValueKind == JsonValueKind.Object;

    [GenerateAssertion(ExpectationMessage = "to have property \"{propertyName}\"")]
    public static bool HasProperty(this JsonElement value, string propertyName)
        => value.TryGetProperty(propertyName, out _);

#if NET9_0_OR_GREATER
    [GenerateAssertion(ExpectationMessage = "to be equal to {expected}")]
    public static AssertionResult IsEqualTo(this JsonElement value, JsonElement expected)
    {
        if (JsonElement.DeepEquals(value, expected))
            return AssertionResult.Passed;

        var diff = JsonDiffHelper.FindFirstDifference(value, expected);
        return AssertionResult.Failed(
            $"differs at {diff.Path}: expected {diff.Expected} but found {diff.Actual}");
    }
#endif
}
```

## JsonDiffHelper

Provides path-to-difference for error messages:

```csharp
internal static class JsonDiffHelper
{
    public readonly record struct DiffResult(string Path, string Expected, string Actual);

    public static DiffResult FindFirstDifference(JsonElement left, JsonElement right)
    {
        return FindDiff(left, right, "$");
    }

    private static DiffResult FindDiff(JsonElement left, JsonElement right, string path)
    {
        if (left.ValueKind != right.ValueKind)
            return new DiffResult(path, right.ValueKind.ToString(), left.ValueKind.ToString());

        return left.ValueKind switch
        {
            JsonValueKind.Object => CompareObjects(left, right, path),
            JsonValueKind.Array => CompareArrays(left, right, path),
            _ => ComparePrimitives(left, right, path)
        };
    }
    // ... implementation details
}
```

## Error Message Examples

```
Expected JSON to be equal to {"name":"Alice","age":31}
but differs at $.age: expected 31 but found 30

Expected JSON to be equal to {"users":[{"id":1}]}
but differs at $.users[0].id: expected 1 but found 2

Expected JSON to be equal to {"active":true}
but differs at $.active: expected True but found False
```

## Usage Examples

```csharp
// Type checking
await Assert.That(element).IsObject();
await Assert.That(node).IsArray();

// Property access
await Assert.That(element).HasProperty("name");
await Assert.That(element).DoesNotHaveProperty("deleted");

// Equality (.NET 8+/9+)
await Assert.That(element).IsEqualTo(expectedElement);
await Assert.That(node).IsEqualTo(expectedNode);

// String validation
await Assert.That(jsonString).IsValidJson();
await Assert.That(jsonString).IsValidJsonObject();
```

## Testing Strategy

- Unit tests in `TUnit.Assertions.Tests` with `#if` for runtime-specific tests
- Test both pass and fail scenarios for each assertion
- Verify error message format includes correct JSON paths
- Multi-target test project to validate behavior on each runtime

## Future Considerations

- JSONPath support via optional dependency or built-in simple path syntax
- `HasPropertyWithValue(name, value)` combined assertion
- JSON Schema validation
- Partial matching / subset assertions
