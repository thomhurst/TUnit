# Returning Data via `await`

It may make sense for our assertions to return data that is different from the input, based on what the assertion is doing. This can allow more cleanly written tests than have to manually do casting or parsing afterwards.

For example, `await Assert.That(collection).Contains(item => item.Price < 0.99)`

We could make that statement return the first item it finds that meets the criteria.

To do this, in your assert condition, in the `GetResult` method where the logic is evaluated, store the item you'll return if the assertion passes within a property.

For the example above:

```csharp
public class EnumerableContainsExpectedFuncAssertCondition<TActual, TInner>(
    Func<TInner, bool> matcher, string? matcherString)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable<TInner>
{
    private bool _wasFound;
    
    protected override string GetExpectation() => $"to contain an entry matching {matcherString ?? "null"}";
    
    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? typeof(TActual).Name} is null");
        }

        foreach (var inner in actualValue)
        {
            if (matcher(inner))
            {
                _wasFound = true;
                FoundItem = inner;
                break;
            }
        }
        
        return AssertionResult
            .FailIf(_wasFound is false, "there was no match found in the collection");
    }

    public TInner? FoundItem { get; private set; }
}
```

As you can see, if we find an item that matches the predicate, we store it in the `FoundItem` property.

After that, the extension method to invoke your assertion should return a `MappableResultAssertionBuilder<TSource, TAssertCondition, TMappedResult>`.

`TSource` is the original type of the data you're asserting on. For the above, that's an `IEnumerable<>`.

`TAssertCondition` is the Assert Condition that you've created, where you've stored the item in the field.

`TMappedResult` is the type of object that you'd return from the `await Assert.That(...)`

This takes 3 arguments:

The assertion builder, via `valueSource.RegisterAssertion(assertCondition)` - As per how assertions are normally registered.

The assert condition object. It is important that you use the same instance that is passed into the `RegisterAssertion(...)` call.

And a mapper to tell the code how to find the item.

So for the above example, that looks like this. (It looks like a lot of generics, I know!)

```csharp
public static MappableResultAssertionBuilder<IEnumerable<TInner>, EnumerableContainsExpectedFuncAssertCondition<IEnumerable<TInner>, TInner>, TInner> Contains<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, Func<TInner, bool> matcher, [CallerArgumentExpression(nameof(matcher))] string doNotPopulateThisValue = null)
    {
        var enumerableContainsExpectedFuncAssertCondition = new EnumerableContainsExpectedFuncAssertCondition<IEnumerable<TInner>, TInner>(matcher, doNotPopulateThisValue);

        return new MappableResultAssertionBuilder<IEnumerable<TInner>,
            EnumerableContainsExpectedFuncAssertCondition<IEnumerable<TInner>, TInner>, TInner>(
            valueSource.RegisterAssertion(enumerableContainsExpectedFuncAssertCondition, [doNotPopulateThisValue]),
            enumerableContainsExpectedFuncAssertCondition,
            (_, assertCondition) => assertCondition.FoundItem
        );
    }
```
