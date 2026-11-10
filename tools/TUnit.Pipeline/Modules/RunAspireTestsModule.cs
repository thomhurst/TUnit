using System.Runtime.InteropServices;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel]
public class RunAspireTestsModule : Module<CommandResult?>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return null;
        }

        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.FindFile(x => x.Name == "TUnit.Aspire.Tests.csproj").AssertExists();

        return await context.Tools.DotNet.RunAsync(new DotNetRunOptions
        {
            Project = project.Name,
            NoBuild = true,
            Configuration = "Release",
            Framework = "net10.0",
            Arguments = [.. HangDumpArguments.Create("hangdump.aspire-tests.dmp", "5m")],
        }, new CommandExecutionOptions
        {
            WorkingDirectory = project.Folder!.Path,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            },
            Logging = new CommandLoggingOptions
            {
                ShowCommandArguments = true,
                ShowStandardError = true,
                ShowExecutionTime = true,
                ShowExitCode = true
            }
        }, cancellationToken);
    }
}
