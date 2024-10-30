using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class DependencyInjectionClassConstructor : IClassConstructor, ITestEndEventReceiver
{
    private readonly IServiceProvider _serviceProvider = CreateServiceProvider();
    private AsyncServiceScope? _scope;
    
    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ClassConstructorMetadata classConstructorMetadata) where T : class
    {
        _scope ??= _serviceProvider.CreateAsyncScope();
        
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope!.Value.ServiceProvider);
    }

    public ValueTask OnTestEnd(TestContext testContext)
    { 
        return _scope!.Value.DisposeAsync();
    }

    private static IServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection()
            .AddTransient<DummyReferenceTypeClass>()
            .BuildServiceProvider();
    }
}