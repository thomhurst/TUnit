using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for Func&lt;T&gt; delegates - provides both value and delegate assertion methods via IValueDelegateSource marker
/// </summary>
public sealed class ValueDelegateAssertion<TActual> : AssertionCore, IValueDelegateSource<TActual>
{
    internal ValueDelegateAssertion(Func<TActual> function, string? expressionBuilder)
        : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
}
