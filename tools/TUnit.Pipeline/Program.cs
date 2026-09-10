using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines;
using ModularPipelines.Extensions;
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

var rootCommand = new RootCommand("The pipeline for building, testing and packaging TUnit");
rootCommand.Add(categoryOption);
rootCommand.SetAction(async parseResult =>
{
    var categories = parseResult.GetValue(categoryOption)!;

    var builder = Pipeline.CreateBuilder();
    builder.Configuration.AddEnvironmentVariables();
    builder.Services.Configure<NuGetOptions>(builder.Configuration.GetSection("NuGet"));
    builder.Services.AddModulesFromAssembly(typeof(Program).Assembly);
    builder.Options.ExecutionMode = ExecutionMode.WaitForAllModules;

    if (categories.Length > 0)
    {
        builder.RunCategories(categories);
    }
    else
    {
        builder.IgnoreCategories("ReadMe");
    }

    await builder.Build().RunAsync();
});

return await rootCommand.Parse(args).InvokeAsync();
