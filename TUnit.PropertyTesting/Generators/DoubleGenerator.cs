using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Generators;

/// <summary>
/// Default generator for double values
/// </summary>
public class DoubleGenerator : IGenerator<double>
{
    private readonly double _min;
    private readonly double _max;

    public DoubleGenerator() : this(-1000000.0, 1000000.0)
    {
    }

    public DoubleGenerator(double min, double max)
    {
        _min = min;
        _max = max;
    }

    public double Generate(Random random)
    {
        return _min + (random.NextDouble() * (_max - _min));
    }
}
