using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

public class DefaultParallelLimit : IParallelLimit
{
    public int Limit { get; } = Environment.ProcessorCount;
}