using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Attributes;

namespace TUnit.Pipeline.Modules;
[DependsOn<GetPackageProjectsModule>]
[DependsOn<CleanProjectsModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        
        var guid = Guid.NewGuid();
        var version = $"0.0.1-alpha{guid}";

        var packedProjects = await projects.Value!.SelectAsync(async project =>
            {
                return await context.DotNet()
                    .Pack(
                        new DotNetPackOptions(project)
                        {
                            Properties = new[]
                                { new KeyValue("Version", version), new KeyValue("PackageVersion", version) }
                        }, cancellationToken);
            }, cancellationToken: cancellationToken)
            .ProcessOneAtATime();

        return projects.Value!.Select(x => new PackedProject(x.NameWithoutExtension, version)).ToList();
    }
}