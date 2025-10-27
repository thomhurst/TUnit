using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Generators;

/// <summary>
/// Default generator for integer values
/// </summary>
public class IntGenerator : IGenerator<int>
{
    private readonly int _min;
    private readonly int _max;

    public IntGenerator() : this(int.MinValue / 2, int.MaxValue / 2)
    {
    }

    public IntGenerator(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public int Generate(Random random)
    {
        return random.Next(_min, _max);
    }
}
