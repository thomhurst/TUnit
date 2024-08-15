using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.Capabilities;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Framework;
#pragma warning disable TPEXP

namespace TUnit.Engine.Extensions;

internal static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();
        
        testApplicationBuilder.RegisterTestFramework(
            serviceProvider  => new TestFrameworkCapabilities(new TrxReportCapability(), new BannerCapability(serviceProvider.GetRequiredService<IPlatformInformation>())),
            (capabilities, serviceProvider) => new TUnitTestFramework(extension, serviceProvider, capabilities));
        
        testApplicationBuilder.AddTreeNodeFilterService(extension);
        testApplicationBuilder.CommandLine.AddProvider(() => new JsonOutputCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new HideTestOutputCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new MaximumParallelTestsCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new ParametersCommandProvider(extension));
    }
}