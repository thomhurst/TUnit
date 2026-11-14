using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines;
using ModularPipelines.Options;
using TUnit.Pipeline;

var categoryOption = new Option<string[]>(
    name: "--categories")
{
    Arity = ArgumentArity.ZeroOrMore,
    AllowMultipleArgumentsPerToken = true,
    Description = "The categories to run.",
    DefaultValueFactory = _ => []
};

var ignoreCategoryOption = new Option<string[]>(
    name: "--ignore-categories")
{
    Arity = ArgumentArity.ZeroOrMore,
    AllowMultipleArgumentsPerToken = true,
    Description = "Categories to skip. Cannot be combined with --categories.",
    DefaultValueFactory = _ => []
};

var osSpecificOnlyOption = new Option<bool>(
    name: "--os-specific-only")
{
    Description = "Run only what can differ by operating system: PlatformNeutral suites keep just their .NET Framework TFMs, which Ubuntu cannot run.",
};

var rootCommand = new RootCommand("The pipeline for building, testing and packaging TUnit");
rootCommand.Add(categoryOption);
rootCommand.Add(ignoreCategoryOption);
rootCommand.Add(osSpecificOnlyOption);
rootCommand.Validators.Add(result =>
{
    if (result.GetValue(categoryOption) is { Length: > 0 } && result.GetValue(ignoreCategoryOption) is { Length: > 0 })
    {
        result.AddError("--ignore-categories cannot be combined with --categories.");
    }
});
rootCommand.SetAction(async parseResult =>
{
    var categories = parseResult.GetValue(categoryOption)!;
    var ignoreCategories = parseResult.GetValue(ignoreCategoryOption)!;
    PipelineScope.OsSpecificOnly = parseResult.GetValue(osSpecificOnlyOption);

    var builder = Pipeline.CreateBuilder(new PipelineBuilderSettings
    {
        EnableCommandLineOptions = false,
        WorkingDirectory = Environment.CurrentDirectory,
    });
    builder.Configuration.AddEnvironmentVariables();
    builder.Services.Configure<NuGetOptions>(builder.Configuration.GetSection("NuGet"));
    builder.AddModulesFromAssembly(typeof(Program).Assembly);
    builder.ConfigureOptions(options => options with
    {
        FailureMode = FailureMode.ContinueOnFailure,
        ThrowOnPipelineFailure = true,
    });

    if (categories.Length > 0)
    {
        builder.ConfigureOptions(options => options with { RunOnlyCategories = categories });
    }
    else
    {
        // Off Windows no PlatformNeutral suite has an OS-specific TFM left, so skip the modules outright.
        var platformNeutral = PipelineScope.OsSpecificOnly && !OperatingSystem.IsWindows()
            ? new[] { PipelineCategories.PlatformNeutral }
            : [];
        builder.ConfigureOptions(options => options with { IgnoreCategories = [.. ignoreCategories, .. platformNeutral] });
    }

    await builder.RunAsync();
});

return await rootCommand.Parse(args).InvokeAsync();
