using ModularPipelines.Interfaces;

namespace TUnit.Pipeline.Modules;

public class ProcessorParallelLimit : IParallelLimit
{
    public static int Limit { get; } = Environment.ProcessorCount * 4;
}
