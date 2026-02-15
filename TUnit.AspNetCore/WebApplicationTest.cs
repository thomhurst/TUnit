using System.ComponentModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore.Interception;
using TUnit.Core;

namespace TUnit.AspNetCore;

public abstract class WebApplicationTest
{
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
    /// <returns>A unique name in the format "Test_{UniqueId}_{baseName}".</returns>
    /// <example>
    /// <code>
    /// // In a test with UniqueId = 42:
    /// var tableName = GetIsolatedName("todos");  // Returns "Test_42_todos"
    /// var topicName = GetIsolatedName("orders"); // Returns "Test_42_orders"
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
/// </remarks>
public abstract class WebApplicationTest<TFactory, TEntryPoint> : WebApplicationTest
    where TFactory : TestWebApplicationFactory<TEntryPoint>, new()
    where TEntryPoint : class
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TFactory GlobalFactory { get; set; } = null!;

    private WebApplicationFactory<TEntryPoint>? _factory;

    private readonly WebApplicationTestOptions _options = new();

    /// <summary>
    /// Gets the per-test delegating factory. This factory is isolated to the current test.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before test setup.</exception>
    public WebApplicationFactory<TEntryPoint> Factory => _factory ?? throw new InvalidOperationException(
            "Factory is not initialized. Ensure the test has started and the BeforeTest hook has run. " +
            "Do not access Factory during test discovery or in data source methods.");

    /// <summary>
    /// Gets the service provider from the per-test factory.
    /// Use this to resolve services for verification or setup.
    /// </summary>
    public IServiceProvider Services => Factory.Services;

    /// <summary>
    /// Initializes the isolated web application factory before each test.
    /// This hook runs after all property injection is complete, ensuring
    /// that dependencies like database containers are available.
    /// </summary>
    [Before(HookType.Test)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task InitializeFactoryAsync(TestContext testContext)
    {
        ConfigureTestOptions(_options);

        // Run async setup first - use this for database/container initialization
        await SetupAsync();

        // Then create factory with sync configuration (required by ASP.NET Core hosting)
        _factory = GlobalFactory.GetIsolatedFactory(
            testContext,
            _options,
            ConfigureTestServices,
            ConfigureTestConfiguration,
            (_, config) => ConfigureTestConfiguration(config),
            ConfigureWebHostBuilder);

        // Eagerly start the test server to catch configuration errors early
        _ = _factory.Server;
    }

    [After(HookType.Test)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task DisposeFactoryAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
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

    /// <summary>
    /// Gets the HTTP exchange capture store, if enabled via <see cref="WebApplicationTestOptions.EnableHttpExchangeCapture"/>.
    /// Returns null if HTTP exchange capture is not enabled.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override WebApplicationTestOptions Options => new() { EnableHttpExchangeCapture = true };
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
        _options.EnableHttpExchangeCapture ? (field ??= new()) : null;
}
