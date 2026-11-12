using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

// Runs in aot.yml with the Packaging modules, so its native AOT publishes don't compete with the
// test matrix for CPUs. The test module shares the NugetTester project's bin and obj folders, so a
// local run that registers both still runs them in turn; aot.yml doesn't register it.
[ModuleCategory(PipelineCategories.Aot)]
[DependsOn<TestNugetPackageModule>(Optional = true)]
[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class PublishNugetTesterAOTModule : Module<IReadOnlyList<CommandResult>>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithSkipWhen(_ => EnvironmentVariables.IsNetFramework
            ? SkipDecision.Skip("Running on .NET Framework")
            : SkipDecision.DoNotSkip);

    protected override async Task<IReadOnlyList<CommandResult>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var version = await context.GetModule<GenerateVersionModule>();

        var testProject = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.NugetTester.csproj")
            .AssertExists();

        // Test AOT publishing
        foreach (var framework in new[] { "net8.0", "net10.0", "net11.0" })
        {
            var result = await context.RunSubModuleAsync<CommandResult>($"AOT-{framework}", async token =>
            {
                return await context.Tools.DotNet.PublishAsync(new DotNetPublishOptions
                {
                    ProjectSolution = testProject.Path,
                    Runtime = RuntimeIdentifiers.Current,
                    Configuration = "Release",
                    Output = $"NUGETTESTER_AOT_{framework}",
                    Properties =
                    [
                        new KeyValue("Aot", "true"),
                        new KeyValue("TUnitVersion", version.ValueOrDefault!.SemVer!),
                    ],
                    Framework = framework,
                }, new CommandExecutionOptions
                {
                    Logging = new CommandLoggingOptions
                    {
                        ShowCommandArguments = true,
                        ShowStandardError = true,
                        ShowExecutionTime = true,
                        ShowExitCode = true
                    }
                }, token);
            }, cancellationToken);

            results.Add(result);
        }

        return results;
    }
}
