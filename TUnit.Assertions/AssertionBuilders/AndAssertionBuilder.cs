using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual> : AssertionBuilder<TActual>, IAndAssertionBuilder
{
    internal AndAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        if (assertionBuilder.Assertions.Any(a => a is OrAssertCondition<TActual>))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}