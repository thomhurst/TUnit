﻿using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests")]
public class RunAnalyzersTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Analyzers.Tests.csproj").AssertExists();

        return await context.DotNet().Test(new DotNetTestOptions
        {
            WorkingDirectory = project.Folder!,
            NoBuild = true,
            Configuration = Configuration.Release,
            Framework = "net8.0",
            Arguments = ["--", "--hangdump", "--hangdump-filename", "hangdump.analyzers-tests.dmp", "--hangdump-timeout", "5m"],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            },
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);
    }
}
