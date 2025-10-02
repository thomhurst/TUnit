using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertion : ISource
{
    TaskAwaiter GetAwaiter();
    string? GetExpression();
}
