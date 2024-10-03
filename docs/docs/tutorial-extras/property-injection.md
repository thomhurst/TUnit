---
sidebar_position: 13
---

# Property Injection

TUnit makes it easy to initialise some properties on your test class instead of passing them into the constructor.

Your properties must be marked with the `required` keyword and then simply place a data attribute on it.
The required keyword keeps your code clean and free from compiler warnings such as nullability.

Supported attributes for properties are:
- Argument
- MethodDataSource
- ClassDataSource
- DataSourceGeneratorAttribute (though limited to the first item returned)

This can help simplify base classes with common behaviour and avoid having to write boilerplate constructors everywhere.

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
