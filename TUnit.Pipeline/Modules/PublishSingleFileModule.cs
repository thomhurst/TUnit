﻿using System.Runtime.InteropServices;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<PublishAOTModule>]
public class PublishSingleFileModule : Module<CommandResult>
{
    protected override Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        return Task.FromResult<SkipDecision>(EnvironmentVariables.IsNetFramework);
    }

    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var testProject = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();

        return await context.DotNet().Publish(new DotNetPublishOptions(testProject)
        {
            RuntimeIdentifier = GetRuntimeIdentifier(),
            Configuration = Configuration.Release,
            OutputDirectory = "TESTPROJECT_SINGLEFILE",
            Properties = [new KeyValue("SingleFile", "true")],
            Framework = "net8.0",
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);
    }

    private string GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-arm64";
        }

        throw new ArgumentException("Unknown platform");
    }
}
