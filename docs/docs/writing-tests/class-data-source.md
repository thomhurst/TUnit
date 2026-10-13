# Injectable Class Data Source

The `ClassDataSource` attribute is used to instantiate and inject in new classes as parameters to your tests and/or test classes.

The attribute takes a generic type argument, which is the type of data you want to inject into your test.

The type created by `ClassDataSource<T>` must have a public parameterless constructor. Constructor injection is supported on the test class receiving the data source, but not on the data source type itself.

For nested dependencies, use property injection on the data source type:

```csharp
[ClassDataSource<ApplicationFixture>(Shared = SharedType.PerTestSession)]
public class ApplicationTests(ApplicationFixture fixture)
{
    [Test]
    public async Task Application_Is_Available()
    {
        await Assert.That(fixture.IsAvailable).IsTrue();
    }
}

public class ApplicationFixture : IAsyncInitializer
{
    [ClassDataSource<DatabaseFixture>(Shared = SharedType.PerTestSession)]
    public required DatabaseFixture Database { get; init; }

    public bool IsAvailable { get; private set; }

    public Task InitializeAsync()
    {
        // Database has already been initialized.
        IsAvailable = Database.IsAvailable;
        return Task.CompletedTask;
    }
}

public class DatabaseFixture : IAsyncInitializer
{
    public bool IsAvailable { get; private set; }

    public Task InitializeAsync()
    {
        IsAvailable = true;
        return Task.CompletedTask;
    }
}
```

See [Nested Property Injection](property-injection.md#nested-property-injection) for dependency chains and lifecycle details.

It also takes an optional `Shared` argument, controlling whether you want to share the instance among other tests. This is useful when it is expensive to create an object and you want to reuse the same instance across many tests.

Avoid mutating the state of shared objects within tests. Because tests run concurrently, the execution order is unpredictable, and shared mutable state leads to flaky tests.

The `SharedType` parameter controls how instances are shared across tests:

- **`SharedType.None`** (default) — new instance for each injection point
- **`SharedType.PerClass`** — one instance per test class
- **`SharedType.PerAssembly`** — one instance shared across all tests in the same assembly
- **`SharedType.PerTestSession`** — single instance for the entire test run, shared across assemblies (best for expensive resources like containers)
- **`SharedType.Keyed`** — share instances based on a key value (use the `Key` parameter)

### Configuring Instances by Key

With `SharedType.Keyed`, each distinct key gets its own instance, and tests that use the same key share it. Implement `IKeyedDataSource` to let one fixture type behave differently for each key. TUnit sets `Key` after the constructor runs and before `InitializeAsync` is called, so initialization can read it:

```csharp
public class DatabaseFixture : IKeyedDataSource, IAsyncInitializer
{
    public string Key { get; set; } = "";

    public bool SeedData { get; private set; }

    public Task InitializeAsync()
    {
        SeedData = Key == "seeded";
        return Task.CompletedTask;
    }
}

[ClassDataSource<DatabaseFixture>(Shared = SharedType.Keyed, Key = "seeded")]
public class ReportTests(DatabaseFixture database)
{
    [Test]
    public async Task Uses_Seeded_Database()
    {
        await Assert.That(database.SeedData).IsTrue();
    }
}
```

Every key creates a separate instance, so each variant pays its own setup cost.

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

        public Task InitializeAsync() => Task.CompletedTask; // start server

        public ValueTask DisposeAsync() => ValueTask.CompletedTask; // stop server
    }
}
```

## When Instances Are Created

TUnit constructs `ClassDataSource<T>` instances while it builds tests, before any test runs. This happens for every `SharedType`. Attributes that affect scheduling, such as `[NotInParallel]`, do not change it. With `SharedType.None`, the constructor can also run more than once for each data row.

`IAsyncInitializer.InitializeAsync` runs later, during execution. It runs right before the first test that receives the instance, after the `[Before(Class)]` hooks, and within that test's `[NotInParallel]` constraint.

Keep constructors free of side effects on shared state, such as caches, static fields, or files. Put that work in `InitializeAsync` so it happens in execution order:

```csharp
public static class ProductCache
{
    private static readonly Dictionary<string, Product> Items = new();

    public static void Register(Product product) => Items[product.Name] = product;

    public static bool Contains(string name) => Items.ContainsKey(name);

    public static void Clear() => Items.Clear();
}

public class Product : IAsyncInitializer
{
    public string Name { get; } = "Widget";

    public Task InitializeAsync()
    {
        // Runs during execution, so a [NotInParallel] test that clears
        // the cache cannot run between this registration and the test.
        ProductCache.Register(this);
        return Task.CompletedTask;
    }
}

public class ProductTests
{
    [Test]
    [NotInParallel]
    public void Clears_Cache()
    {
        ProductCache.Clear();
    }

    [Test]
    [NotInParallel]
    [ClassDataSource<Product>]
    public async Task Product_Is_Cached(Product product)
    {
        await Assert.That(ProductCache.Contains(product.Name)).IsTrue();
    }
}
```

Use `IAsyncInitializer`, not `IAsyncDiscoveryInitializer`, for this work. `IAsyncDiscoveryInitializer` runs while tests are built. See the [test lifecycle](lifecycle.md) for the full order.

# Class Data Source Overloads

If you are using an overload that supports injecting multiple classes at once (e.g. `ClassDataSource<T1, T2, T3>`) then you should specify multiple SharedTypes in an array and keys where applicable.

**Important:** The `Keys` array is **positional** - each index corresponds to the type at that position in the generic parameters. Only types with `SharedType.Keyed` need keys; other positions can be empty strings or omitted.

E.g.

```csharp
public sealed record Value1;
public sealed record Value2;
public sealed record Value3;
public sealed record Value4;
public sealed record Value5;

public class MyType
{
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
    public void Test(Value1 value1, Value2 value2, Value3 value3, Value4 value4, Value5 value5)
    {
        Console.WriteLine($"{value1}, {value2}, {value3}, {value4}, {value5}");
    }
}
```
