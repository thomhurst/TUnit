using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.PlatformNeutral)]
[NotInParallel("SnapshotTests")]
public class RunTemplateTestsModule : Module<CommandResult?>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        // Templates have no .NET Framework variant, so an OS-specific run leaves them to Ubuntu.
        if (PipelineScope.OsSpecificOnly)
        {
            return null;
        }

        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.FindFile(x => x.Name == "TUnit.Templates.Tests.csproj").AssertExists();

        return await context.Tools.DotNet.TestAsync(new DotNetTestOptions
        {
            NoBuild = true,
            Configuration = "Release",
            Framework = "net10.0",
            ExtensionOptions = [.. HangDumpArguments.Create("hangdump.template-tests.dmp", "5m")],
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
