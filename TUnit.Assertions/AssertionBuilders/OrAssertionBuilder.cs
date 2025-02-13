using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder : AssertionBuilder, IOrAssertionBuilder
{
    internal OrAssertionBuilder(AssertionBuilder assertionBuilder) : base(((ISource)assertionBuilder).AssertionDataTask, (
        (ISource)assertionBuilder).ActualExpression!, (
        (ISource)assertionBuilder).ExpressionBuilder, ((ISource)assertionBuilder).Assertions)
    {
        if (((ISource)assertionBuilder).Assertions.Any(a => a is AndAssertCondition))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}