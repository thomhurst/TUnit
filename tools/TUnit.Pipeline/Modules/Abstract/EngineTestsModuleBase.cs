using System.Globalization;
using System.Runtime.InteropServices;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules.Abstract;

// Runs TUnit.Engine.Tests for one TestMode (see InvokableTestBase.GetTestModes), so the
// Reflection half need not wait for the AOT publish.
public abstract class EngineTestsModuleBase : Module<CommandResult>
{
    protected abstract string TestMode { get; }

    protected virtual IEnumerable<string> AdditionalArguments => [];

    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Engine.Tests.csproj").AssertExists();

        return await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project.Name,
            NoBuild = true,
            Configuration = "Release",
            Framework = "net10.0",
            Arguments = [
                "--hangdump", "--hangdump-filename", $"hangdump.{RuntimeInformation.RuntimeIdentifier}.engine-tests-{TestMode}.dmp", "--hangdump-timeout", "30m",
                "--timeout", "35m",
                // Each engine test mostly waits on a child test-application process, so the
                // engine's default limit (twice the processor count) leaves the CPUs idle. Four
                // times the processor count was the default before #7354.
                "--maximum-parallel-tests", (Environment.ProcessorCount * 4).ToString(CultureInfo.InvariantCulture),
                ..AdditionalArguments,
            ],
        }, new CommandExecutionOptions
        {
            WorkingDirectory = project.Folder!.Path,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["TUNIT_DISABLE_GITHUB_REPORTER"] = "true",
                // Engine tests spawn hundreds of short-lived child TUnit processes (many
                // designed to fail). With aggregation on by default in CI, every child
                // would re-merge the job's shared report on exit — pure overhead here,
                // and the failures would pollute the job's aggregated summary.
                ["TUNIT_AGGREGATE_REPORTS"] = "off",
                ["TUNIT_ENGINE_TEST_MODE"] = TestMode,
            },
            LogSettings = new CommandLoggingOptions
            {
                ShowCommandArguments = true,
                ShowStandardError = true,
                ShowExecutionTime = true,
                ShowExitCode = true
            }
        }, cancellationToken);
    }
}
