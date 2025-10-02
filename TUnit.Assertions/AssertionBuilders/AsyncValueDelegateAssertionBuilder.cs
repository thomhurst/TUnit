using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for async Task&lt;T&gt; delegates - provides both value and delegate assertion methods via IValueDelegateSource marker
/// </summary>
public sealed class AsyncValueDelegateAssertion<TActual> : AssertionCore, IValueDelegateSource<TActual>
{
    internal AsyncValueDelegateAssertion(Func<Task<TActual>> function, string? expressionBuilder)
        : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
}
