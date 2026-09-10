namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper methods for parsing and extracting information from assertion expressions.
/// Consolidates expression parsing logic to ensure consistent behavior across assertion classes.
/// </summary>
internal static class ExpressionHelper
{
    /// <summary>
    /// Extracts the source variable name from an assertion expression string.
    /// </summary>
    /// <param name="expression">The expression string, e.g., "Assert.That(variableName).IsEquivalentTo(...)"</param>
    /// <returns>The variable name, or "value" if it cannot be extracted or is a lambda expression.</returns>
    /// <example>
    /// Input: "Assert.That(myObject).IsEquivalentTo(expected)"
    /// Output: "myObject"
    ///
    /// Input: "Assert.That(async () => GetValue()).IsEquivalentTo(expected)"
    /// Output: "value"
    /// </example>
    public static string ExtractSourceVariable(string expression)
    {
        // Extract variable name from "Assert.That(variableName)" or similar
        var thatIndex = expression.IndexOf(".That(", StringComparison.Ordinal);
        if (thatIndex >= 0)
        {
            var startIndex = thatIndex + 6; // Length of ".That("
            var endIndex = expression.IndexOf(')', startIndex);
            if (endIndex > startIndex)
            {
                var variable = expression.Substring(startIndex, endIndex - startIndex);
                // Handle lambda expressions like "async () => ..." by returning "value"
                if (variable.Contains("=>") || variable.StartsWith("()", StringComparison.Ordinal))
                {
                    return "value";
                }
                return variable;
            }
        }

        return "value";
    }
}
