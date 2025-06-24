using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Services;
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
            serviceProvider => new TestFrameworkCapabilities(new ITestFrameworkCapability[]
            {
                new TrxReportCapability(),
                new BannerCapability(
                    #pragma warning disable TPEXP // Experimental API
                    serviceProvider.GetRequiredService<IPlatformInformation>(), 
                    #pragma warning restore TPEXP
                    serviceProvider.GetCommandLineOptions()),
                #pragma warning disable TPEXP // Experimental API
                new StopExecutionCapability()
                #pragma warning restore TPEXP
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
        
    }
}