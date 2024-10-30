---
sidebar_position: 14
---

# Attribute Event Subscribing

Custom attributes (and custom class constructor objects) applied to your tests can optionally implement the following interfaces:
- ITestRegisteredEventReceiver
- ITestStartEventReceiver
- ITestEndEventReceiver
- ILastTestInClassEventReceiver
- ILastTestInAssemblyEventReceiver
- ILastTestInTestSessionEventReceiver

This can be useful especially when generating data that you need to track and maybe dispose later. By hooking into these events, we can do things like track and dispose our objects when we need.

Each attribute will be new'd up for each test, so you are able to store state within the fields of your attribute class.

The `[ClassDataSource<T>]` uses these events to do the following:
- On Test Register > Increment Counts for Various Types (Global, Keyed, etc.)
- On Test Start > Initialise any objects if they have the `IAsyncInitializer` interface
- On Test End > If the object isn't shared, dispose it. Otherwise, decrement the count for the type.
- On Last Test for Class > Dispose the object being used to inject into that specific class
- On Last Test for Assembly > Dispose the object being used to inject into that specific assembly

Here's a simple Dependency Injection Class Constructor class subscribing to the TestEnd event in order to dispose the service scope when the test is finished:

```csharp
public class DependencyInjectionClassConstructor : IClassConstructor, ITestEndEventReceiver
{
    private readonly IServiceProvider _serviceProvider = CreateServiceProvider();
    private AsyncServiceScope _scope;
    
    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class
    {
        _scope = _serviceProvider.CreateAsyncScope();
        
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }

    public ValueTask OnTestEnd(TestContext testContext)
    { 
        return _scope.DisposeAsync();
    }

    private static IServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection()
            .AddTransient<Class1>()
            .AddTransient<Class2>()
            .AddTransient<Class3>()
            ...
            .BuildServiceProvider();
    }
}
```