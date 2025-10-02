using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for async Task delegates - provides delegate-specific assertion methods via IDelegateSource marker
/// </summary>
public sealed class AsyncDelegateAssertion : Assertion<object?>, IDelegateSource
{
    internal AsyncDelegateAssertion(Func<Task> function, string? expressionBuilder)
        : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
}
