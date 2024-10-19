using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder<TActual> : AssertionBuilder<TActual>, IOrAssertionBuilder
{
    internal OrAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        if (assertionBuilder.Assertions.Any(a => a is AndAssertCondition<TActual>))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}