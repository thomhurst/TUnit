using ModularPipelines.Interfaces;

namespace TUnit.Pipeline.Modules;

public class ProcessorParallelLimit : IParallelLimit
{
    public int Limit { get; } = Environment.ProcessorCount * 8;
}