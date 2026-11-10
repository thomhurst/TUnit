using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules;

var builder = Pipeline.CreateBuilder(new PipelineBuilderSettings
{
    EnableCommandLineOptions = false,
    WorkingDirectory = Environment.CurrentDirectory,
});
builder.AddModule<GetPackageProjectsModule>()
    .AddModule<GitProbe>()
    .AddModule<DotNetProbe>()
    .AddModule<SkippedProbe>()
    .AddModule<SkipDependentProbe>()
    .AddModule<FailureProbe>()
    .AddModule<FailureDependentProbe>()
    .AddModule<IndependentProbe>();
builder.ConfigureOptions(options => options with
{
    FailureMode = FailureMode.ContinueOnFailure,
    // Keep independent work queued until the highest-priority failure is recorded.
    Concurrency = options.Concurrency with { MaxParallelism = 1 },
    ThrowOnPipelineFailure = false,
    Console = options.Console with { ShowProgress = false, PrintLogo = false },
});

var summary = await builder.RunAsync();
Require(summary.Results.Count == 8, "Every registered module must have a result.");
Require(!summary.Succeeded, "The intentional failure must fail the pipeline summary.");
Require(summary.Failures.Count == 2, "The failure and its dependent must be reported.");
Require(summary.GetResult<FailureProbe, string>().Status == ModuleStatus.Failed, "Failure status changed.");
Require(summary.GetResult<FailureDependentProbe, string>().Status == ModuleStatus.DependencyFailed, "Failed dependencies must not execute.");
Require(summary.GetResult<SkippedProbe, string>().Status == ModuleStatus.Skipped, "Skip status changed.");
Require(summary.GetResult<SkipDependentProbe, string>().Status == ModuleStatus.Skipped, "Skipped dependencies must propagate.");
Require(summary.GetResult<FailureProbe, string>().EndTime <= summary.GetResult<IndependentProbe, string>().StartTime, "Independent work must start after the intentional failure.");
Require(summary.GetResult<IndependentProbe, string>().ValueOrDefault == "completed", "ContinueOnFailure must finish independent modules.");
Require(summary.GetResult<GitProbe, string>().ValueOrDefault is not null, "Git information failed.");
Require(summary.GetResult<DotNetProbe, CommandResult>().ValueOrDefault?.ExitCode == 0, "Generated DotNet command failed.");
var projects = summary.GetResult<GetPackageProjectsModule, List<FilePath>>().ValueOrDefault;
Require(projects is { Count: > 0 } && projects.All(project => System.IO.File.Exists(project.Path)), "Consumer project paths must exist.");
Console.WriteLine("TUnit v4 migration validation passed: consumer paths, Git, DotNet, ordering, skips, failures, logging and summaries.");
return 0;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

public class GitProbe : Module<string>
{
    protected override async Task<string> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var information = await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken);
        context.Logger.LogInformation("Consumer repository root: {Root}", information.Root.Path);
        return information.Root.Path;
    }
}

[DependsOn<GitProbe>]
public class DotNetProbe : Module<CommandResult>
{
    protected override async Task<CommandResult> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var git = await context.GetModule<GitProbe>();
        if (git.ValueOrDefault is null)
        {
            throw new InvalidOperationException("Git must finish before DotNet.");
        }

        return await context.Tools.DotNet.Tool.ListAsync(new DotNetToolListOptions { Global = true }, cancellationToken: cancellationToken);
    }
}

public class SkippedProbe : SyncModule<string>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module.WithSkipWhen(_ => SkipDecision.Skip("Migration skip probe"));
    protected override string Execute(IModuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Skipped module executed.");
}

[DependsOn<SkippedProbe>]
public class SkipDependentProbe : SyncModule<string>
{
    protected override string Execute(IModuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Skipped dependent executed.");
}

public class FailureProbe : SyncModule<string>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module.WithPriority(ModulePriority.Critical);
    protected override string Execute(IModuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Intentional migration failure probe");
}

[DependsOn<FailureProbe>]
public class FailureDependentProbe : SyncModule<string>
{
    protected override string Execute(IModuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Failed dependent executed.");
}

public class IndependentProbe : SyncModule<string>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module.WithPriority(ModulePriority.Low);

    protected override string Execute(IModuleContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("Independent module completes in ContinueOnFailure mode.");
        return "completed";
    }
}
