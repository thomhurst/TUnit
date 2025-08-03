using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder : AssertionBuilder, IAndAssertionBuilder
{
    internal AndAssertionBuilder(AssertionBuilder assertionBuilder) : base(((ISource) assertionBuilder).AssertionDataTask, (
        (ISource) assertionBuilder).ActualExpression!, (
        (ISource) assertionBuilder).ExpressionBuilder, ((ISource) assertionBuilder).Assertions)
    {
        if (((ISource) assertionBuilder).Assertions.Any(a => a is OrAssertCondition))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}
