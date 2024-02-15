using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;
[DependsOn<GetPackageProjectsModule>]
[DependsOn<RunTUnitEngineTestsModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();

        var git = await context.Git().Versioning.GetGitVersioningInformation();
                    
        var version = git.SemVer;
        var packageVersion = version;
        
        if (git.BranchName == "main")
        {
            packageVersion += "-alpha01";
        }

        await projects.Value!.SelectAsync(async project =>
            {
                return await context.DotNet()
                    .Pack(
                        new DotNetPackOptions(project)
                        {
                            Properties = new[]
                                { 
                                    new KeyValue("Version", version!), 
                                    new KeyValue("PackageVersion", packageVersion!) 
                                },
                            IncludeSource = true,
                        }, cancellationToken);
            }, cancellationToken: cancellationToken)
            .ProcessOneAtATime();

        return projects.Value!.Select(x => new PackedProject(x.NameWithoutExtension, version!)).ToList();
    }
}