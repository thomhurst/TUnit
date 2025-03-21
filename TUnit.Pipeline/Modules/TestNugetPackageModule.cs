﻿using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class TestNugetPackageModule : Module<CommandResult[]>
{
    private readonly List<string> _frameworks = [
        /* TODO: Bug with:
        Unhandled exception. System.MissingMethodException: Method not found: 'Void 
        Microsoft.Testing.Platform.Extensions.Messages.TestNode.set_DisplayName(System.S
        tring)'.
        "net6.0", */ 
        "net8.0", "net9.0"];

    public TestNugetPackageModule()
    {
        if (EnvironmentVariables.IsNet472)
        {
            _frameworks.AddRange(["net462", "net472", "net481"]);
        }
    }

    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var version = await GetModule<GenerateVersionModule>();

        var project = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.NugetTester.csproj")
            .AssertExists();

        return await _frameworks.SelectAsync(framework =>
                SubModule(framework, () =>
                    context.DotNet().Run(new DotNetRunOptions
                    {
                        Project = project,
                        Framework = framework,
                        Properties =
                        [
                            new KeyValue("TUnitVersion", version.Value!.SemVer!)
                        ]
                    }, cancellationToken)
                )
            , cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}
