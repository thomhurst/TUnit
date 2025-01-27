using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder<TActual> : AssertionBuilder, IOrAssertionBuilder
{
    internal OrAssertionBuilder(AssertionBuilder assertionBuilder) : base(assertionBuilder.AssertionDataTask, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        if (assertionBuilder.Assertions.Any(a => a is AndAssertCondition))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}