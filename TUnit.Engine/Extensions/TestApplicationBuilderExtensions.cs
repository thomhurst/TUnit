using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.Capabilities;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Framework;
using TUnit.Engine.Reporters;
using Microsoft.Testing.Platform.CommandLine;

#pragma warning disable TPEXP

namespace TUnit.Engine.Extensions;

public static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();

        var githubReporter = new GitHubReporter(extension);
        var githubReporterCommandProvider = new GitHubReporterCommandProvider(extension);

        testApplicationBuilder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(CreateCapabilities(serviceProvider)),
            (capabilities, serviceProvider) => new TUnitTestFramework(extension, serviceProvider, capabilities));

        testApplicationBuilder.AddTreeNodeFilterService(extension);
        testApplicationBuilder.AddMaximumFailedTestsService(extension);

        // Core functionality command providers
        testApplicationBuilder.CommandLine.AddProvider(() => new MaximumParallelTestsCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new ParametersCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new FailFastCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new ReflectionModeCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new DisableLogoCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new LogLevelCommandProvider(extension));

        // Adaptive parallelism command providers
        testApplicationBuilder.CommandLine.AddProvider(() => new ParallelismStrategyCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => new AdaptiveMetricsCommandProvider(extension));

        // Unified verbosity control (replaces HideTestOutput, DisableLogo)
        testApplicationBuilder.CommandLine.AddProvider(() => new VerbosityCommandProvider(extension));
        
        // Keep detailed stacktrace option for backward compatibility 
        testApplicationBuilder.CommandLine.AddProvider(() => new DetailedStacktraceCommandProvider(extension));

        // GitHub reporter configuration
        testApplicationBuilder.CommandLine.AddProvider(() => githubReporterCommandProvider);

        testApplicationBuilder.TestHost.AddDataConsumer(serviceProvider =>
        {
            // Apply command-line configuration if provided
            var commandLineOptions = serviceProvider.GetRequiredService<ICommandLineOptions>();
            if (commandLineOptions.TryGetOptionArgumentList(GitHubReporterCommandProvider.GitHubReporterStyleOption, out var styleArgs))
            {
                var style = GitHubReporterCommandProvider.ParseReporterStyle(styleArgs);
                githubReporter.SetReporterStyle(style);
            }
            return githubReporter;
        });
        testApplicationBuilder.TestHost.AddTestHostApplicationLifetime(_ => githubReporter);
    }

    private static IReadOnlyCollection<ITestFrameworkCapability> CreateCapabilities(IServiceProvider serviceProvider)
    {
        return
        [
            new TrxReportCapability(),
            new BannerCapability(serviceProvider.GetRequiredService<IPlatformInformation>(), serviceProvider.GetCommandLineOptions(), serviceProvider.GetLoggerFactory()),
            new StopExecutionCapability(),
        ];
    }
}
