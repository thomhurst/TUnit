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
    /// substituting <c>?</c> when the caller expression is unavailable.
    /// </summary>
    internal static StringBuilder Create(string? expression)
        => new StringBuilder("Assert.That(").Append(expression ?? "?").Append(')');
}
