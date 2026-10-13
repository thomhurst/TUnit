using System.ComponentModel;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore.Interception;
using TUnit.Core;

namespace TUnit.AspNetCore;

public abstract class WebApplicationTest
{
    // Shared across all generic instantiations of WebApplicationTest<TFactory, TEntryPoint>.
    // WebApplicationFactory.Services is synchronous; Task.Run prevents blocking async threads,
    // and this semaphore caps concurrent DI container builds to avoid thread pool starvation.
    // Capped at 8: startup is reflection/I/O-bound, not CPU-bound, so ProcessorCount alone
    // would allow too many concurrent builds on high-core-count machines.
    private static readonly int _maxConcurrentServerInits = Math.Min(Environment.ProcessorCount * 2, 8);
    private protected static readonly SemaphoreSlim ServerInitSemaphore =
        new(_maxConcurrentServerInits, _maxConcurrentServerInits);

    /// <summary>
    /// Gets a unique identifier for this test instance.
    /// Delegates to <see cref="TestContext.Isolation"/> to ensure consistency
    /// regardless of whether accessed via this property or the TestContext API.
    /// </summary>
    public int UniqueId => TestContext.Current!.Isolation.UniqueId;

    internal WebApplicationTest()
    {
    }

    /// <summary>
    /// Creates an isolated name by combining a base name with the test's unique identifier.
    /// Use for database tables, Redis keys, Kafka topics, etc.
    /// </summary>
    /// <param name="baseName">The base name for the resource.</param>
    /// <returns>A unique name in the format "test_{UniqueId}_{baseName}".</returns>
    /// <example>
    /// <code>
    /// // In a test with UniqueId = 42:
    /// var tableName = GetIsolatedName("todos");  // Returns "test_42_todos"
    /// var topicName = GetIsolatedName("orders"); // Returns "test_42_orders"
    /// </code>
    /// </example>
    protected string GetIsolatedName(string baseName) => TestContext.Current!.Isolation.GetIsolatedName(baseName);

    /// <summary>
    /// Creates an isolated prefix using the test's unique identifier.
    /// Use for key prefixes in Redis, Kafka topic prefixes, etc.
    /// </summary>
    /// <param name="separator">The separator character. Defaults to "_".</param>
    /// <returns>A unique prefix in the format "test{separator}{UniqueId}{separator}".</returns>
    /// <example>
    /// <code>
    /// // In a test with UniqueId = 42:
    /// var prefix = GetIsolatedPrefix();       // Returns "test_42_"
    /// var dotPrefix = GetIsolatedPrefix("."); // Returns "test.42."
    /// </code>
    /// </example>
    protected string GetIsolatedPrefix(string separator = "_") => TestContext.Current!.Isolation.GetIsolatedPrefix(separator);
}

