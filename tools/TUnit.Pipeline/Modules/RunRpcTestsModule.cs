using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("NetworkTests")]
// The RPC tests `dotnet build` TUnit.TestProject without a runtime identifier, which re-restores
// the obj/project.assets.json the AOT publish needs for its RID (NETSDK1047 if they overlap).
[DependsOn<PublishTestProjectAOTModule>]
public class RunRpcTestsModule : TestBaseModule
{
    protected override IEnumerable<string> TestableFrameworks =>
    [
        "net10.0"
    ];

    protected override Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.RpcTests.csproj").AssertExists();

        return Task.FromResult<(DotNetRunOptions, CommandExecutionOptions?)>((
            new DotNetRunOptions
            {
                NoBuild = true,
                Configuration = "Release",
                Framework = framework,
                Arguments = ["--ignore-exit-code", "8"],
            },
            new CommandExecutionOptions
            {
                WorkingDirectory = project.Folder!.Path,
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["DISABLE_GITHUB_REPORTER"] = "true",
                }
            }
        ));
    }
}
