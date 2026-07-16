using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[NotInParallel]
public class RunAspireTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return null;
        }

        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Aspire.Tests.csproj").AssertExists();

        return await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project.Name,
            NoBuild = true,
            Configuration = "Release",
            Framework = "net10.0",
            Arguments = ["--hangdump", "--hangdump-filename", "hangdump.aspire-tests.dmp", "--hangdump-timeout", "5m"],
        }, new CommandExecutionOptions
        {
            WorkingDirectory = project.Folder!.Path,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
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
