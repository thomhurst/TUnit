using Microsoft.Extensions.Logging;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using ModularPipelines.Options;
using TUnit.Testing.Pipeline;

await PipelineHostBuilder.Create()
    .ConfigureServices((context, collection) =>
    {
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .AddRequirement<BuiltTestProjectRequirement>()
    .ConfigurePipelineOptions((_, options) =>
    {
        options.PrintResults = false;
        options.DefaultCommandLogging = CommandLogging.Input | CommandLogging.Error;
        options.ShowProgressInConsole = false;
        options.ExecutionMode = ExecutionMode.WaitForAllModules;
    })
    .ExecutePipelineAsync();