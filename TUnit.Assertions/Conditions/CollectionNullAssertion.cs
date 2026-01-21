using System.Collections;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is not null, preserving collection type information.
/// Extends CollectionAssertionBase to ensure .And and .Or return collection-specific continuations.
/// </summary>
public class CollectionNotNullAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionNotNullAssertion(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;

        if (value != null)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed("value is null"));
    }

    protected override string GetExpectation() => "to not be null";
}
