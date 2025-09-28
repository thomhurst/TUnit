using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders.Core;

public class ChainedAssertion
{
    public BaseAssertCondition Assertion { get; }
    public ChainType ChainType { get; }

    public ChainedAssertion(BaseAssertCondition assertion, ChainType chainType = ChainType.None)
    {
        Assertion = assertion;
        ChainType = chainType;
    }
}