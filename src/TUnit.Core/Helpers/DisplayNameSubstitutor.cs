using System.Text;

namespace TUnit.Core.Helpers;

/// <summary>
/// Utility for substituting parameter placeholders in display names.
/// Supports $paramName and $arg1, $arg2, etc. syntax.
/// </summary>
internal static class DisplayNameSubstitutor
{
    /// <summary>
    /// Substitutes parameter placeholders with actual argument values.
    /// </summary>
    /// <param name="displayName">The display name template with placeholders.</param>
    /// <param name="parameters">The parameter metadata.</param>
    /// <param name="arguments">The actual argument values.</param>
    /// <param name="formatters">Optional custom formatters for argument values.</param>
    /// <returns>The display name with placeholders replaced by formatted argument values.</returns>
    public static string Substitute(
        string displayName,
        ParameterMetadata[] parameters,
        object?[] arguments,
        List<Func<object?, string?>>? formatters = null)
    {
        if (string.IsNullOrEmpty(displayName) || !displayName.Contains('$'))
        {
            return displayName;
        }

        var result = displayName;
        var effectiveFormatters = formatters ?? [];

        // Substitute by position ($arg1, $arg2, etc.) first. Positional placeholders are more
        // specific than single-letter parameter names (e.g. a parameter named "a" would otherwise
        // be matched inside "$arg1"), so resolving them up front avoids that collision.
        for (var i = 0; i < arguments.Length; i++)
        {
            var placeholder = $"$arg{i + 1}";
            if (!result.Contains(placeholder))
            {
                continue;
            }

            var parameterType = i < parameters.Length ? parameters[i].Type : null;
            result = ReplacePlaceholder(result, placeholder, ArgumentFormatter.Format(arguments[i], parameterType, effectiveFormatters));
        }

        // Substitute by parameter name ($paramName)
        for (var i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            var paramName = parameters[i].Name;
            if (string.IsNullOrEmpty(paramName))
            {
                continue;
            }

            var placeholder = $"${paramName}";
            if (!result.Contains(placeholder))
            {
                continue;
            }

            result = ReplacePlaceholder(result, placeholder, ArgumentFormatter.Format(arguments[i], parameters[i].Type, effectiveFormatters));
        }

        return result;
    }

    /// <summary>
    /// Replaces every occurrence of <paramref name="placeholder"/> that is followed by a
    /// non-identifier character (or the end of the string) with <paramref name="value"/>, so that
    /// "$a" is not matched inside "$arg1" or "$abc".
    /// </summary>
    private static string ReplacePlaceholder(string input, string placeholder, string? value)
    {
        var index = input.IndexOf(placeholder, StringComparison.Ordinal);
        if (index < 0)
        {
            return input;
        }

        var builder = new StringBuilder(input.Length);
        var searchStart = 0;

        while (index >= 0)
        {
            var afterIndex = index + placeholder.Length;
            var isBoundary = afterIndex >= input.Length || !IsIdentifierChar(input[afterIndex]);

            builder.Append(input, searchStart, index - searchStart);
            builder.Append(isBoundary ? value ?? string.Empty : placeholder);

            searchStart = afterIndex;
            index = input.IndexOf(placeholder, searchStart, StringComparison.Ordinal);
        }

        builder.Append(input, searchStart, input.Length - searchStart);
        return builder.ToString();
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
