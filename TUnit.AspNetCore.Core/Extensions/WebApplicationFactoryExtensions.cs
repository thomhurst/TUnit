using Microsoft.AspNetCore.Mvc.Testing;

namespace TUnit.AspNetCore;

/// <summary>
/// Extension methods for <see cref="WebApplicationFactory{TEntryPoint}"/> to simplify creating
/// HTTP clients that propagate TUnit test context.
/// </summary>
public static class WebApplicationFactoryExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> with a <see cref="TUnitTestIdHandler"/> that automatically
    /// propagates the current test context ID to the server via HTTP headers.
    /// </summary>
    /// <typeparam name="TEntryPoint">The entry point class of the web application.</typeparam>
    /// <param name="factory">The web application factory.</param>
    /// <returns>An <see cref="HttpClient"/> configured with test context propagation.</returns>
    [Obsolete("TestWebApplicationFactory now injects ActivityPropagationHandler and TUnitTestIdHandler automatically. Use CreateClient() or CreateDefaultClient() instead.")]
    public static HttpClient CreateClientWithTestContext<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
    {
        return factory.CreateDefaultClient(new ActivityPropagationHandler(), new TUnitTestIdHandler());
    }
}
