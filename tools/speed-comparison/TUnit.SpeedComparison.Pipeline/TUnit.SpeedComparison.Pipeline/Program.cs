using ModularPipelines.Host;
using TUnit.SpeedComparison.Pipeline;

await PipelineHostBuilder.Create()
    .AddModule<xUnitModule>()
    .AddModule<NUnitModule>()
    .AddModule<TUnitModule>()
    .ExecutePipelineAsync();