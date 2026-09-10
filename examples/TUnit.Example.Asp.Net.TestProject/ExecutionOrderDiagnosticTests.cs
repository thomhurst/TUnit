namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Diagnostic test to capture and display the complete execution order.
/// </summary>
public class ExecutionOrderDiagnosticTests : TestsBase
{
    [Test]
    [DisplayName("Diagnostic: Show complete execution order")]
    public async Task Show_Complete_Execution_Order()
    {
        // Access the factory to trigger full initialization
        _ = Factory.CreateClient();

        // Output all order values for analysis
        Console.WriteLine("=== EXECUTION ORDER DIAGNOSTIC ===");
        Console.WriteLine($"1. ConfigureTestOptions:              {ConfigureTestOptionsCalledOrder}");
        Console.WriteLine($"2. SetupAsync:                        {SetupCalledOrder}");
        Console.WriteLine($"3. Factory.ConfigureWebHost:          {FactoryConfigureWebHostCalledOrder}");
        Console.WriteLine($"4. Factory.ConfigureStartupConfig:    {FactoryConfigureStartupConfigurationCalledOrder}");
        Console.WriteLine($"5. ConfigureTestConfiguration:        {ConfigureTestConfigurationCalledOrder}");
        Console.WriteLine($"6. ConfigureWebHostBuilder:           {ConfigureWebHostBuilderCalledOrder}");
        Console.WriteLine($"7. ConfigureTestServices:             {ConfigureTestServicesCalledOrder}");
        Console.WriteLine($"8. Startup (IStartupFilter):          {StartupCalledOrder}");
        Console.WriteLine("==================================");

        // Verify all were called
        await Assert.That(ConfigureTestOptionsCalledOrder).IsGreaterThan(0);
        await Assert.That(SetupCalledOrder).IsGreaterThan(0);
        await Assert.That(FactoryConfigureWebHostCalledOrder).IsGreaterThan(0);
        await Assert.That(FactoryConfigureStartupConfigurationCalledOrder).IsGreaterThan(0);
        await Assert.That(ConfigureWebHostBuilderCalledOrder).IsGreaterThan(0);
        await Assert.That(ConfigureTestConfigurationCalledOrder).IsGreaterThan(0);
        await Assert.That(ConfigureTestServicesCalledOrder).IsGreaterThan(0);
        await Assert.That(StartupCalledOrder).IsGreaterThan(0);
    }
}
