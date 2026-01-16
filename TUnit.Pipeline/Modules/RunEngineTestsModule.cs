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

[NotInParallel("DotNetTests")]
[DependsOn<RunAnalyzersTestsModule>]
[DependsOn<RunUnitTestsModule>]
[DependsOn<RunTemplateTestsModule>]
[DependsOn<RunAspNetTestsModule>]
[DependsOn<RunAssertionsTestsModule>]
[DependsOn<RunPlaywrightTestsModule>]
[DependsOn<RunRpcTestsModule>]
[DependsOn<RunAssertionsAnalyzersTestsModule>]
[DependsOn<RunPublicAPITestsModule>]
[DependsOn<RunSourceGeneratorTestsModule>]
[DependsOn<RunAssertionsCodeFixersTestsModule>]
public class RunEngineTestsModule : Module<CommandResult>
{
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
                "--hangdump", "--hangdump-filename", $"hangdump.{Environment.OSVersion.Platform}.engine-tests.dmp", "--hangdump-timeout", "30m",
                "--timeout", "35m",
            ],
        }, new CommandExecutionOptions
        {
            WorkingDirectory = project.Folder!.Path,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["TUNIT_DISABLE_GITHUB_REPORTER"] = "true",
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
