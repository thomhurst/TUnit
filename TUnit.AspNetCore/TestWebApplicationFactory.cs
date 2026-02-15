using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore.Extensions;
using TUnit.AspNetCore.Interception;
using TUnit.AspNetCore.Logging;
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
        Action<IServiceCollection> configureIsolatedServices,
        Action<IConfigurationBuilder> configureIsolatedStartupConfiguration,
        Action<WebHostBuilderContext, IConfigurationBuilder> configureIsolatedAppConfiguration,
        Action<IWebHostBuilder>? configureWebHostBuilder = null)
    {
        return WithWebHostBuilder(builder =>
        {
            var configurationBuilder = new ConfigurationManager();
            ConfigureStartupConfiguration(configurationBuilder);
            configureIsolatedStartupConfiguration(configurationBuilder);

            foreach (var keyValuePair in configurationBuilder.AsEnumerable())
            {
                builder.UseSetting(keyValuePair.Key, keyValuePair.Value);
            }

            builder
                .ConfigureAppConfiguration(configureIsolatedAppConfiguration)
                .ConfigureTestServices(services =>
                {
                    configureIsolatedServices(services);
                    services.AddSingleton(testContext);
                    services.AddTUnitLogging(testContext);
                });

            if (options.EnableHttpExchangeCapture)
            {
                builder.ConfigureTestServices(services => services.AddHttpExchangeCapture());
            }

            configureWebHostBuilder?.Invoke(builder);
        });
    }

    protected virtual void ConfigureStartupConfiguration(IConfigurationBuilder configurationBuilder)
    {
    }

    protected override IHostBuilder? CreateHostBuilder()
    {
        var hostBuilder = base.CreateHostBuilder();

        hostBuilder?.ConfigureHostConfiguration(ConfigureStartupConfiguration);

        hostBuilder?.ConfigureServices(services =>
        {
            services.AddCorrelatedTUnitLogging();
        });

        return hostBuilder;
    }

}
