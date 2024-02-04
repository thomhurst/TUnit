using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Attributes;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules;
[DependsOn<GetPackageProjectsModule>]
[DependsOn<CleanProjectsModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();

        var git = await context.Git().Versioning.GetGitVersioningInformation();
                    
        var version = git.SemVer;

        if (git.BranchName == "main")
        {
            version += "alpha01";
        }

        await projects.Value!.SelectAsync(async project =>
            {
                return await context.DotNet()
                    .Pack(
                        new DotNetPackOptions(project)
                        {
                            Properties = new[]
                                { 
                                    new KeyValue("Version", version), 
                                    new KeyValue("PackageVersion", version) 
                                }
                        }, cancellationToken);
            }, cancellationToken: cancellationToken)
            .ProcessOneAtATime();

        return projects.Value!.Select(x => new PackedProject(x.NameWithoutExtension, version)).ToList();
    }
}