﻿using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using TUnit.Pipeline.Modules.Tests;

namespace TUnit.Pipeline.Modules;

[DependsOn<GetPackageProjectsModule>]
[DependsOn<GenerateVersionModule>]
[DependsOnAllModulesInheritingFrom<TestModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        var versionResult = await GetModule<GenerateVersionModule>();

        var version = versionResult.Value!;

        // TODO: Full version
        var packageVersion = version.NuGetVersionV2;

        if (context.Git().Information.BranchName == "main")
        {
            packageVersion += "-alpha01";
        }

        await projects.Value!.SelectAsync(
            async project =>
            {
                return await context.DotNet()
                    .Pack(
                        new DotNetPackOptions(project)
                        {
                            Properties = new[]
                            {
                                new KeyValue("Version", version.SemVer!),
                                new KeyValue("PackageVersion", packageVersion!)
                            },
                            IncludeSource = !project.Name.Contains("Generator"),
                        }, cancellationToken);
            }, cancellationToken: cancellationToken).ProcessOneAtATime();
        
        return projects.Value!.Select(x => new PackedProject(x.NameWithoutExtension, version.SemVer!)).ToList();
    }
}