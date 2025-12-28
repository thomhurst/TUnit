# JSON Assertions Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add comprehensive JSON assertions for JsonElement, JsonNode, and string types with semantic equality and path-to-difference error messages.

**Architecture:** Use `[GenerateAssertion]` attribute pattern for all assertions. Conditional compilation (`#if NET8_0_OR_GREATER`, `#if NET9_0_OR_GREATER`) enables runtime-specific features. JsonDiffHelper provides detailed error messages showing where JSON structures differ.

**Tech Stack:** System.Text.Json (built-in), TUnit.Assertions source generator, C# 12

---

## Task 1: Create JsonDiffHelper

**Files:**
- Create: `TUnit.Assertions/Conditions/Json/JsonDiffHelper.cs`

**Step 1: Create the Json directory**

```bash
mkdir TUnit.Assertions/Conditions/Json
```

**Step 2: Write the JsonDiffHelper**

Create `TUnit.Assertions/Conditions/Json/JsonDiffHelper.cs`:

```csharp
using System.Text.Json;

namespace TUnit.Assertions.Conditions.Json;

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
        {
            return new DiffResult(path, right.ValueKind.ToString(), left.ValueKind.ToString());
        }

        return left.ValueKind switch
        {
            JsonValueKind.Object => CompareObjects(left, right, path),
            JsonValueKind.Array => CompareArrays(left, right, path),
            _ => ComparePrimitives(left, right, path)
        };
    }

    private static DiffResult CompareObjects(JsonElement left, JsonElement right, string path)
    {
        // Check for missing properties in left that exist in right
        foreach (var prop in right.EnumerateObject())
        {
            var propPath = $"{path}.{prop.Name}";
            if (!left.TryGetProperty(prop.Name, out var leftProp))
            {
                return new DiffResult(propPath, FormatValue(prop.Value), "(missing)");
            }

            var diff = FindDiff(leftProp, prop.Value, propPath);
            if (!string.IsNullOrEmpty(diff.Expected) || !string.IsNullOrEmpty(diff.Actual))
            {
                return diff;
            }
        }

        // Check for extra properties in left that don't exist in right
        foreach (var prop in left.EnumerateObject())
        {
            var propPath = $"{path}.{prop.Name}";
            if (!right.TryGetProperty(prop.Name, out _))
            {
                return new DiffResult(propPath, "(missing)", FormatValue(prop.Value));
            }
        }

        return default;
    }

    private static DiffResult CompareArrays(JsonElement left, JsonElement right, string path)
    {
        var leftLength = left.GetArrayLength();
        var rightLength = right.GetArrayLength();

        if (leftLength != rightLength)
        {
            return new DiffResult($"{path}.Length", rightLength.ToString(), leftLength.ToString());
        }

        var leftEnumerator = left.EnumerateArray();
        var rightEnumerator = right.EnumerateArray();
        var index = 0;

        while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
        {
            var itemPath = $"{path}[{index}]";
            var diff = FindDiff(leftEnumerator.Current, rightEnumerator.Current, itemPath);
            if (!string.IsNullOrEmpty(diff.Expected) || !string.IsNullOrEmpty(diff.Actual))
            {
                return diff;
            }
            index++;
        }

        return default;
    }

    private static DiffResult ComparePrimitives(JsonElement left, JsonElement right, string path)
    {
        var leftText = FormatValue(left);
        var rightText = FormatValue(right);

        if (leftText != rightText)
        {
            return new DiffResult(path, rightText, leftText);
        }

        return default;
    }

    private static string FormatValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Object => "{...}",
            JsonValueKind.Array => "[...]",
            _ => element.GetRawText()
        };
    }
}
```

**Step 3: Verify it compiles**

```bash
cd C:/git/TUnit && dotnet build TUnit.Assertions/TUnit.Assertions.csproj --no-restore -v q
```

Expected: Build succeeded

