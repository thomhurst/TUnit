using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that finds an item in a collection matching a predicate
/// and returns it for further assertions
/// </summary>
public class ContainsPredicateAssertion<TCollection, TItem> : AssertionBase<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;
    private TItem? _foundItem;

    public ContainsPredicateAssertion(Func<Task<TCollection>> collectionProvider, Func<TItem, bool> predicate)
        : base(collectionProvider)
    {
        _predicate = predicate;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var collection = await GetActualValueAsync();

        if (collection == null)
        {
            return AssertionResult.Fail("Collection was null");
        }

        _foundItem = collection.FirstOrDefault(_predicate);

        if (_foundItem == null && !collection.Any(_predicate))
        {
            return AssertionResult.Fail("No item in collection matched the predicate");
        }

        return AssertionResult.Passed;
    }

    // Custom GetAwaiter that returns the found item
    public new TaskAwaiter<TItem?> GetAwaiter()
    {
        return GetFoundItemAsync().GetAwaiter();
    }

    private async Task<TItem?> GetFoundItemAsync()
    {
        await ExecuteAsync();
        return _foundItem;
    }
}