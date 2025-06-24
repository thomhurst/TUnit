using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.Capabilities;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Framework;
using TUnit.Engine.Reporters;

#pragma warning disable TPEXP

namespace TUnit.Engine.Extensions;

public static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();

        var githubReporter = new GitHubReporter(extension);
        
        testApplicationBuilder.RegisterTestFramework(
            serviceProvider  => new TestFrameworkCapabilities(CreateCapabilities(serviceProvider)),
            (capabilities, serviceProvider) => new SimplifiedTUnitTestFramework(extension, serviceProvider, capabilities));
        
        testApplicationBuilder.AddTreeNodeFilterService(extension);
        testApplicationBuilder.AddMaximumFailedTestsService(extension);
        
        // TODO: testApplicationBuilder.CommandLine.AddProvider(() => new JsonOutputCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new HideTestOutputCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new MaximumParallelTestsCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new ParametersCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new FailFastCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new DisableLogoCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new DetailedStacktraceCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new ReflectionScannerCommandProvider(extension));

        testApplicationBuilder.TestHost.AddDataConsumer(_ => githubReporter);
        testApplicationBuilder.TestHost.AddTestApplicationLifecycleCallbacks(_ => githubReporter);
    }

    private static IReadOnlyCollection<ITestFrameworkCapability> CreateCapabilities(IServiceProvider serviceProvider)
    {
        return
        [
            new TrxReportCapability(),
            new BannerCapability(serviceProvider.GetRequiredService<IPlatformInformation>(), serviceProvider.GetCommandLineOptions()),
            new StopExecutionCapability(),
        ];
    }
}