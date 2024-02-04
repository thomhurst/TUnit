using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModularPipelines.Extensions;
using ModularPipelines.Host;

await PipelineHostBuilder.Create()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((_, collection) =>
    {
        _.Configuration.GetSection("NuGet").Bind(new NuGetOptions());
        collection.AddModulesFromAssembly(typeof(Program).Assembly);
    })
    .SetLogLevel(LogLevel.Debug)
    .ExecutePipelineAsync();