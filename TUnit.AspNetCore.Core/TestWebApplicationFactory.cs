using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using OpenTelemetry.Trace;
using TUnit.AspNetCore.Extensions;
using TUnit.AspNetCore.Http;
using TUnit.AspNetCore.Interception;
using TUnit.AspNetCore.Logging;
using TUnit.Core;
using TUnit.OpenTelemetry;

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

                    if (options.AutoPropagateHttpClientFactory)
                    {
                        services.TryAddEnumerable(
                            ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, TUnitHttpClientFilter>());
                    }

                    if (options.AutoConfigureOpenTelemetry)
                    {
                        AddTUnitOpenTelemetry(services);
                    }
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
    /// Adds TUnit's default OpenTelemetry tracing configuration to <paramref name="services"/>:
    /// the <see cref="TUnitTestCorrelationProcessor"/> and ASP.NET Core + HttpClient
    /// instrumentation. Safe to call even if the SUT already registers these — OpenTelemetry
    /// de-duplicates them. Also safe when combined with the <c>TUnit.OpenTelemetry</c>
    /// zero-config package: the SUT and test-runner <c>TracerProvider</c>s each carry their
    /// own processor, and the processor's idempotent tagging guard prevents duplicate
    /// <c>tunit.test.id</c> tags across its <c>OnStart</c>/<c>OnEnd</c> hooks.
    /// </summary>
    private static void AddTUnitOpenTelemetry(IServiceCollection services)
    {
        services.AddOpenTelemetry().WithTracing(tracing => tracing
            .AddProcessor(new TUnitTestCorrelationProcessor())
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation());
    }

    /// <summary>
    /// Controls whether every registered <see cref="Microsoft.Extensions.Hosting.IHostedService"/>
    /// has its <c>StartAsync</c> dispatched onto a thread-pool worker with a clean
    /// <see cref="ExecutionContext"/>.
    /// <para>
    /// When enabled (the default), background work spawned inside a hosted service's
    /// <c>StartAsync</c> — synchronously or after an <c>await</c> — captures a clean
    /// execution context. Activities emitted later on background threads become orphan
    /// roots rather than inheriting the first test's <see cref="System.Diagnostics.Activity.Current"/>.
    /// Without this, spans from hosted-service work done during test B are attributed to
    /// test A's <c>TraceId</c>.
    /// </para>
    /// <para>
    /// Override and return <c>false</c> to preserve ambient context flow into hosted
    /// services — only needed if the hosted service intentionally relies on
    /// <c>Activity.Current</c> or other <see cref="System.Threading.AsyncLocal{T}"/>
    /// values captured at factory-build time, or requires <c>StartAsync</c> to run on
    /// the calling thread.
    /// </para>
    /// </summary>
    protected virtual bool SuppressHostedServiceExecutionContextFlow => true;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (SuppressHostedServiceExecutionContextFlow)
        {
            builder.ConfigureServices(DecorateHostedServicesWithFlowSuppression);
        }

        return base.CreateHost(builder);
    }

    private static void DecorateHostedServicesWithFlowSuppression(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++)
        {
            var descriptor = services[i];

            if (descriptor.ServiceType != typeof(IHostedService))
            {
                continue;
            }

            services[i] = WrapHostedServiceDescriptor(descriptor);
        }
    }

    private static ServiceDescriptor WrapHostedServiceDescriptor(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IHostedService instance)
        {
            return new ServiceDescriptor(
                typeof(IHostedService),
                _ => new FlowSuppressingHostedService(instance),
                descriptor.Lifetime);
        }

        if (descriptor.ImplementationFactory is { } factory)
        {
            return new ServiceDescriptor(
                typeof(IHostedService),
                sp => new FlowSuppressingHostedService((IHostedService)factory(sp)),
                descriptor.Lifetime);
        }

        if (descriptor.ImplementationType is { } implType)
        {
            return new ServiceDescriptor(
                typeof(IHostedService),
                sp => new FlowSuppressingHostedService(
                    (IHostedService)ActivatorUtilities.CreateInstance(sp, implType)),
                descriptor.Lifetime);
        }

        return descriptor;
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
        return base.CreateDefaultClient(TUnitHttpClientFilter.PrependPropagationHandlers(handlers));
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
