---
sidebar_position: 5
---

# Injectable Class Data Source

The `ClassDataSource` attribute is used to instantiate and inject in new classes as parameters to your tests and/or test classes.

The attribute takes a generic type argument, which is the type of data you want to inject into your test.

It also takes an optional `Shared` argument, controlling whether you want to share the instance among other tests.
This could be useful for times where it's very intensive to spin up lots of objects, and you instead want to share that same instance across many tests.

Ideally don't manipulate the state of this object within your tests if your object is shared. Because of concurrency, it's impossible to know which test will run in which order, and so your tests could become flaky and undeterministic.

Options are:

### Shared = SharedType.None
The instance is not shared ever. A new one will be created for you.

### Shared = SharedType.Globally
The instance is shared globally for every test that also uses this setting, meaning it'll always be the same instance.

### Shared = SharedType.ForClass
The instance is shared for every test in the same class as itself, that also has this setting.

### Shared = SharedType.Keyed
When using this, you must also populate the `Key` argument on the attribute.

The instance is shared for every test that also has this setting, and also uses the same key.

## Initialization and TearDown
If you need to do some initialization or teardown for when this object is created/disposed, simply implement the `IAsyncInitializer` and/or `IAsyncDisposable` interfaces

```csharp
public class MyTestClass
{
    [Test]
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.Globally)]
    public void MyTest(WebApplicationFactory webApplicationFactory)
    {
    }

    public record WebApplicationFactory : IAsyncInitializer, IAsyncDisposable
    {
        // Some properties/methods/whatever!

        public Task InitializeAsync() 
        {
            await StopServer();
        }

        public ValueTask DisposeAsync() 
        {
            await StartServer();
        }
    }
}
```