**Step 4: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonDiffHelper.cs
git commit -m "feat(assertions): add JsonDiffHelper for path-to-difference error messages"
```

---

## Task 2: Create JsonElement Type Assertions

**Files:**
- Create: `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs`
- Test: `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`

**Step 1: Write the failing test**

Create `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`:

```csharp
using System.Text.Json;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class JsonElementAssertionTests
{
    [Test]
    public async Task IsObject_WithObject_Passes()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"test\"}");
        await Assert.That(doc.RootElement).IsObject();
    }

    [Test]
    public async Task IsObject_WithArray_Fails()
    {
        using var doc = JsonDocument.Parse("[1,2,3]");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsObject());
    }

    [Test]
    public async Task IsArray_WithArray_Passes()
    {
        using var doc = JsonDocument.Parse("[1,2,3]");
        await Assert.That(doc.RootElement).IsArray();
    }

    [Test]
    public async Task IsString_WithString_Passes()
    {
        using var doc = JsonDocument.Parse("\"hello\"");
        await Assert.That(doc.RootElement).IsString();
    }

    [Test]
    public async Task IsNumber_WithNumber_Passes()
    {
        using var doc = JsonDocument.Parse("42");
        await Assert.That(doc.RootElement).IsNumber();
    }

    [Test]
    public async Task IsBoolean_WithTrue_Passes()
    {
        using var doc = JsonDocument.Parse("true");
        await Assert.That(doc.RootElement).IsBoolean();
    }

    [Test]
    public async Task IsNull_WithNull_Passes()
    {
        using var doc = JsonDocument.Parse("null");
        await Assert.That(doc.RootElement).IsNull();
    }

    [Test]
    public async Task IsNotNull_WithObject_Passes()
    {
        using var doc = JsonDocument.Parse("{}");
        await Assert.That(doc.RootElement).IsNotNull();
    }
}
```

**Step 2: Run test to verify it fails**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonElementAssertionTests" --no-build
```

Expected: FAIL - IsObject method not found

**Step 3: Write the JsonElement type assertions**

Create `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs`:

```csharp
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
}
```

**Step 4: Build to generate extension methods**

```bash
cd C:/git/TUnit && dotnet build TUnit.Assertions/TUnit.Assertions.csproj -v q
```

Expected: Build succeeded

**Step 5: Run tests to verify they pass**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonElementAssertionTests"
```

Expected: All tests pass

**Step 6: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs TUnit.Assertions.Tests/JsonElementAssertionTests.cs
git commit -m "feat(assertions): add JsonElement type checking assertions"
```

---

## Task 3: Add JsonElement Property Assertions

**Files:**
- Modify: `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs`
- Modify: `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`

**Step 1: Write the failing tests**

Add to `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`:

```csharp
    [Test]
    public async Task HasProperty_WhenPropertyExists_Passes()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\",\"age\":30}");
        await Assert.That(doc.RootElement).HasProperty("name");
    }

    [Test]
    public async Task HasProperty_WhenPropertyMissing_Fails()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).HasProperty("missing"));
    }

    [Test]
    public async Task DoesNotHaveProperty_WhenPropertyMissing_Passes()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.That(doc.RootElement).DoesNotHaveProperty("missing");
    }

    [Test]
    public async Task DoesNotHaveProperty_WhenPropertyExists_Fails()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).DoesNotHaveProperty("name"));
    }
```

**Step 2: Run tests to verify they fail**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonElementAssertionTests.HasProperty" --no-build
```

Expected: FAIL - HasProperty method not found

**Step 3: Add property assertions**

Add to `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs`:

```csharp
    [GenerateAssertion(ExpectationMessage = "to have property \"{propertyName}\"", InlineMethodBody = true)]
    public static bool HasProperty(this JsonElement value, string propertyName)
        => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out _);

    [GenerateAssertion(ExpectationMessage = "to not have property \"{propertyName}\"", InlineMethodBody = true)]
    public static bool DoesNotHaveProperty(this JsonElement value, string propertyName)
        => value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(propertyName, out _);
```

**Step 4: Run tests to verify they pass**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonElementAssertionTests"
```

Expected: All tests pass

**Step 5: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs TUnit.Assertions.Tests/JsonElementAssertionTests.cs
git commit -m "feat(assertions): add JsonElement property assertions"
```

---

## Task 4: Add JsonElement Equality Assertions (.NET 9+)

**Files:**
- Modify: `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs`
- Modify: `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`

**Step 1: Write the failing tests**

Add to `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`:

```csharp
#if NET9_0_OR_GREATER
    [Test]
    public async Task IsEqualTo_WithIdenticalJson_Passes()
    {
        using var doc1 = JsonDocument.Parse("{\"name\":\"Alice\",\"age\":30}");
        using var doc2 = JsonDocument.Parse("{\"name\":\"Alice\",\"age\":30}");
        await Assert.That(doc1.RootElement).IsEqualTo(doc2.RootElement);
    }

    [Test]
    public async Task IsEqualTo_WithDifferentWhitespace_Passes()
    {
        using var doc1 = JsonDocument.Parse("{ \"name\" : \"Alice\" }");
        using var doc2 = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.That(doc1.RootElement).IsEqualTo(doc2.RootElement);
    }

    [Test]
    public async Task IsEqualTo_WithDifferentValues_FailsWithPath()
    {
        using var doc1 = JsonDocument.Parse("{\"name\":\"Alice\",\"age\":30}");
        using var doc2 = JsonDocument.Parse("{\"name\":\"Alice\",\"age\":31}");

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc1.RootElement).IsEqualTo(doc2.RootElement));

        await Assert.That(exception.Message).Contains("$.age");
    }

    [Test]
    public async Task IsNotEqualTo_WithDifferentJson_Passes()
    {
        using var doc1 = JsonDocument.Parse("{\"name\":\"Alice\"}");
        using var doc2 = JsonDocument.Parse("{\"name\":\"Bob\"}");
        await Assert.That(doc1.RootElement).IsNotEqualTo(doc2.RootElement);
    }
