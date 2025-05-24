# Dependency Injection

Dependency Injection can be set up by leveraging the power of the Data Source Generators.

TUnit provides you an abstract class to handle most of the logic for you, you need to simply provide the implementation on how to create a DI Scope, and then how to get or create an object when given its type.

So create a new class that inherits from `DependencyInjectionDataSourceAttribute<TScope>` and pass through the Scope type as the generic argument.

Here's an example of that using the Microsoft.Extensions.DependencyInjection library:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MicrosoftDependencyInjectionDataSourceAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private static readonly IServiceProvider ServiceProvider = CreateSharedServiceProvider();

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ServiceProvider.CreateAsyncScope();
    }

    public override object? Create(IServiceScope scope, Type type)
    {
        return scope.ServiceProvider.GetService(type);
    }
    
    private static IServiceProvider CreateSharedServiceProvider()
    {
        return new ServiceCollection()
            .AddSingleton<SomeClass1>()
            .AddSingleton<SomeClass2>()
            .AddTransient<SomeClass3>()
            .BuildServiceProvider();
    }
}

[MicrosoftDependencyInjectionDataSource]
public class MyTestClass(SomeClass1 someClass1, SomeClass2 someClass2, SomeClass3 someClass3)
{
    [Test]
    public async Task Test()
    {
        // ...
    }
}
```

