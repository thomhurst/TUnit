using ModularPipelines.Extensions;
using ModularPipelines.Host;

await PipelineHostBuilder.Create()
    .ConfigureServices((context, collection) =>
    {
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .ExecutePipelineAsync();