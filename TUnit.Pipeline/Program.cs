using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using TUnit.Pipeline;
using TUnit.Pipeline.Modules;

var pipelineSummary = await PipelineHostBuilder.Create()
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

var versionResult = await pipelineSummary.Modules.GetModule<GenerateVersionModule>();

#pragma warning disable ConsoleUse
Console.WriteLine($"NuGet Version is: {versionResult.Value!.SemVer}");
#pragma warning restore ConsoleUse
