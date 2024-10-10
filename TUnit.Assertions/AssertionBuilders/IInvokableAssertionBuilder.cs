using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    IAsyncEnumerable<(BaseAssertCondition Assertion, AssertionResult Result)> GetFailures();
    string? GetExpression();
}