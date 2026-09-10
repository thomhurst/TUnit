using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TUnit.Core;
using TUnit.Logging.Microsoft;

namespace TUnit.AspNetCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to simplify service replacement in tests.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces all registrations of <typeparamref name="TService"/> with the specified instance.
    /// </summary>
    /// <typeparam name="TService">The service type to replace.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="instance">The instance to use for the service.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.ReplaceService&lt;IEmailService&gt;(new FakeEmailService());
    /// </code>
    /// </example>
    public static IServiceCollection ReplaceService<TService>(
        this IServiceCollection services,
        TService instance)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(instance);
        return services;
    }

    /// <summary>
    /// Replaces all registrations of <typeparamref name="TService"/> with the specified factory.
    /// </summary>
    /// <typeparam name="TService">The service type to replace.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory to create the service instance.</param>
    /// <param name="lifetime">The lifetime of the service. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.ReplaceService&lt;IEmailService&gt;(
    ///     sp => new FakeEmailService(sp.GetRequiredService&lt;ILogger&gt;()));
    /// </code>
    /// </example>
    public static IServiceCollection ReplaceService<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> factory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
    {
        services.RemoveAll<TService>();

        var descriptor = new ServiceDescriptor(typeof(TService), factory, lifetime);
        services.Add(descriptor);

        return services;
    }

    /// <summary>
    /// Replaces all registrations of <typeparamref name="TService"/> with <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The service type to replace.</typeparam>
    /// <typeparam name="TImplementation">The implementation type to use.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The lifetime of the service. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.ReplaceService&lt;IEmailService, FakeEmailService&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection ReplaceService<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService
    {
        services.RemoveAll<TService>();

        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime);
        services.Add(descriptor);

        return services;
    }

    /// <summary>
    /// Removes all registrations of <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service type to remove.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RemoveService<TService>(this IServiceCollection services)
        where TService : class
    {
        services.RemoveAll<TService>();
        return services;
    }

    /// <summary>
    /// Adds TUnit logging to the service collection with a specific test context.
    /// Use this overload when you need to capture logs for a specific test context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="context">A function that returns the test context.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTUnitLogging(
        this IServiceCollection services,
        TestContext context,
        LogLevel minLogLevel = LogLevel.Information)
    {
        services.AddLogging(builder => builder.AddProvider(new TUnitLoggerProvider(context, minLogLevel)));
        return services;
    }
}
