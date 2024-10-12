using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    TaskAwaiter GetAwaiter();
    string? GetExpression();
}