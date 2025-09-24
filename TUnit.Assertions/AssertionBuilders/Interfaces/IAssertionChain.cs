using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders.Interfaces;

/// <summary>
/// Internal interface for managing the chain of assertions with logical connectors
/// </summary>
internal interface IAssertionChain
{
    void AddAssertion(BaseAssertCondition assertion, ChainType chainType = ChainType.None);
    void AddAndAssertion(BaseAssertCondition assertion);
    void AddOrAssertion(BaseAssertCondition assertion);
    IEnumerable<ChainedAssertion> GetAssertions();
    IEnumerable<BaseAssertCondition> GetBaseAssertions();
    BaseAssertCondition? GetLastAssertion();
}