#endif
```

**Step 2: Run tests to verify they fail**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonElementAssertionTests.IsEqualTo" --framework net9.0 --no-build
```

Expected: FAIL - IsEqualTo method not found

**Step 3: Add equality assertions**

Add to `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs`:

```csharp
#if NET9_0_OR_GREATER
    [GenerateAssertion(ExpectationMessage = "to be equal to {expected}")]
    public static TUnit.Assertions.Core.AssertionResult IsEqualTo(this JsonElement value, JsonElement expected)
    {
        if (JsonElement.DeepEquals(value, expected))
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }

        var diff = JsonDiffHelper.FindFirstDifference(value, expected);
        return TUnit.Assertions.Core.AssertionResult.Failed(
            $"differs at {diff.Path}: expected {diff.Expected} but found {diff.Actual}");
    }

    [GenerateAssertion(ExpectationMessage = "to not be equal to {expected}", InlineMethodBody = true)]
    public static bool IsNotEqualTo(this JsonElement value, JsonElement expected)
        => !JsonElement.DeepEquals(value, expected);
#endif
```

**Step 4: Run tests to verify they pass**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonElementAssertionTests" --framework net9.0
```

Expected: All tests pass

**Step 5: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs TUnit.Assertions.Tests/JsonElementAssertionTests.cs
git commit -m "feat(assertions): add JsonElement equality assertions for .NET 9+"
```

---

## Task 5: Create JsonNode Assertions

**Files:**
- Create: `TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs`
- Create: `TUnit.Assertions.Tests/JsonNodeAssertionTests.cs`

**Step 1: Write the failing test**

Create `TUnit.Assertions.Tests/JsonNodeAssertionTests.cs`:

```csharp
using System.Text.Json.Nodes;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class JsonNodeAssertionTests
{
    [Test]
    public async Task IsObject_WithJsonObject_Passes()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"test\"}");
        await Assert.That(node).IsJsonObject();
    }

    [Test]
    public async Task IsArray_WithJsonArray_Passes()
    {
        JsonNode? node = JsonNode.Parse("[1,2,3]");
        await Assert.That(node).IsJsonArray();
    }

    [Test]
    public async Task IsValue_WithJsonValue_Passes()
    {
        JsonNode? node = JsonNode.Parse("42");
        await Assert.That(node).IsJsonValue();
    }

    [Test]
    public async Task HasProperty_WhenPropertyExists_Passes()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.That(node).HasJsonProperty("name");
    }

    [Test]
    public async Task DoesNotHaveProperty_WhenPropertyMissing_Passes()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.That(node).DoesNotHaveJsonProperty("missing");
    }

#if NET8_0_OR_GREATER
    [Test]
    public async Task IsEqualTo_WithIdenticalJson_Passes()
    {
        JsonNode? node1 = JsonNode.Parse("{\"name\":\"Alice\",\"age\":30}");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Alice\",\"age\":30}");
        await Assert.That(node1).IsEqualTo(node2);
    }

    [Test]
    public async Task IsNotEqualTo_WithDifferentJson_Passes()
    {
        JsonNode? node1 = JsonNode.Parse("{\"name\":\"Alice\"}");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Bob\"}");
        await Assert.That(node1).IsNotEqualTo(node2);
    }
#endif
}
```

**Step 2: Run tests to verify they fail**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonNodeAssertionTests" --no-build
```

Expected: FAIL - methods not found

**Step 3: Write the JsonNode assertions**

Create `TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs`:

```csharp
using System.Text.Json.Nodes;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions.Json;

