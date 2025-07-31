using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class DependencyInjectionClassConstructor : IClassConstructor
{
    private readonly IServiceProvider _serviceProvider = CreateServiceProvider();

    public Task<object> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        var instance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, type);
        return Task.FromResult(instance);
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddTransient<DummyReferenceTypeClass>();
        return services.BuildServiceProvider();
    }
}
