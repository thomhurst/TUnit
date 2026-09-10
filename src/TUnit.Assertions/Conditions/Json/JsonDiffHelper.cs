using System.Text.Json;
using System.Text.Json.Nodes;

namespace TUnit.Assertions.Conditions.Json;

/// <summary>
/// Helper class for comparing JSON elements and identifying differences.
/// </summary>
internal static class JsonDiffHelper
{
    /// <summary>
    /// Represents the result of a JSON comparison, including the path where a difference was found
    /// and the expected and actual values at that location.
    /// </summary>
    /// <param name="Path">The JSON path where the difference was found (e.g., "$.person.name").</param>
    /// <param name="Expected">The expected value at this path.</param>
    /// <param name="Actual">The actual value at this path.</param>
    /// <param name="HasDifference">Whether a difference was found. Defaults to true.</param>
    public readonly record struct DiffResult(string Path, string Expected, string Actual, bool HasDifference = true);

    /// <summary>
    /// Finds the first difference between two JSON elements.
    /// </summary>
    /// <param name="actual">The actual JSON element to compare.</param>
    /// <param name="expected">The expected JSON element to compare against.</param>
    /// <returns>A <see cref="DiffResult"/> containing information about the first difference found,
    /// or a result with <see cref="DiffResult.HasDifference"/> set to false if the elements are identical.</returns>
    public static DiffResult FindFirstDifference(JsonElement actual, JsonElement expected)
    {
        return FindDiff(actual, expected, "$");
    }

    private static DiffResult FindDiff(JsonElement actual, JsonElement expected, string path)
    {
        if (actual.ValueKind != expected.ValueKind)
        {
            return new DiffResult(path, expected.ValueKind.ToString(), actual.ValueKind.ToString());
        }

        return actual.ValueKind switch
        {
            JsonValueKind.Object => CompareObjects(actual, expected, path),
            JsonValueKind.Array => CompareArrays(actual, expected, path),
            _ => ComparePrimitives(actual, expected, path)
        };
    }

    private static DiffResult CompareObjects(JsonElement actual, JsonElement expected, string path)
    {
        // Check for missing properties in actual that exist in expected
        foreach (var prop in expected.EnumerateObject())
        {
            var propPath = $"{path}.{prop.Name}";
            if (!actual.TryGetProperty(prop.Name, out var actualProp))
            {
                return new DiffResult(propPath, FormatValue(prop.Value), "(missing)");
            }

            var diff = FindDiff(actualProp, prop.Value, propPath);
            if (diff.HasDifference)
            {
                return diff;
            }
        }

        // Check for extra properties in actual that don't exist in expected
        foreach (var prop in actual.EnumerateObject())
        {
            var propPath = $"{path}.{prop.Name}";
            if (!expected.TryGetProperty(prop.Name, out _))
            {
                return new DiffResult(propPath, "(missing)", FormatValue(prop.Value));
            }
        }

        return new DiffResult(path, "", "", HasDifference: false);
    }

    private static DiffResult CompareArrays(JsonElement actual, JsonElement expected, string path)
    {
        var actualLength = actual.GetArrayLength();
        var expectedLength = expected.GetArrayLength();

        if (actualLength != expectedLength)
        {
            return new DiffResult($"{path}.Length", expectedLength.ToString(), actualLength.ToString());
        }

        var actualEnumerator = actual.EnumerateArray();
        var expectedEnumerator = expected.EnumerateArray();
        var index = 0;

        while (actualEnumerator.MoveNext() && expectedEnumerator.MoveNext())
        {
            var itemPath = $"{path}[{index}]";
            var diff = FindDiff(actualEnumerator.Current, expectedEnumerator.Current, itemPath);
            if (diff.HasDifference)
            {
                return diff;
            }
            index++;
        }

        return new DiffResult(path, "", "", HasDifference: false);
    }

