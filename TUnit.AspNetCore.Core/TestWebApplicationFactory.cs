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
        Action<WebHostBuilderContext, IConfigurationBuilder> configureIsolatedAppConfiguration,
        Action<IWebHostBuilder>? configureWebHostBuilder = null)
    {
        return WithWebHostBuilder(builder =>
        {
            var configurationBuilder = new ConfigurationManager();
            ConfigureStartupConfiguration(configurationBuilder);

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

        return hostBuilder;
    }

    /// <summary>
    /// Registers <see cref="CorrelatedTUnitLoggingExtensions.AddCorrelatedTUnitLogging"/> here
    /// (rather than in <see cref="CreateHostBuilder"/>) so that minimal API hosts — where
    /// <see cref="CreateHostBuilder"/> returns <c>null</c> — also get correlated logging.
    /// Also registers <see cref="PropagatorAlignmentStartupFilter"/> so the SUT's
    /// <see cref="System.Diagnostics.DistributedContextPropagator.Current"/> ends up W3C-aligned
    /// even when user startup code assigns a custom propagator of its own.
    /// Subclasses overriding this method must call <c>base.ConfigureWebHost(builder)</c>.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, PropagatorAlignmentStartupFilter>();
            services.AddCorrelatedTUnitLogging();
        });
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with <see cref="ActivityPropagationHandler"/> and
    /// <see cref="TUnitTestIdHandler"/> automatically prepended to the handler chain.
    /// This ensures all HTTP requests made through clients created by this factory:
    /// <list type="bullet">
    ///   <item><description>Propagate W3C <c>traceparent</c> and <c>baggage</c> headers for Activity-based correlation</description></item>
    ///   <item><description>Propagate the current test's context ID via the <c>X-TUnit-TestId</c> header</description></item>
    /// </list>
    /// </summary>
    public new HttpClient CreateDefaultClient(params DelegatingHandler[] handlers)
    {
        var all = new DelegatingHandler[handlers.Length + 2];
        all[0] = new ActivityPropagationHandler();
        all[1] = new TUnitTestIdHandler();
        Array.Copy(handlers, 0, all, 2, handlers.Length);
        return base.CreateDefaultClient(all);
    }

    /// <inheritdoc cref="CreateDefaultClient(DelegatingHandler[])"/>
    public new HttpClient CreateDefaultClient(Uri baseAddress, params DelegatingHandler[] handlers)
    {
        var client = CreateDefaultClient(handlers);
        client.BaseAddress = baseAddress;
        return client;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with automatic Activity tracing and test context propagation.
    /// Equivalent to calling <see cref="CreateDefaultClient(DelegatingHandler[])"/> with no additional handlers.
    /// </summary>
    public new HttpClient CreateClient()
    {
        var client = CreateDefaultClient();
        ConfigureClient(client);
        return client;
    }

}
