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

        // Substitute by parameter name ($paramName)
        for (var i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            var paramName = parameters[i].Name;
            if (!string.IsNullOrEmpty(paramName))
            {
                var placeholder = $"${paramName}";
                if (result.Contains(placeholder))
                {
                    var formatted = ArgumentFormatter.Format(arguments[i], effectiveFormatters);
                    result = result.Replace(placeholder, formatted);
                }
            }
        }

        // Substitute by position ($arg1, $arg2, etc.)
        for (var i = 0; i < arguments.Length; i++)
        {
            var placeholder = $"$arg{i + 1}";
            if (result.Contains(placeholder))
            {
                var formatted = ArgumentFormatter.Format(arguments[i], effectiveFormatters);
                result = result.Replace(placeholder, formatted);
            }
        }

        return result;
    }
}
