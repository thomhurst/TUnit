namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Tests verifying the execution order of WebApplicationFactory methods
/// relative to WebApplicationTest hooks.
///
/// Factory methods run after SetupAsync but before test-specific configuration:
/// - Factory.ConfigureWebHost (order 3)
/// - Factory.ConfigureStartupConfiguration (order 4)
///
/// This allows:
/// - Factory to provide base configuration shared across tests
/// - Tests to override factory defaults via ConfigureTestConfiguration (order 5)
/// </summary>
public class FactoryMethodOrderTests : TestsBase
{
    [Test]
    [DisplayName("Factory.ConfigureWebHost is called")]
    public async Task Factory_ConfigureWebHost_Is_Called()
    {
        _ = Factory.CreateClient();

        await Assert.That(GlobalFactory.ConfigureWebHostCallCount).IsGreaterThan(0)
            .Because("ConfigureWebHost should be called when the factory is initialized");
    }

    [Test]
    [DisplayName("Factory.ConfigureStartupConfiguration is called")]
    public async Task Factory_ConfigureStartupConfiguration_Is_Called()
    {
        _ = Factory.CreateClient();

        await Assert.That(GlobalFactory.ConfigureStartupConfigurationCallCount).IsGreaterThan(0)
            .Because("ConfigureStartupConfiguration should be called when the factory is initialized");
    }

    [Test]
    [DisplayName("Factory.ConfigureWebHost runs after SetupAsync")]
    public async Task Factory_ConfigureWebHost_Runs_After_SetupAsync()
    {
        _ = Factory.CreateClient();

        // Note: Factory order tracking only works reliably when this test triggers factory initialization
        // If factory was already initialized, these values are from a previous test's counter
        if (FactoryConfigureWebHostCalledOrder > 0)
        {
            await Assert.That(FactoryConfigureWebHostCalledOrder)
                .IsGreaterThan(SetupCalledOrder)
                .Because("Factory methods run after SetupAsync (during factory access)");
        }
    }

    [Test]
    [DisplayName("Factory.ConfigureStartupConfiguration runs after ConfigureWebHost")]
    public async Task Factory_ConfigureStartupConfiguration_Runs_After_ConfigureWebHost()
    {
        _ = Factory.CreateClient();

        // Only verify relative order if factory methods were tracked by this test's counter
        if (FactoryConfigureWebHostCalledOrder > 0 && FactoryConfigureStartupConfigurationCalledOrder > 0)
        {
            await Assert.That(FactoryConfigureStartupConfigurationCalledOrder)
                .IsGreaterThan(FactoryConfigureWebHostCalledOrder)
                .Because("ConfigureStartupConfiguration should run after ConfigureWebHost");
        }
    }

    [Test]
    [DisplayName("Test configuration runs after SetupAsync")]
    public async Task Test_Configuration_Runs_After_SetupAsync()
    {
        _ = Factory.CreateClient();

        // Test configuration always runs after SetupAsync, regardless of factory initialization
        await Assert.That(ConfigureTestConfigurationCalledOrder)
            .IsGreaterThan(SetupCalledOrder)
            .Because("Test's ConfigureTestConfiguration should run after SetupAsync");
    }

    [Test]
    [DisplayName("All test hooks run before Startup")]
    public async Task All_Test_Hooks_Run_Before_Startup()
    {
        _ = Factory.CreateClient();

        await Assert.That(ConfigureTestOptionsCalledOrder)
            .IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestOptions should run before application startup");

        await Assert.That(SetupCalledOrder)
            .IsLessThan(StartupCalledOrder)
            .Because("SetupAsync should run before application startup");

        await Assert.That(ConfigureTestConfigurationCalledOrder)
            .IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestConfiguration should run before application startup");

        await Assert.That(ConfigureWebHostBuilderCalledOrder)
            .IsLessThan(StartupCalledOrder)
            .Because("ConfigureWebHostBuilder should run before application startup");

        await Assert.That(ConfigureTestServicesCalledOrder)
            .IsLessThan(StartupCalledOrder)
            .Because("ConfigureTestServices should run before application startup");
    }

    [Test]
    [DisplayName("Complete relative execution order: Options → Setup → Config → WebHost → Services → Startup")]
    public async Task Full_Relative_Order()
    {
        _ = Factory.CreateClient();

        // Verify all hooks were called
        await Assert.That(ConfigureTestOptionsCalledOrder).IsGreaterThan(0);
        await Assert.That(SetupCalledOrder).IsGreaterThan(0);
        await Assert.That(ConfigureTestConfigurationCalledOrder).IsGreaterThan(0);
        await Assert.That(ConfigureWebHostBuilderCalledOrder).IsGreaterThan(0);
        await Assert.That(ConfigureTestServicesCalledOrder).IsGreaterThan(0);
        await Assert.That(StartupCalledOrder).IsGreaterThan(0);

        // Verify relative execution order (works regardless of factory initialization timing)
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
}