/// <summary>
/// Source-generated assertions for JsonNode types.
/// </summary>
file static partial class JsonNodeAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a JsonObject", InlineMethodBody = true)]
    public static bool IsJsonObject(this JsonNode? value)
        => value is JsonObject;

    [GenerateAssertion(ExpectationMessage = "to be a JsonArray", InlineMethodBody = true)]
    public static bool IsJsonArray(this JsonNode? value)
        => value is JsonArray;

    [GenerateAssertion(ExpectationMessage = "to be a JsonValue", InlineMethodBody = true)]
    public static bool IsJsonValue(this JsonNode? value)
        => value is JsonValue;

    [GenerateAssertion(ExpectationMessage = "to have property \"{propertyName}\"", InlineMethodBody = true)]
    public static bool HasJsonProperty(this JsonNode? value, string propertyName)
        => value is JsonObject obj && obj.ContainsKey(propertyName);

    [GenerateAssertion(ExpectationMessage = "to not have property \"{propertyName}\"", InlineMethodBody = true)]
    public static bool DoesNotHaveJsonProperty(this JsonNode? value, string propertyName)
        => value is not JsonObject obj || !obj.ContainsKey(propertyName);

#if NET8_0_OR_GREATER
    [GenerateAssertion(ExpectationMessage = "to be equal to {expected}", InlineMethodBody = true)]
    public static bool IsEqualTo(this JsonNode? value, JsonNode? expected)
        => JsonNode.DeepEquals(value, expected);

    [GenerateAssertion(ExpectationMessage = "to not be equal to {expected}", InlineMethodBody = true)]
    public static bool IsNotEqualTo(this JsonNode? value, JsonNode? expected)
        => !JsonNode.DeepEquals(value, expected);
#endif
}
```

**Step 4: Run tests to verify they pass**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonNodeAssertionTests"
```

Expected: All tests pass

**Step 5: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs TUnit.Assertions.Tests/JsonNodeAssertionTests.cs
git commit -m "feat(assertions): add JsonNode assertions with equality for .NET 8+"
```

---

## Task 6: Create JsonArray Assertions

**Files:**
- Modify: `TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs`
- Modify: `TUnit.Assertions.Tests/JsonNodeAssertionTests.cs`

**Step 1: Write the failing tests**

Add to `TUnit.Assertions.Tests/JsonNodeAssertionTests.cs`:

```csharp
    [Test]
    public async Task IsEmpty_WithEmptyArray_Passes()
    {
        var array = new JsonArray();
        await Assert.That(array).IsJsonArrayEmpty();
    }

    [Test]
    public async Task IsNotEmpty_WithNonEmptyArray_Passes()
    {
        var array = new JsonArray(1, 2, 3);
        await Assert.That(array).IsJsonArrayNotEmpty();
    }

    [Test]
    public async Task HasCount_WithMatchingCount_Passes()
    {
        var array = new JsonArray(1, 2, 3);
        await Assert.That(array).HasJsonArrayCount(3);
    }
```

**Step 2: Run tests to verify they fail**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonNodeAssertionTests.IsEmpty" --no-build
```

Expected: FAIL - methods not found

**Step 3: Add JsonArray assertions**

Add to `TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs`:

```csharp
    [GenerateAssertion(ExpectationMessage = "to be an empty JSON array", InlineMethodBody = true)]
    public static bool IsJsonArrayEmpty(this JsonArray value)
        => value.Count == 0;

    [GenerateAssertion(ExpectationMessage = "to not be an empty JSON array", InlineMethodBody = true)]
    public static bool IsJsonArrayNotEmpty(this JsonArray value)
        => value.Count > 0;

    [GenerateAssertion(ExpectationMessage = "to have {expected} elements")]
    public static TUnit.Assertions.Core.AssertionResult HasJsonArrayCount(this JsonArray value, int expected)
    {
        if (value.Count == expected)
        {
            return TUnit.Assertions.Core.AssertionResult.Passed;
        }
        return TUnit.Assertions.Core.AssertionResult.Failed($"has {value.Count} elements");
    }
```

**Step 4: Run tests to verify they pass**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonNodeAssertionTests"
```

Expected: All tests pass

**Step 5: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs TUnit.Assertions.Tests/JsonNodeAssertionTests.cs
git commit -m "feat(assertions): add JsonArray count and empty assertions"
```

---

## Task 7: Create String JSON Validation Assertions

**Files:**
- Create: `TUnit.Assertions/Conditions/Json/JsonStringAssertionExtensions.cs`
- Create: `TUnit.Assertions.Tests/JsonStringAssertionTests.cs`

**Step 1: Write the failing test**