    private static DiffResult ComparePrimitives(JsonElement actual, JsonElement expected, string path)
    {
        var actualText = FormatValue(actual);
        var expectedText = FormatValue(expected);

        if (actualText != expectedText)
        {
            return new DiffResult(path, expectedText, actualText);
        }

        return new DiffResult(path, "", "", HasDifference: false);
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

    /// <summary>
    /// Finds the first difference between two JSON nodes.
    /// </summary>
    /// <param name="actual">The actual JSON node to compare.</param>
    /// <param name="expected">The expected JSON node to compare against.</param>
    /// <returns>A <see cref="DiffResult"/> containing information about the first difference found,
    /// or a result with <see cref="DiffResult.HasDifference"/> set to false if the nodes are identical.</returns>
    public static DiffResult FindFirstDifference(JsonNode? actual, JsonNode? expected)
    {
        return FindNodeDiff(actual, expected, "$");
    }

    private static DiffResult FindNodeDiff(JsonNode? actual, JsonNode? expected, string path)
    {
        // Handle null cases
        if (actual is null && expected is null)
        {
            return new DiffResult(path, "", "", HasDifference: false);
        }
        if (actual is null)
        {
            return new DiffResult(path, FormatNode(expected), "null");
        }
        if (expected is null)
        {
            return new DiffResult(path, "null", FormatNode(actual));
        }

        // Check type mismatch
        if (actual.GetType() != expected.GetType())
        {
            return new DiffResult(path, GetNodeTypeName(expected), GetNodeTypeName(actual));
        }

        return actual switch
        {
            JsonObject actualObj => CompareJsonObjects(actualObj, (JsonObject)expected, path),
            JsonArray actualArr => CompareJsonArrays(actualArr, (JsonArray)expected, path),
            JsonValue actualVal => CompareJsonValues(actualVal, (JsonValue)expected, path),
            _ => new DiffResult(path, "", "", HasDifference: false)
        };
    }

    private static DiffResult CompareJsonObjects(JsonObject actual, JsonObject expected, string path)
    {
        // Check for missing properties in actual that exist in expected
        foreach (var prop in expected)
        {
            var propPath = $"{path}.{prop.Key}";
            if (!actual.TryGetPropertyValue(prop.Key, out var actualProp))
            {
                return new DiffResult(propPath, FormatNode(prop.Value), "(missing)");
            }

            var diff = FindNodeDiff(actualProp, prop.Value, propPath);
            if (diff.HasDifference)
            {
                return diff;
            }
        }

        // Check for extra properties in actual that don't exist in expected
        foreach (var prop in actual)
        {
            var propPath = $"{path}.{prop.Key}";
            if (!expected.ContainsKey(prop.Key))
            {
                return new DiffResult(propPath, "(missing)", FormatNode(prop.Value));
            }
        }

        return new DiffResult(path, "", "", HasDifference: false);
    }

    private static DiffResult CompareJsonArrays(JsonArray actual, JsonArray expected, string path)
    {
        if (actual.Count != expected.Count)
        {
            return new DiffResult($"{path}.Length", expected.Count.ToString(), actual.Count.ToString());
        }

        for (var i = 0; i < actual.Count; i++)
        {
            var itemPath = $"{path}[{i}]";
            var diff = FindNodeDiff(actual[i], expected[i], itemPath);
            if (diff.HasDifference)
            {
                return diff;
            }
        }

        return new DiffResult(path, "", "", HasDifference: false);
    }

    private static DiffResult CompareJsonValues(JsonValue actual, JsonValue expected, string path)
    {
        var actualText = actual.ToJsonString();
        var expectedText = expected.ToJsonString();

        if (actualText != expectedText)
        {
            return new DiffResult(path, expectedText, actualText);
        }

        return new DiffResult(path, "", "", HasDifference: false);
    }

    private static string GetNodeTypeName(JsonNode? node)
    {
        return node switch
        {
            JsonObject => "Object",
            JsonArray => "Array",
            JsonValue => "Value",
            _ => "null"
        };
    }

    private static string FormatNode(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        return node switch
        {
            JsonObject => "{...}",
            JsonArray => "[...]",
            JsonValue val => val.ToJsonString(),
            _ => node.ToJsonString()
        };
    }
}
