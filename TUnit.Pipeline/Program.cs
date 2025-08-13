using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
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
rootCommand.SetAction(parseResult =>
{
    var categories = parseResult.GetValue(categoryOption)!;

    var pipelineHostBuilder = PipelineHostBuilder.Create()
        .ConfigureAppConfiguration((_, builder) =>
        {
            builder.AddEnvironmentVariables();
        })
        .ConfigureServices((context, collection) =>
        {
            collection.Configure<NuGetOptions>(context.Configuration.GetSection("NuGet"));
            collection.AddModulesFromAssembly(typeof(Program).Assembly);
        })
        .ConfigurePipelineOptions((_, options) => options.ExecutionMode = ExecutionMode.WaitForAllModules);

    if (categories.Length > 0)
    {
        pipelineHostBuilder.RunCategories(categories);
    }
    else
    {
        pipelineHostBuilder.IgnoreCategories("ReadMe");
    }

    pipelineHostBuilder
        .ExecutePipelineAsync()
        .GetAwaiter()
        .GetResult();
});

return await rootCommand.Parse(args).InvokeAsync();
