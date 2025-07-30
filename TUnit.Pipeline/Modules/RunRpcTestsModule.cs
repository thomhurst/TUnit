﻿using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests")]
public class RunRpcTestsModule : TestBaseModule
{
    protected override IEnumerable<string> TestableFrameworks =>
    [
        "net8.0"
    ];

    protected override Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.RpcTests.csproj").AssertExists();

        return Task.FromResult(new DotNetRunOptions
        {
            WorkingDirectory = project.Folder!,
            NoBuild = true,
            Configuration = Configuration.Release,
            Framework = framework,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            },
            Arguments = ["--ignore-exit-code", "8"],
        });
    }
}
