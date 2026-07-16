using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

public class GetPackageProjectsModule : Module<List<File>>
{
    protected override Task<List<File>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var sourceDirectory = Path.Combine(context.Git().RootDirectory.Path, "src");

        File SourceProject(string name) => new(Path.Combine(sourceDirectory, name, $"{name}.csproj"));

        return Task.FromResult<List<File>?>(
        [
            SourceProject("TUnit.Assertions"),
            SourceProject("TUnit.Assertions.Should"),
            SourceProject("TUnit.Assertions.FSharp"),
            SourceProject("TUnit.Core"),
            SourceProject("TUnit.Engine"),
            SourceProject("TUnit"),
            SourceProject("TUnit.Playwright"),
            SourceProject("TUnit.Templates"),
            SourceProject("TUnit.Logging.Microsoft"),
            SourceProject("TUnit.AspNetCore"),
            SourceProject("TUnit.AspNetCore.Core"),
            SourceProject("TUnit.Aspire"),
            SourceProject("TUnit.Aspire.Core"),
            SourceProject("TUnit.OpenTelemetry"),
            SourceProject("TUnit.FsCheck"),
            SourceProject("TUnit.Mocks"),
            SourceProject("TUnit.Mocks.Assertions"),
            SourceProject("TUnit.Mocks.Http"),
            SourceProject("TUnit.Mocks.Logging"),
            SourceProject("TUnit.Reporting.Tool")
        ]);
    }
}
