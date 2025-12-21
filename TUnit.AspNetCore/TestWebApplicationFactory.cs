using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore.Extensions;
using TUnit.AspNetCore.Interception;
using TUnit.Core;

namespace TUnit.AspNetCore;

/// <summary>
/// Internal factory wrapper that allows configuration via a delegate.
/// </summary>
public abstract class TestWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    public WebApplicationFactory<TEntryPoint> GetIsolatedFactory(
        TestContext testContext,
        WebApplicationTestOptions options,
        Action<IServiceCollection> configureServices,
        Action<WebHostBuilderContext, IConfigurationBuilder> configureConfiguration,
        Action<IWebHostBuilder>? configureWebHostBuilder = null)
    {
        return WithWebHostBuilder(builder =>
        {
            // Apply user's escape hatch configuration first
            configureWebHostBuilder?.Invoke(builder);

            // Then apply standard configuration
            builder.ConfigureTestServices(configureServices)
                .ConfigureAppConfiguration(configureConfiguration);

            if (options.EnableHttpExchangeCapture)
            {
                builder.ConfigureTestServices(services => services.AddHttpExchangeCapture());
            }
        });
    }
}
