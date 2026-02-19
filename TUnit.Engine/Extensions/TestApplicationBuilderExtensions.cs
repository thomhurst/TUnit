using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
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

        var junitReporter = new JUnitReporter(extension);
        var junitReporterCommandProvider = new JUnitReporterCommandProvider(extension);

        var timingReporter = new TimingReporter(extension);
        var timingReporterCommandProvider = new TimingReporterCommandProvider(extension);

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

        // Keep detailed stacktrace option for backward compatibility
        testApplicationBuilder.CommandLine.AddProvider(() => new DetailedStacktraceCommandProvider(extension));

        // GitHub reporter configuration
        testApplicationBuilder.CommandLine.AddProvider(() => githubReporterCommandProvider);

        // JUnit reporter configuration
        testApplicationBuilder.CommandLine.AddProvider(() => junitReporterCommandProvider);

        // Performance baseline and timing reporter command providers
        testApplicationBuilder.CommandLine.AddProvider(() => new PerformanceBaselineCommandProvider(extension));
        testApplicationBuilder.CommandLine.AddProvider(() => timingReporterCommandProvider);

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

        testApplicationBuilder.TestHost.AddDataConsumer(serviceProvider =>
        {
            // Apply command-line configuration if provided
            var commandLineOptions = serviceProvider.GetRequiredService<ICommandLineOptions>();
            if (commandLineOptions.TryGetOptionArgumentList(JUnitReporterCommandProvider.JUnitOutputPathOption, out var pathArgs))
            {
                junitReporter.SetOutputPath(pathArgs[0]);
            }
            return junitReporter;
        });
        testApplicationBuilder.TestHost.AddTestHostApplicationLifetime(_ => junitReporter);

        // Timing reporter configuration (enabled via --report-timing)
        testApplicationBuilder.TestHost.AddDataConsumer(serviceProvider =>
        {
            var commandLineOptions = serviceProvider.GetRequiredService<ICommandLineOptions>();

            // Configure performance baseline fail mode
            if (commandLineOptions.IsOptionSet(PerformanceBaselineCommandProvider.PerformanceBaselineFail))
            {
                PerformanceBaselineAttribute.FailOnViolation = true;
            }

            // Enable timing reporter when --report-timing is specified
            if (commandLineOptions.IsOptionSet(TimingReporterCommandProvider.ReportTiming))
            {
                timingReporter.Enable();
            }

            // Configure timing reporter output path
            if (commandLineOptions.TryGetOptionArgumentList(TimingReporterCommandProvider.ReportTimingOutputPath, out var pathArgs))
            {
                timingReporter.SetOutputPath(pathArgs[0]);
                timingReporter.Enable(); // Specifying a path implies enabling
            }

            return timingReporter;
        });
        testApplicationBuilder.TestHost.AddTestHostApplicationLifetime(_ => timingReporter);
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
