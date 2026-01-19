namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Integration tests for ASP.NET Core lifecycle hook execution order.
/// Uses atomic order counters for precise ordering verification.
///
/// Verified execution order:
/// 1. ConfigureTestOptions - Set test options (HTTP capture, etc.)
/// 2. SetupAsync - Async setup (create DB tables, prepare test data)
/// 3. Factory.ConfigureWebHost - Base factory web host configuration
/// 4. Factory.ConfigureStartupConfiguration - Base factory startup config
/// 5. ConfigureTestConfiguration - Test-specific app configuration (can override factory)
/// 6. ConfigureWebHostBuilder - Low-level web host builder access (escape hatch)
/// 7. ConfigureTestServices - Test-specific service registration (can override factory)
/// 8. Startup (IStartupFilter) - Application starts
///
/// This order allows:
/// - Tests to do async setup before configuration
/// - Factory to provide base configuration
/// - Tests to override factory defaults
/// - Low-level access via ConfigureWebHostBuilder as escape hatch
/// - All configuration completes before app starts
/// </summary>
public class Tests : TestsBase
{
    [Test]
    public async Task Ping_ReturnsHelloWorld()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/ping");

        response.EnsureSuccessStatusCode();

        var stringContent = await response.Content.ReadAsStringAsync();

        await Assert.That(stringContent).IsEqualTo("Hello, World!");
    }

    [Test]
    [DisplayName("All hooks were called (order values are non-zero)")]
    public async Task All_Hooks_Were_Called()
    {
        // Trigger factory initialization
        _ = Factory.CreateClient();

        // Verify all hooks were actually invoked
        await Assert.That(ConfigureTestOptionsCalledOrder).IsGreaterThan(0)
            .Because("ConfigureTestOptions should have been called");

        await Assert.That(SetupCalledOrder).IsGreaterThan(0)
            .Because("SetupAsync should have been called");

        await Assert.That(ConfigureTestConfigurationCalledOrder).IsGreaterThan(0)
            .Because("ConfigureTestConfiguration should have been called");

        await Assert.That(ConfigureWebHostBuilderCalledOrder).IsGreaterThan(0)
            .Because("ConfigureWebHostBuilder should have been called");

        await Assert.That(ConfigureTestServicesCalledOrder).IsGreaterThan(0)
            .Because("ConfigureTestServices should have been called");

        await Assert.That(StartupCalledOrder).IsGreaterThan(0)
            .Because("Application startup should have been triggered");
    }

    [Test]
    [DisplayName("ConfigureTestOptions runs first (before SetupAsync)")]
    public async Task ConfigureTestOptions_Runs_First()
    {
        _ = Factory.CreateClient();

        await Assert.That(ConfigureTestOptionsCalledOrder)
            .IsLessThan(SetupCalledOrder)
            .Because("ConfigureTestOptions should run before SetupAsync");
    }

    [Test]
    [DisplayName("SetupAsync runs after ConfigureTestOptions")]
    public async Task SetupAsync_Runs_After_ConfigureTestOptions()
    {
        _ = Factory.CreateClient();

        await Assert.That(SetupCalledOrder).IsGreaterThan(ConfigureTestOptionsCalledOrder)
            .Because("SetupAsync should run after ConfigureTestOptions");
    }

    [Test]
    [DisplayName("ConfigureTestConfiguration runs after SetupAsync")]
    public async Task ConfigureTestConfiguration_Runs_After_SetupAsync()
    {
        _ = Factory.CreateClient();

        await Assert.That(ConfigureTestConfigurationCalledOrder).IsGreaterThan(SetupCalledOrder)
            .Because("ConfigureTestConfiguration should run after SetupAsync");
    }

    [Test]
    [DisplayName("ConfigureWebHostBuilder runs after ConfigureTestConfiguration")]
    public async Task ConfigureWebHostBuilder_Runs_After_ConfigureTestConfiguration()
    {
        _ = Factory.CreateClient();

        await Assert.That(ConfigureWebHostBuilderCalledOrder)
            .IsGreaterThan(ConfigureTestConfigurationCalledOrder)
            .Because("ConfigureWebHostBuilder is an escape hatch called after ConfigureTestConfiguration");
    }

    [Test]
    [DisplayName("ConfigureTestServices runs after ConfigureWebHostBuilder")]
    public async Task ConfigureTestServices_Runs_After_ConfigureWebHostBuilder()
    {
        _ = Factory.CreateClient();

        await Assert.That(ConfigureTestServicesCalledOrder)
            .IsGreaterThan(ConfigureWebHostBuilderCalledOrder)
            .Because("ConfigureTestServices should run after ConfigureWebHostBuilder");
    }

    [Test]
    [DisplayName("Startup runs last (after all configuration)")]
    public async Task Startup_Runs_Last()
    {
        _ = Factory.CreateClient();

        await Assert.That(StartupCalledOrder).IsGreaterThan(ConfigureTestServicesCalledOrder)
            .Because("Application startup should run after all configuration hooks");
    }

    [Test]
    [DisplayName("Full relative execution order verification")]
    public async Task Full_Execution_Order()
    {
        _ = Factory.CreateClient();

        // Verify the complete relative execution order
        // Note: Absolute positions vary when factory is shared across tests
        await Assert.That(ConfigureTestOptionsCalledOrder)
            .IsLessThan(SetupCalledOrder)
            .Because("ConfigureTestOptions runs before SetupAsync");

        await Assert.That(SetupCalledOrder)
            .IsLessThan(ConfigureTestConfigurationCalledOrder)
            .Because("SetupAsync runs before ConfigureTestConfiguration");

        await Assert.That(ConfigureTestConfigurationCalledOrder)
            .IsLessThan(ConfigureWebHostBuilderCalledOrder)
            .Because("ConfigureTestConfiguration runs before ConfigureWebHostBuilder");

        await Assert.That(ConfigureWebHostBuilderCalledOrder)
            .IsLessThan(ConfigureTestServicesCalledOrder)
            .Because("ConfigureWebHostBuilder runs before ConfigureTestServices");

        await Assert.That(ConfigureTestServicesCalledOrder)
            .IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestServices runs before Startup");
    }

    [Test]
    [DisplayName("All configuration completes before startup")]
    public async Task All_Configuration_Hooks_Run_Before_Startup()
    {
        _ = Factory.CreateClient();

        await Assert.That(ConfigureTestOptionsCalledOrder).IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestOptions should run before the application starts");

        await Assert.That(SetupCalledOrder).IsLessThan(StartupCalledOrder)
            .Because("SetupAsync should run before the application starts");

        await Assert.That(ConfigureTestConfigurationCalledOrder).IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestConfiguration should run before the application starts");

        await Assert.That(ConfigureWebHostBuilderCalledOrder).IsLessThan(StartupCalledOrder)
            .Because("ConfigureWebHostBuilder should run before the application starts");

        await Assert.That(ConfigureTestServicesCalledOrder).IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestServices should run before the application starts");
    }
}
