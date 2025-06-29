using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class DependencyInjectionClassConstructor : IClassConstructor, ITestEndEventReceiver
{
    private readonly IServiceProvider _serviceProvider = CreateServiceProvider();
    private AsyncServiceScope? _scope;

    public object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        _scope ??= _serviceProvider.CreateAsyncScope();

        return ActivatorUtilities.GetServiceOrCreateInstance(_scope!.Value.ServiceProvider, type);
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
