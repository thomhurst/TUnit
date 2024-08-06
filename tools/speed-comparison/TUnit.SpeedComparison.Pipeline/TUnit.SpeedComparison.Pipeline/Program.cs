using ModularPipelines.Host;
using TUnit.SpeedComparison.Pipeline;

await PipelineHostBuilder.Create()
    .AddModule<FindProjectsModule>()
    .AddModule<xUnitModule>()
    .AddModule<NUnitModule>()
    .AddModule<MSTestModule>()
    .AddModule<TUnitModule>()
    .ExecutePipelineAsync();