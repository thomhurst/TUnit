---
sidebar_position: 13
---

# Class Constructor Helpers

Some test suites might be more complex than others, and a user may want control over 'newing' up their test classes.
This control is given to you by the `[ClassConstructorAttribute<T>]` - Where `T` is a class that implements `IClassConstructor`.

This interface simply requires you to generate a `T` object - How you do that is up to you!

By giving the freedom of how classes are created, we can tap into things like Dependency Injection.

You can also add [event-subscribing interfaces](event-subscribing.md) to get notified for things like when the test has finished. This functionality can be used to dispose your object afterwards.

Attributes are new'd up per test, so you can store state within them.

Here's an example of that using the Microsoft.Extensions.DependencyInjection library

```csharp
using TUnit.Core;

namespace MyTestProject;

public class DependencyInjectionClassConstructor : IClassConstructor, ITestEndEvent
{
    private static readonly IServiceProvider _serviceProvider = CreateServiceProvider();

    private AsyncServiceScope _scope;

    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ClassConstructorMetadata classConstructorMetadata)
        where T : class
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
            .AddSingleton<SomeClass1>()
            .AddSingleton<SomeClass2>()
            .AddTransient<SomeClass3>()
            .BuildServiceProvider();
    }
}

[ClassConstructor<DependencyInjectionClassConstructor>]
public class MyTestClass(SomeClass1 someClass1, SomeClass2 someClass2, SomeClass3 someClass3)
{
    [Test]
    public async Task Test()
    {
        // ...
    }
}
```
