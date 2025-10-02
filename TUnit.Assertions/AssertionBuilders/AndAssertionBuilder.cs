using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// And assertion - ensures all assertions in the chain must pass (logical AND)
/// </summary>
public class AndAssertion : AssertionCore, IAndAssertion
{
    internal AndAssertion(AssertionCore assertionCore) : base(((ISource) assertionCore).AssertionDataTask,
        ((ISource) assertionCore).ActualExpression!,
        ((ISource) assertionCore).ExpressionBuilder, ((ISource) assertionCore).Assertions)
    {
        if (((ISource) assertionCore).Assertions.Any(a => a is OrAssertCondition))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}
