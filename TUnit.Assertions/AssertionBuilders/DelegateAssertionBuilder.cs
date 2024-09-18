using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder
    : AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>,
        IDelegateSource<object?, DelegateAnd<object?>, DelegateOr<object?>>
{
    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(action.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> ISource<object?, DelegateAnd<object?>, DelegateOr<object?>>.AssertionBuilder => this;
}