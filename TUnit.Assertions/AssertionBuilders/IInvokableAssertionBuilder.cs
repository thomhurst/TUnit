using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    Task ProcessAssertionsAsync();
    IAsyncEnumerable<BaseAssertCondition> GetFailures();
    TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();
    string? GetExpression();
}