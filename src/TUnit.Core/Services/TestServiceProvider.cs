namespace TUnit.Core.Services;

/// <summary>
/// Simple service provider implementation for tests
/// </summary>
public class TestServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    /// <summary>
    /// Registers a singleton service instance
    /// </summary>
    public TestServiceProvider AddSingleton<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
        return this;
    }

    /// <summary>
    /// Registers a singleton service instance by type
    /// </summary>
    public TestServiceProvider AddSingleton(Type serviceType, object service)
    {
        _services[serviceType] = service;
        return this;
    }

    /// <summary>
    /// Registers a service factory
    /// </summary>
    public TestServiceProvider AddTransient<T>(Func<T> factory) where T : class
    {
        _factories[typeof(T)] = () => factory();
        return this;
    }

    /// <summary>
    /// Registers a service factory by type
    /// </summary>
    public TestServiceProvider AddTransient(Type serviceType, Func<object> factory)
    {
        _factories[serviceType] = factory;
        return this;
    }

    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var service))
        {
            return service;
        }

        if (_factories.TryGetValue(serviceType, out var factory))
        {
            return factory();
        }

        // If requesting IServiceProvider, return self
        if (serviceType == typeof(IServiceProvider))
        {
            return this;
        }

        return null;
    }
}

/// <summary>
/// Extension methods for IServiceProvider
/// </summary>
public static class ServiceProviderExtensions
{
    public static T? GetService<T>(this IServiceProvider serviceProvider) where T : class
    {
        return serviceProvider.GetService(typeof(T)) as T;
    }

    public static T GetRequiredService<T>(this IServiceProvider serviceProvider) where T : class
    {
        if (serviceProvider.GetService(typeof(T)) is not T service)
        {
            throw new InvalidOperationException($"Service of type {typeof(T)} is not registered.");
        }
        return service;
    }

    public static object GetRequiredService(this IServiceProvider serviceProvider, Type serviceType)
    {
        var service = serviceProvider.GetService(serviceType);
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
        }
        return service;
    }
}
