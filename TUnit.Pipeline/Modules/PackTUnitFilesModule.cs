using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Models;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;
public class GenerateVersionModule : Module<string>
{
    protected override async Task<string?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var git = await context.Git().Versioning.GetGitVersioningInformation();
        return git.SemVer;
    }
}

[DependsOn<GetPackageProjectsModule>]
[DependsOn<RunTUnitEngineTestsModule>]
[DependsOn<GenerateVersionModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        var versionResult = await GetModule<GenerateVersionModule>();
        
        var version = versionResult.Value!;
        
        var packageVersion = version;
        
        if (context.Git().Information.BranchName == "main")
        {
            packageVersion += "-alpha01";
        }

        await projects.Value!.SelectAsync(async project =>
        {
            return await context.DotNet().Pack(new DotNetPackOptions(project) { Properties = new[] { new KeyValue("Version", version!), new KeyValue("PackageVersion", packageVersion!) }, IncludeSource = true, }, cancellationToken);
        }, cancellationToken: cancellationToken).ProcessOneAtATime();
        return projects.Value!.Select(x => new PackedProject(x.NameWithoutExtension, version!)).ToList();
    }
}