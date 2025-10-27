using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Generators;

/// <summary>
/// Default generator for long values
/// </summary>
public class LongGenerator : IGenerator<long>
{
    private readonly long _min;
    private readonly long _max;

    public LongGenerator() : this(long.MinValue / 2, long.MaxValue / 2)
    {
    }

    public LongGenerator(long min, long max)
    {
        _min = min;
        _max = max;
    }

    public long Generate(Random random)
    {
        var range = (ulong)(_max - _min);
        var sample = (long)(random.NextDouble() * range);
        return _min + sample;
    }
}
