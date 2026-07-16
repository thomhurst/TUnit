using System.Text;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Builds the initial assertion-expression text shared by every source assertion's constructor.
/// Centralises the <c>Assert.That(...)</c> prefix so the format lives in a single place.
/// </summary>
internal static class AssertionExpressionBuilder
{
    /// <summary>
    /// Creates a <see cref="StringBuilder"/> seeded with <c>Assert.That(&lt;expression&gt;)</c>,
    /// substituting <c>?</c> when the caller expression is unavailable. <paramref name="extraCapacity"/>
    /// pads the buffer for callers that append further text after the seed.
    /// </summary>
    internal static StringBuilder Create(string? expression, int extraCapacity = 0)
    {
        var expr = expression ?? "?";
        // Pre-size to "Assert.That(" (12) + expression + ")" (1) so the default 16-char
        // buffer doesn't resize for any expression longer than 3 characters.
        return new StringBuilder(13 + expr.Length + extraCapacity).Append("Assert.That(").Append(expr).Append(')');
    }
}
