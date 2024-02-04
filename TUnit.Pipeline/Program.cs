using Microsoft.Extensions.Logging;
using ModularPipelines.Extensions;
using ModularPipelines.Host;

await PipelineHostBuilder.Create()
    .ConfigureServices((_, collection) =>
    {
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .SetLogLevel(LogLevel.Debug)
    .ExecutePipelineAsync();