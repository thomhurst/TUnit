﻿using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests")]
public class RunSourceGeneratorTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Engine.SourceGenerator.Tests.csproj").AssertExists();
        
        return await context.DotNet().Test(new DotNetTestOptions(project)
        {
            NoBuild = true,
            Configuration = Configuration.Release,
            Framework = "net8.0"
        }, cancellationToken);
    }
}