# Injectable Class Data Source

The `ClassDataSource` attribute is used to instantiate and inject in new classes as parameters to your tests and/or test classes.

The attribute takes a generic type argument, which is the type of data you want to inject into your test.

It also takes an optional `Shared` argument, controlling whether you want to share the instance among other tests. This is useful when it is expensive to create an object and you want to reuse the same instance across many tests.

Avoid mutating the state of shared objects within tests. Because tests run concurrently, the execution order is unpredictable, and shared mutable state leads to flaky tests.

The `SharedType` parameter controls how instances are shared across tests. See [Property Injection -- Sharing Strategies](property-injection.md#sharing-strategies) for full details on the available options (`None`, `PerClass`, `PerAssembly`, `PerTestSession`, `Keyed`).

## Initialization and TearDown
If you need to do some initialization or teardown for when this object is created/disposed, simply implement the `IAsyncInitializer` and/or `IAsyncDisposable` interfaces

# Example

```csharp
public class MyTestClass
{
    [Test]
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public void MyTest(WebApplicationFactory webApplicationFactory)
    {
    }

    public record WebApplicationFactory : IAsyncInitializer, IAsyncDisposable
    {
        // Some properties/methods/whatever!

        public async Task InitializeAsync()
        {
            await StartServer();
        }

        public async ValueTask DisposeAsync()
        {
            await StopServer();
        }
    }
}
```

# Class Data Source Overloads

If you are using an overload that supports injecting multiple classes at once (e.g. `ClassDataSource<T1, T2, T3>`) then you should specify multiple SharedTypes in an array and keys where applicable.

**Important:** The `Keys` array is **positional** - each index corresponds to the type at that position in the generic parameters. Only types with `SharedType.Keyed` need keys; other positions can be empty strings or omitted.

E.g.

```csharp
[Test]
    [ClassDataSource<Value1, Value2, Value3, Value4, Value5>
        (
        Shared = [SharedType.PerTestSession, SharedType.Keyed, SharedType.PerClass, SharedType.Keyed, SharedType.None],
        Keys = ["", "Value2Key", "", "Value4Key", ""]
        // Index 0: Value1 (PerTestSession) - empty string (no key needed)
        // Index 1: Value2 (Keyed) - "Value2Key"
        // Index 2: Value3 (PerClass) - empty string (no key needed)
        // Index 3: Value4 (Keyed) - "Value4Key"
        // Index 4: Value5 (None) - empty string (no key needed)
        )]
    public class MyType(Value1 value1, Value2 value2, Value3 value3, Value4 value4, Value5 value5)
    {

    }
```