using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using ModularPipelines.Options;
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
    .ConfigurePipelineOptions((_, options) => options.ExecutionMode = ExecutionMode.WaitForAllModules)
    .ExecutePipelineAsync();