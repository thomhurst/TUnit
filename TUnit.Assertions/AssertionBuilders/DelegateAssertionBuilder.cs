using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for Action delegates - provides delegate-specific assertion methods via IDelegateSource marker
/// </summary>
public sealed class DelegateAssertion : Assertion<object?>, IDelegateSource
{
    internal DelegateAssertion(Action action, string? expressionBuilder)
        : base(action.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
}
