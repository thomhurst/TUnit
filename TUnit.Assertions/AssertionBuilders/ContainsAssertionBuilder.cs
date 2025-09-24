using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions.Collections;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// A special assertion builder for Contains with predicate that:
/// 1. Allows chaining enumerable assertions
/// 2. Returns the found item when awaited
/// </summary>
public class ContainsAssertionBuilder<TEnumerable, TInner> : AssertionBuilder<TEnumerable>
    where TEnumerable : IEnumerable<TInner>
{
    private readonly EnumerableContainsExpectedFuncAssertCondition<TEnumerable, TInner> _containsCondition;

    public ContainsAssertionBuilder(
        AssertionBuilder<TEnumerable> innerBuilder,
        EnumerableContainsExpectedFuncAssertCondition<TEnumerable, TInner> containsCondition)
        : base(innerBuilder.Actual, innerBuilder.ActualExpression)
    {
        _containsCondition = containsCondition;

        // Copy assertions from the original builder
        foreach (var assertion in innerBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }

    // Custom awaiter that returns the found item
    public new TaskAwaiter<TInner> GetAwaiter() => GetFoundItemAsync().GetAwaiter();

    private async Task<TInner> GetFoundItemAsync()
    {
        await ProcessAssertionsAsync();
        return _containsCondition.FoundItem!;
    }

    // Enable fluent chaining with proper type
    public new ContainsAssertionBuilder<TEnumerable, TInner> And
    {
        get
        {
            AppendExpression("And");
            return this;
        }
    }
}