/// <summary>
/// Base class for ASP.NET Core integration tests where each test configures its own application
/// by calling <see cref="CreateFactoryAsync(Action{IWebHostBuilder})"/>.
/// </summary>
/// <typeparam name="TFactory">The factory type derived from TestWebApplicationFactory.</typeparam>
/// <typeparam name="TEntryPoint">The entry point class (typically 'Program') of the application under test.</typeparam>
/// <remarks>
/// <para>
/// Both this class and <see cref="WebApplicationTest{TFactory, TEntryPoint}"/> give every test its own
/// isolated application. The difference is where the configuration comes from:
/// <see cref="WebApplicationTest{TFactory, TEntryPoint}"/> builds one application before each test from
/// the class-level overrides, so every test in the class gets the same configuration. This class builds
/// nothing before the test; each test calls <see cref="CreateFactoryAsync(Action{IWebHostBuilder})"/>
/// with its own configuration.
/// </para>
/// <para>
/// Use this class when tests need application configuration that is only known in the test body,
/// such as a fake that the test creates and asserts on afterwards. A test pays only for the
/// applications it creates.
/// </para>
/// <para>
/// Factories returned by <see cref="CreateFactoryAsync(Action{IWebHostBuilder})"/> have the same isolation,
/// logging, tracing and test context wiring as <see cref="WebApplicationTest{TFactory, TEntryPoint}.Factory"/>,
/// and are disposed after the test. Use <see cref="WebApplicationTest{TFactory, TEntryPoint}"/> when every
/// test in the class uses the same configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PaymentTests : PerTestWebApplicationTest&lt;MyFactory, Program&gt;
/// {
///     [Test]
///     public async Task Rejects_When_Payment_Fails()
///     {
///         var payments = new FakePayments(fail: true);
///         var factory = await CreateFactoryAsync(builder =>
///             builder.ConfigureTestServices(services => services.ReplaceService&lt;IPayments&gt;(payments)));
///
///         using var client = factory.CreateClient();
///         // ...
///     }
/// }
/// </code>
/// </example>
public abstract class PerTestWebApplicationTest<TFactory, TEntryPoint> : WebApplicationTest
    where TFactory : TestWebApplicationFactory<TEntryPoint>, new()
    where TEntryPoint : class
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TFactory GlobalFactory { get; set; } = null!;

    // A test may call CreateFactoryAsync from concurrent tasks, so tracking is locked.
    private readonly List<TracedWebApplicationFactory<TEntryPoint>> _factories = [];
    private readonly object _factoriesLock = new();

    private protected WebApplicationTestOptions ConfiguredOptions { get; } = new();

    /// <summary>
    /// Runs <see cref="ConfigureTestOptions"/> and <see cref="SetupAsync"/> before each test.
    /// This hook runs after all property injection is complete, ensuring
    /// that dependencies like database containers are available.
    /// </summary>
    [Before(HookType.Test)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task InitializeFactoryAsync(TestContext testContext)
    {
        ConfigureTestOptions(ConfiguredOptions);

        // Run async setup first - use this for database/container initialization
        await SetupAsync();

        await OnSetupCompletedAsync(testContext);
    }

    private protected virtual Task OnSetupCompletedAsync(TestContext testContext) => Task.CompletedTask;

    /// <summary>
    /// Creates and starts a new isolated application for the current test.
    /// <paramref name="configureWebHostBuilder"/> runs after the class-level configuration
    /// (<see cref="ConfigureWebHostBuilder"/>, <see cref="ConfigureTestServices"/> and
    /// <see cref="ConfigureTestConfiguration"/>), so it can replace services or override
    /// settings for this test only.
    /// </summary>
    /// <param name="configureWebHostBuilder">Test-specific configuration for the web host.</param>
    /// <returns>The started factory. It is disposed automatically after the test.</returns>
    /// <remarks>Each call builds a separate application instance.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when called outside a running test.</exception>
    protected Task<TracedWebApplicationFactory<TEntryPoint>> CreateFactoryAsync(
        Action<IWebHostBuilder>? configureWebHostBuilder = null)
    {
        var testContext = TestContext.Current
            ?? throw new InvalidOperationException("CreateFactoryAsync must be called from within a running test.");

        return CreateFactoryAsync(testContext, configureWebHostBuilder);
    }

    private protected async Task<TracedWebApplicationFactory<TEntryPoint>> CreateFactoryAsync(
        TestContext testContext,
        Action<IWebHostBuilder>? configureWebHostBuilder)
    {
        // Create factory with sync configuration (required by ASP.NET Core hosting)
        var factory = new TracedWebApplicationFactory<TEntryPoint>(
            GlobalFactory.GetIsolatedFactory(
                testContext,
                ConfiguredOptions,
                ConfigureTestServices,
                (_, config) => ConfigureTestConfiguration(config),
                configureWebHostBuilder is null
                    ? ConfigureWebHostBuilder
                    : builder =>
                    {
                        ConfigureWebHostBuilder(builder);
                        configureWebHostBuilder(builder);
                    }));

        // Tracked before the host build so a factory whose build fails is still disposed after the test.
        lock (_factoriesLock)
        {
            _factories.Add(factory);
        }

        // Services initializes both TestServer and Kestrel; Server only supports TestServer.
        // Semaphore guards only the Services property access (the synchronous host build),
        // not the factory creation above which is fast synchronous configuration.
        await ServerInitSemaphore.WaitAsync(testContext.Execution.CancellationToken);
        try
        {
            await Task.Run(() => _ = factory.Services, testContext.Execution.CancellationToken);
        }
        finally
        {
            ServerInitSemaphore.Release();
        }

        return factory;
    }

    [After(HookType.Test)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task DisposeFactoryAsync()
    {
        TracedWebApplicationFactory<TEntryPoint>[] factories;
        lock (_factoriesLock)
        {
            factories = [.. _factories];
            _factories.Clear();
        }

        // Dispose every factory even if an earlier one throws, so no application outlives the test.
        List<Exception>? exceptions = null;
        foreach (var factory in factories)
        {
            try
            {
                await factory.DisposeAsync();
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
        }

        if (exceptions is [var single])
        {
            ExceptionDispatchInfo.Capture(single).Throw();
        }

        if (exceptions is not null)
        {
            throw new AggregateException(exceptions);
        }
    }

    /// <summary>
    /// Override to perform async setup before the factory is created.
    /// Use this for operations that require async (database table creation,
    /// container health checks, external service initialization, etc.).
    /// </summary>
    /// <remarks>
    /// This method runs BEFORE <see cref="ConfigureTestServices"/> and
    /// <see cref="ConfigureTestConfiguration"/>. Store any results in
    /// instance fields for use in the configuration methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override async Task SetupAsync()
    /// {
    ///     TableName = GetIsolatedName("todos");
    ///     await CreateTableAsync(TableName);
    /// }
    ///
    /// protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    /// {
    ///     config.AddInMemoryCollection(new Dictionary&lt;string, string?&gt;
    ///     {
    ///         { "Database:TableName", TableName }
    ///     });
    /// }
    /// </code>
    /// </example>
    protected virtual Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual void ConfigureTestOptions(WebApplicationTestOptions options)
    {
    }

    /// <summary>
    /// Override to configure additional services for the test.
    /// Called synchronously during factory creation (ASP.NET Core requirement).
    /// For async setup, use <see cref="SetupAsync"/> instead.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <example>
    /// <code>
    /// protected override void ConfigureTestServices(IServiceCollection services)
    /// {
    ///     services.ReplaceService&lt;IEmailService&gt;(new FakeEmailService());
    /// }
    /// </code>
    /// </example>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Override to configure the application for the test.
    /// Called synchronously during factory creation (ASP.NET Core requirement).
    /// For async setup, use <see cref="SetupAsync"/> instead.
    /// </summary>
    /// <param name="config">The configuration builder to configure.</param>
    /// <example>
    /// <code>
    /// protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    /// {
    ///     config.AddInMemoryCollection(new Dictionary&lt;string, string?&gt;
    ///     {
    ///         { "Database:TableName", GetIsolatedName("todos") }
    ///     });
    /// }
    /// </code>
    /// </example>
    protected virtual void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
    }

    /// <summary>
    /// Override to configure the web host builder directly.
    /// This is an escape hatch for advanced scenarios not covered by other configuration methods.
    /// Called first, before <see cref="ConfigureTestServices"/> and <see cref="ConfigureTestConfiguration"/>.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    /// <example>
    /// <code>
    /// protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    /// {
    ///     builder.UseEnvironment("Staging");
    ///     builder.UseSetting("MyFeature:Enabled", "true");
    ///     builder.ConfigureKestrel(options => options.AddServerHeader = false);
    /// }
    /// </code>
    /// </example>
    protected virtual void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
    }
}

