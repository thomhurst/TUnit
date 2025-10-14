# Returning Data via `await`

Sometimes, you may want your assertion to return a value, such as an item found in a collection, so you can use it in further assertions or logic.
TUnit supports this by allowing your assertion to return a value when awaited.

This is useful for scenarios where you want to extract a value from an assertion and use it in subsequent test logic, reducing the need for manual extraction or casting.

For example, `await Assert.That(collection).Contains(item => item.Price < 0.99)` can return the first item that meets the criteria.

## How to Return Values from Assertions

The key to returning values is to make your assertion class extend `Assertion<TReturn>` where `TReturn` is the type you want to return when awaited. The value is returned by transforming the input using `context.Map<TReturn>(...)`.

### Example: Contains with Predicate

Let's say we want to create a `Contains` assertion that returns the found item:

```csharp
public class CollectionContainsPredicateAssertion<TCollection, TItem> : Assertion<TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;

    public CollectionContainsPredicateAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, bool> predicate)
        : base(context.Map<TItem>(collection =>
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), "collection was null");
            }

            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    // Return the found item - this becomes the value when awaited
                    return item;
                }
            }

            // Throw an exception if not found - this will be captured as a failure
            throw new InvalidOperationException("no item matching predicate found in collection");
        }))
    {
        _predicate = predicate;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TItem> metadata)
    {
        // The transformation already happened in the Map function
        // If we got here without exception, the item was found
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(metadata.Exception.Message));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to contain item matching predicate";
}
```

### How It Works

1. **Extend `Assertion<TReturn>`**: Your assertion class extends `Assertion<TItem>` where `TItem` is what you want to return
2. **Use `context.Map<TReturn>(...)`**: In the constructor, call `context.Map<TReturn>(...)` to transform the input value
3. **Return the value**: Inside the Map function, return the value you want to make available when the assertion is awaited
4. **Throw on failure**: If you can't find/produce the value, throw an exception - this will be captured as an assertion failure

### Extension Method

The extension method is straightforward - just return your assertion class:

```csharp
public static class CollectionAssertionExtensions
{
    public static CollectionContainsPredicateAssertion<TCollection, TItem> Contains<TCollection, TItem>(
        this IAssertionSource<TCollection> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsPredicateAssertion<TCollection, TItem>(source.Context, predicate);
    }
}
```

### Usage

You can now use the assertion and get the found item:

```csharp
// Returns the first item with price < 0.99
Product cheapProduct = await Assert.That(products).Contains(p => p.Price < 0.99);

// Use the returned value in further assertions
await Assert.That(cheapProduct.Name).IsNotNull();
await Assert.That(cheapProduct.Category).IsEqualTo("Bargain");
```

## Built-in Examples

TUnit includes several built-in examples of assertions that return values:

- `Contains(predicate)` - Returns the first item matching the predicate
- `WhenParsedInto<T>()` - Returns the parsed value (e.g., `int value = await Assert.That("123").WhenParsedInto<int>()`)
- `IsTypeOf<T>()` - Returns the casted value (e.g., `StringBuilder sb = await Assert.That(obj).IsTypeOf<StringBuilder>()`)
