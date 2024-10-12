using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    TaskAwaiter GetAwaiter();
    IAsyncEnumerable<(BaseAssertCondition Assertion, AssertionResult Result)> GetFailures();
    string? GetExpression();
}