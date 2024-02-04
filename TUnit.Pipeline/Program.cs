using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularPipelines.Extensions;
using ModularPipelines.Host;

await PipelineHostBuilder.Create()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, collection) =>
    {
        collection.Configure<NuGetOptions>(context.Configuration.GetSection("NuGet"));
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .SetLogLevel(LogLevel.Debug)
    .ExecutePipelineAsync();