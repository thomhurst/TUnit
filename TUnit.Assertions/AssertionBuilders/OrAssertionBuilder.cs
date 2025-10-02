using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Or assertion - at least one assertion in the chain must pass (logical OR)
/// </summary>
public class OrAssertion : AssertionCore, IOrAssertion
{
    internal OrAssertion(AssertionCore assertionCore) : base(((ISource) assertionCore).AssertionDataTask,
        ((ISource) assertionCore).ActualExpression!,
        ((ISource) assertionCore).ExpressionBuilder, ((ISource) assertionCore).Assertions)
    {
        if (((ISource) assertionCore).Assertions.Any(a => a is AndAssertCondition))
        {
            throw new MixedAndOrAssertionsException();
        }
    }
}
