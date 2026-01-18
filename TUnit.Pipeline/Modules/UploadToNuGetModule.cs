using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<TestNugetPackageModule>]
//[DependsOn<TestFSharpNugetPackageModule>]
//[DependsOn<TestVBNugetPackageModule>]
public class UploadToNuGetModule(IOptions<NuGetOptions> options) : Module<CommandResult[]>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(_ =>
        {
            if (!options.Value.ShouldPublish)
            {
                return SkipDecision.Skip("Should Publish is false");
            }

            if (string.IsNullOrEmpty(options.Value.ApiKey))
            {
                return SkipDecision.Skip("No API key found");
            }

            return SkipDecision.DoNotSkip;
        })
        .Build();

    protected override async Task<CommandResult[]?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var nupkgs = context.Git().RootDirectory
            .GetFiles(x => x.NameWithoutExtension.Contains("TUnit") && x.Extension is ".nupkg");

        return await nupkgs.SelectAsync(file =>
                context.DotNet().Nuget.Push(new DotNetNugetPushOptions
                {
                    Path = file.Path,
                    Source = "https://api.nuget.org/v3/index.json",
                    ApiKey = options.Value.ApiKey,
                }, new CommandExecutionOptions
                {
                    LogSettings = new CommandLoggingOptions
                    {
                        ShowCommandArguments = true,
                        ShowStandardError = true,
                        ShowExecutionTime = true,
                        ShowExitCode = true
                    }
                }, cancellationToken), cancellationToken: cancellationToken)
            .ProcessOneAtATime();
    }
}
