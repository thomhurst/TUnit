using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using TUnit.Pipeline;

await PipelineHostBuilder.Create()
    .ConfigureAppConfiguration((_, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, collection) =>
    {
        collection.Configure<NuGetOptions>(context.Configuration.GetSection("NuGet"));
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .ExecutePipelineAsync();