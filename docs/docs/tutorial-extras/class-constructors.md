---
sidebar_position: 13
---

# Class Constructor Helpers

Some test suites might be more complex than others, and a user may want control over 'newing' up their test classes.
This control is given to you by the `[ClassConstructorAttribute<T>]` - Where `T` is a class that implements `IClassConstructor`.

This interface is very generic so you have freedom to construct and dispose as you please.

By giving the freedom of how classes are created, we can tap into things like Dependency Injection.

Here's an example of that using the Microsoft.Extensions.DependencyInjection library

```csharp
using TUnit.Core;

namespace MyTestProject;

public class DependencyInjectionClassConstructor : IClassConstructor
{
    private static readonly IServiceProvider _serviceProvider = CreateServiceProvider();
    
    private static readonly ConditionalWeakTable<object, IServiceScope> Scopes = new();

    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class
    {
        var scope = _serviceProvider.CreateAsyncScope();
        
        var instance = ActivatorUtilities.GetServiceOrCreateInstance<T>(scope.ServiceProvider);
        
        Scopes.Add(instance, scope);
        
        return instance;
    }

    public async Task DisposeAsync<T>(T t)
    {
        if (t is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (t is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        if (t != null && Scopes.TryGetValue(t, out var scope))
        {
            if (scope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync();
            }
            else
            {
                scope.Dispose();
            }
        }
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
