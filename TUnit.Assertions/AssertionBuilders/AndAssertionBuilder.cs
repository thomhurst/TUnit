using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual> : AssertionBuilder, IAndAssertionBuilder
{
    internal AndAssertionBuilder(AssertionBuilder assertionBuilder) : base(assertionBuilder.AssertionDataTask, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        if (assertionBuilder.Assertions.Any(a => a is OrAssertCondition))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}