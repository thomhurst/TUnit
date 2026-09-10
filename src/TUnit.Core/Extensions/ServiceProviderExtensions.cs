namespace TUnit.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/>.
/// </summary>
internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets a service of the specified type from the service provider.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The service instance, or null if not found</returns>
    public static T? GetService<T>(this IServiceProvider serviceProvider) where T : class
    {
        return serviceProvider.GetService(typeof(T)) as T;
    }
}
