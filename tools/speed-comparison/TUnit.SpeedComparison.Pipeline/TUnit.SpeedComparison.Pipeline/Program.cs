// See https://aka.ms/new-console-template for more information

using ModularPipelines.Host;
using TUnit.SpeedComparison.Pipeline;

await PipelineHostBuilder.Create()
    .AddModule<xUnitModule>()
    .AddModule<NUnitModule>()
    .AddModule<TUnitModule>()
    .ExecutePipelineAsync();