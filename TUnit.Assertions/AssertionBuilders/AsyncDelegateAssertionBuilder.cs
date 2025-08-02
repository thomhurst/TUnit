using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncDelegateAssertionBuilder
    : AssertionBuilder,
        IDelegateSource
{
    internal AsyncDelegateAssertionBuilder(Func<Task> function, string? expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
}