/// <summary>
/// Base class for ASP.NET Core integration tests with TUnit.
/// Provides per-test isolated web application factories via the delegating factory pattern.
/// </summary>
/// <typeparam name="TFactory">The factory type derived from TestWebApplicationFactory.</typeparam>
/// <typeparam name="TEntryPoint">The entry point class (typically 'Program') of the application under test.</typeparam>
/// <remarks>
/// <para>
/// This class creates a global <see cref="WebApplicationFactory{TEntryPoint}"/> once per test class type,
/// then creates a per-test delegating factory using <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/>
/// for each test. This enables parallel test execution with complete isolation.
/// </para>
/// <para>
/// The factory is created in a BeforeTest hook, ensuring all other injected properties
/// (such as database containers) are available before the factory is configured.
/// </para>
/// <para>
/// To configure the application differently for individual tests, derive from
/// <see cref="PerTestWebApplicationTest{TFactory, TEntryPoint}"/> instead, or call
/// <see cref="PerTestWebApplicationTest{TFactory, TEntryPoint}.CreateFactoryAsync(Action{IWebHostBuilder})"/>
/// to build an additional application alongside <see cref="Factory"/>.
/// </para>
/// </remarks>
public abstract class WebApplicationTest<TFactory, TEntryPoint> : PerTestWebApplicationTest<TFactory, TEntryPoint>
    where TFactory : TestWebApplicationFactory<TEntryPoint>, new()
    where TEntryPoint : class
{
    private TracedWebApplicationFactory<TEntryPoint>? _factory;

    /// <summary>
    /// Gets the per-test factory, isolated to the current test.
    /// All <see cref="TracedWebApplicationFactory{TEntryPoint}.CreateClient()"/> calls on this factory
    /// automatically inject activity tracing and test context propagation handlers.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before test setup.</exception>
    public TracedWebApplicationFactory<TEntryPoint> Factory => _factory ?? throw new InvalidOperationException(
            "Factory is not initialized. Ensure the test has started and the BeforeTest hook has run. " +
            "Do not access Factory during test discovery or in data source methods.");

    /// <summary>
    /// Gets the service provider from the per-test factory.
    /// Use this to resolve services for verification or setup.
    /// </summary>
    public IServiceProvider Services => Factory.Services;

    private protected override async Task OnSetupCompletedAsync(TestContext testContext)
    {
        _factory = await CreateFactoryAsync(testContext, configureWebHostBuilder: null);
    }

    /// <summary>
    /// Gets the HTTP exchange capture store, if enabled via <see cref="WebApplicationTestOptions.EnableHttpExchangeCapture"/>.
    /// Returns null if HTTP exchange capture is not enabled.
    /// </summary>
    /// <remarks>
    /// This is the <see cref="HttpExchangeCapture"/> registered in <see cref="Factory"/>'s services,
    /// which the capture middleware records into.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if capture is enabled and the factory is not initialized yet.</exception>
    /// <example>
    /// <code>
    /// protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    /// {
    ///     options.EnableHttpExchangeCapture = true;
    /// }
    ///
    /// [Test]
    /// public async Task CapturesRequests()
    /// {
    ///     var client = Factory.CreateClient();
    ///     await client.GetAsync("/api/todos");
    ///
    ///     await Assert.That(HttpCapture).IsNotNull();
    ///     await Assert.That(HttpCapture!.Last!.Response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    /// }
    /// </code>
    /// </example>
    public HttpExchangeCapture? HttpCapture =>
        ConfiguredOptions.EnableHttpExchangeCapture ? Services.GetRequiredService<HttpExchangeCapture>() : null;
}
