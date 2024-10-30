using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;
#pragma warning disable

namespace TUnit.TestProject;

public class Class1 : IClass1
{
    public int Value => 1;
}

public interface IClass1
{
    int Value { get; }
}

[ClassConstructor<DependencyInjectionClassConstructor2>]
public class ExampleTestFixture(Class1 c1)
{
    [Test]
    public Task PassTest()
    {
        return Task.CompletedTask;
    }
}

public class DependencyInjectionClassConstructor2 : IClassConstructor, ITestEndEventReceiver
{
    private IServiceProvider? _serviceProvider;
    private AsyncServiceScope _scope;

    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ClassConstructorMetadata classConstructorMetadata) where T : class
    {
        _serviceProvider = CreateServiceProvider();
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
            //.AddScoped<Class1>() //Commenting this line out removes the test from the discovery
            .BuildServiceProvider();
    }
}