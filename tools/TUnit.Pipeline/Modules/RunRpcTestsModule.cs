using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The RPC tests `dotnet run` TUnit.TestProject, which reads its obj/project.assets.json. The AOT
// publish restores into its own artifacts tree, so it no longer rewrites that file under them
// (an overlapping restore caused NETSDK1047).
[NotInParallel("NetworkTests")]
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
                    // The workflow's solution build already produced the Release test host.
                    ["TUNIT_RPC_TESTPROJECT_PREBUILT"] = "true",
                }
            }
        ));
    }
}
