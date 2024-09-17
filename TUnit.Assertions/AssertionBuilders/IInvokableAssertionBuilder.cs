using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    IAsyncEnumerable<BaseAssertCondition> GetFailures();
    string? GetExpression();
}