using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class DependencyInjectionClassConstructor : IClassConstructor, ITestEndEventReceiver
{
    private readonly IServiceProvider _serviceProvider = CreateServiceProvider();
    private AsyncServiceScope? _scope;

    public Task<object> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        _scope ??= _serviceProvider.CreateAsyncScope();

        var instance = ActivatorUtilities.GetServiceOrCreateInstance(_scope!.Value.ServiceProvider, type);
        return Task.FromResult(instance);
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

    public int Order => 0;
}
