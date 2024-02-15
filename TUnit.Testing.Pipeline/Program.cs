using Microsoft.Extensions.Logging;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using TUnit.Testing.Pipeline;

await PipelineHostBuilder.Create()
    .ConfigureServices((context, collection) =>
    {
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .AddRequirement<BuiltTestProjectRequirement>()
    .SetLogLevel(LogLevel.Debug)
    .ExecutePipelineAsync();