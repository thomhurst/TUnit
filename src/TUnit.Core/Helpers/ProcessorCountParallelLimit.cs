using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

public class ProcessorCountParallelLimit : IParallelLimit
{
    public int Limit { get; } = Environment.ProcessorCount;
}
