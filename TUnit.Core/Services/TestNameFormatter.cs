using System.Collections;
using System.Text;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

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
            bool b => b ? "true" : "false",
            // Use InvariantCulture for numeric types to avoid culture-specific formatting issues
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(System.Globalization.CultureInfo.InvariantCulture),
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
        // Mutate a pooled StringBuilder in place then materialize once, instead of
        // allocating a new string per Replace call. Only the final string allocates.
        var builder = StringBuilderPool.Get();
        try
        {
            return builder
                .Append(template)
                .Replace("{TestIndex}", testIndex.ToString())
                .Replace("{RepeatIndex}", repeatIndex.ToString())
                .Replace("{ClassDataIndex}", classDataIndex.ToString())
                .Replace("{MethodDataIndex}", methodDataIndex.ToString())
                .ToString();
        }
        finally
        {
            StringBuilderPool.Return(builder);
        }
    }

    private string FormatArguments(object?[] args)
    {
        if (args.Length == 0)
        {
            return string.Empty;
        }

        // Build directly into a pooled StringBuilder to avoid the LINQ iterator/closure
        // and the temporary string[] that Select + string.Join would materialize.
        var builder = StringBuilderPool.Get();
        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(FormatArgumentValue(args[i]));
            }

            return builder.ToString();
        }
        finally
        {
            StringBuilderPool.Return(builder);
        }
    }

    private string FormatEnumerable(IEnumerable enumerable)
    {
        // Pool the builder like BuildTestId. Reentrant-safe: a nested enumerable draws a
        // distinct instance from the pool, and each Get is balanced by a Return.
        var sb = StringBuilderPool.Get();
        try
        {
            sb.Append('[');
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
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }
}
