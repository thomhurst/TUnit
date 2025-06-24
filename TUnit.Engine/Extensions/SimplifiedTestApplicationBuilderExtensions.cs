using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using TUnit.Engine.Capabilities;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Framework;

namespace TUnit.Engine.Extensions;

/// <summary>
/// Simplified extension registration using the new architecture
/// </summary>
public static class SimplifiedTestApplicationBuilderExtensions
{
    public static void AddSimplifiedTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();

        // Register the simplified test framework
        testApplicationBuilder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(new[]
            {
                new TrxReportCapability(),
                new BannerCapability(
                    serviceProvider.GetRequiredService<IPlatformInformation>(), 
                    serviceProvider.GetCommandLineOptions()),
                new StopExecutionCapability()
            }),
            (capabilities, serviceProvider) => new SimplifiedTUnitTestFramework(
                extension, 
                serviceProvider, 
                capabilities));

        // Register essential command line providers
        testApplicationBuilder.CommandLine.AddProvider(() => new HideTestOutputCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new MaximumParallelTestsCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new FailFastCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new ReflectionScannerCommandProvider(extension));
        
        // Register tree node filter for test filtering
        testApplicationBuilder.AddTreeNodeFilterService(extension);
    }
}