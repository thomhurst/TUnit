using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    ValueTask<AssertionData> GetAssertionData();
    ValueTask ProcessAssertionsAsync(AssertionData data);
    Stack<BaseAssertCondition> Assertions { get; }
    TaskAwaiter GetAwaiter();
}