using System.Collections;
using System.Text;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of test name formatting service.
/// </summary>
public class TestNameFormatter : ITestNameFormatter
{
    /// <inheritdoc />
    public string FormatTestName(
        string template, 
        object?[]? classArgs = null, 
        object?[]? methodArgs = null,
        IDictionary<string, object?>? propertyValues = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var result = template;

        // Replace argument placeholders
        if (classArgs?.Length > 0)
        {
            result = result.Replace("$classArguments", FormatArguments(classArgs));
        }

        if (methodArgs?.Length > 0)
        {
            result = result.Replace("$methodArguments", FormatArguments(methodArgs));
        }

        if (propertyValues?.Count > 0)
        {
            foreach (var kvp in propertyValues)
            {
                result = result.Replace($"${kvp.Key}", FormatArgumentValue(kvp.Value));
            }
        }

        return result;
    }

    /// <inheritdoc />
    public string FormatArgumentValue(object? value)
    {
        return value switch
        {
            null => "null",
            string str => $"\"{str}\"",
            char ch => $"'{ch}'",
            bool b => b.ToString().ToLowerInvariant(),
            IEnumerable enumerable when value.GetType() != typeof(string) => FormatEnumerable(enumerable),
            _ => value.ToString() ?? "null"
        };
    }

    /// <inheritdoc />
    public string BuildTestId(
        string template, 
        int testIndex, 
        int repeatIndex = 0, 
        int classDataIndex = 0, 
        int methodDataIndex = 0)
    {
        return template
            .Replace("{TestIndex}", testIndex.ToString())
            .Replace("{RepeatIndex}", repeatIndex.ToString())
            .Replace("{ClassDataIndex}", classDataIndex.ToString())
            .Replace("{MethodDataIndex}", methodDataIndex.ToString());
    }

    private string FormatArguments(object?[] args)
    {
        if (args.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", args.Select(FormatArgumentValue));
    }

    private string FormatEnumerable(IEnumerable enumerable)
    {
        var sb = new StringBuilder("[");
        var first = true;

        foreach (var item in enumerable)
        {
            if (!first)
            {
                sb.Append(", ");
            }
            first = false;
            sb.Append(FormatArgumentValue(item));
        }

        sb.Append(']');
        return sb.ToString();
    }
}