Create `TUnit.Assertions.Tests/JsonStringAssertionTests.cs`:

```csharp
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class JsonStringAssertionTests
{
    [Test]
    public async Task IsValidJson_WithValidJson_Passes()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.That(json).IsValidJson();
    }

    [Test]
    public async Task IsValidJson_WithInvalidJson_Fails()
    {
        var json = "not valid json";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJson());
    }

    [Test]
    public async Task IsNotValidJson_WithInvalidJson_Passes()
    {
        var json = "not valid json";
        await Assert.That(json).IsNotValidJson();
    }

    [Test]
    public async Task IsValidJsonObject_WithObject_Passes()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.That(json).IsValidJsonObject();
    }

    [Test]
    public async Task IsValidJsonObject_WithArray_Fails()
    {
        var json = "[1,2,3]";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJsonObject());
    }

    [Test]
    public async Task IsValidJsonArray_WithArray_Passes()
    {
        var json = "[1,2,3]";
        await Assert.That(json).IsValidJsonArray();
    }

    [Test]
    public async Task IsValidJsonArray_WithObject_Fails()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJsonArray());
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonStringAssertionTests" --no-build
```

Expected: FAIL - methods not found

**Step 3: Write the string JSON assertions**

Create `TUnit.Assertions/Conditions/Json/JsonStringAssertionExtensions.cs`:

```csharp
using System.Text.Json;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Json;

/// <summary>
/// Source-generated assertions for validating JSON strings.
/// </summary>
file static partial class JsonStringAssertionExtensions
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
```

**Step 4: Run tests to verify they pass**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj --filter "FullyQualifiedName~JsonStringAssertionTests"
```

Expected: All tests pass

**Step 5: Commit**

```bash
git add TUnit.Assertions/Conditions/Json/JsonStringAssertionExtensions.cs TUnit.Assertions.Tests/JsonStringAssertionTests.cs
git commit -m "feat(assertions): add JSON string validation assertions"
```

---

## Task 8: Run Full Test Suite and Update Snapshots

**Files:**
- May modify: `TUnit.PublicAPI/**/*.verified.txt`
- May modify: `TUnit.Core.SourceGenerator.Tests/**/*.verified.txt`

**Step 1: Run full assertion tests**

```bash
cd C:/git/TUnit && dotnet test TUnit.Assertions.Tests/TUnit.Assertions.Tests.csproj
```

Expected: All tests pass

**Step 2: Run public API tests**

```bash
cd C:/git/TUnit && dotnet test TUnit.PublicAPI/TUnit.PublicAPI.csproj
```

Expected: May fail if new public APIs are detected

**Step 3: Accept snapshots if needed**

```bash
cd C:/git/TUnit/TUnit.PublicAPI
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"
```

**Step 4: Run source generator tests**

```bash
cd C:/git/TUnit && dotnet test TUnit.Core.SourceGenerator.Tests/TUnit.Core.SourceGenerator.Tests.csproj
```

Expected: Should pass (JSON assertions don't affect source generator)

**Step 5: Commit snapshots**

```bash
git add TUnit.PublicAPI/*.verified.txt
git commit -m "chore: update public API snapshots for JSON assertions"
```

---

## Task 9: Final Verification

**Step 1: Run complete test suite**

```bash
cd C:/git/TUnit && dotnet test
```

Expected: All tests pass

**Step 2: Test AOT compatibility**

```bash
cd C:/git/TUnit/TUnit.TestProject && dotnet publish -c Release -p:PublishAot=true --use-current-runtime
```

Expected: Publish succeeds

**Step 3: Final commit if any cleanup needed**

```bash
git status
# If clean, no action needed
# If changes, commit them
```

---

## Summary

This plan creates 4 new files:
- `TUnit.Assertions/Conditions/Json/JsonDiffHelper.cs` - Path-to-difference logic
- `TUnit.Assertions/Conditions/Json/JsonElementAssertionExtensions.cs` - JsonElement assertions
- `TUnit.Assertions/Conditions/Json/JsonNodeAssertionExtensions.cs` - JsonNode/JsonArray assertions
- `TUnit.Assertions/Conditions/Json/JsonStringAssertionExtensions.cs` - String validation assertions

And 3 new test files:
- `TUnit.Assertions.Tests/JsonElementAssertionTests.cs`
- `TUnit.Assertions.Tests/JsonNodeAssertionTests.cs`
- `TUnit.Assertions.Tests/JsonStringAssertionTests.cs`

Total assertions: ~20 across all types with runtime-specific availability.
