using System.Collections.Generic;
using System.Linq;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders.Core;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

internal class AssertionChain : IAssertionChain
{
    private readonly List<ChainedAssertion> _assertions = new();

    public void AddAssertion(BaseAssertCondition assertion, ChainType chainType = ChainType.None)
    {
        _assertions.Add(new ChainedAssertion(assertion, chainType));
    }

    public void AddAndAssertion(BaseAssertCondition assertion)
    {
        AddAssertion(assertion, ChainType.And);
    }

    public void AddOrAssertion(BaseAssertCondition assertion)
    {
        AddAssertion(assertion, ChainType.Or);
    }

    public IEnumerable<ChainedAssertion> GetAssertions()
    {
        return _assertions;
    }

    public IEnumerable<BaseAssertCondition> GetBaseAssertions()
    {
        return _assertions.Select(a => a.Condition);
    }

    public BaseAssertCondition? GetLastAssertion()
    {
        return _assertions.LastOrDefault()?.Condition;
    }
}