using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertionBuilders;

public class AssertionChain
{
    private readonly List<ChainedAssertion> _assertions = [];
    
    public void AddAssertion(BaseAssertCondition assertion, ChainType chainType = ChainType.None)
    {
        _assertions.Add(new ChainedAssertion(assertion, chainType));
    }

    public void AddAndAssertion(BaseAssertCondition assertion)
    {
        if (_assertions.Count > 0)
        {
            var previous = _assertions[^1];
            _assertions[^1] = new ChainedAssertion(
                new AndAssertCondition(previous.Condition, assertion), 
                previous.ChainType
            );
        }
        else
        {
            AddAssertion(assertion);
        }
    }

    public void AddOrAssertion(BaseAssertCondition assertion)
    {
        if (_assertions.Count > 0)
        {
            var previous = _assertions[^1];
            _assertions[^1] = new ChainedAssertion(
                new OrAssertCondition(previous.Condition, assertion),
                previous.ChainType
            );
        }
        else
        {
            AddAssertion(assertion);
        }
    }

    public IEnumerable<BaseAssertCondition> GetAssertions()
    {
        return _assertions.Select(a => a.Condition);
    }

    public BaseAssertCondition? GetLastAssertion()
    {
        return _assertions.Count > 0 ? _assertions[^1].Condition : null;
    }

    private record ChainedAssertion(BaseAssertCondition Condition, ChainType ChainType);
}