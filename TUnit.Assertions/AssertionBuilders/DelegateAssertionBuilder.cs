using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder
    : AssertionBuilder,
        IDelegateSource
{
    internal DelegateAssertionBuilder(Action action, string? expressionBuilder) : base(LazyAssertionData.Create(action, expressionBuilder), expressionBuilder)
    {
    }
}
