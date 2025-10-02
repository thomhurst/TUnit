using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for value types - provides value-specific assertion methods via IValueSource marker
/// </summary>
public sealed class ValueAssertion<TActual> : Assertion<TActual>, IValueSource<TActual>
{
    internal ValueAssertion(TActual value, string? expressionBuilder)
        : base(value, expressionBuilder)
    {
    }

    internal ValueAssertion(ISource source) : base(source)
    {
    }
}
