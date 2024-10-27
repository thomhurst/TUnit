using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using ModularPipelines.Options;
using TUnit.Pipeline;

var fileOption = new Option<string[]>(
    name: "--categories",
    description: "The categories to run.", 
    getDefaultValue: () => [])
{
    Arity = ArgumentArity.ZeroOrMore,
    AllowMultipleArgumentsPerToken = true,
};

var rootCommand = new RootCommand("The pipeline for building, testing and packaging TUnit");
rootCommand.AddOption(fileOption);

rootCommand.SetHandler((categories) =>
    {
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
    },
    fileOption);

return await rootCommand.InvokeAsync(args);