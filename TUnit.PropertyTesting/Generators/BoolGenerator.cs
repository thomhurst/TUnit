using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Generators;

/// <summary>
/// Default generator for boolean values
/// </summary>
public class BoolGenerator : IGenerator<bool>
{
    public bool Generate(Random random)
    {
        return random.Next(2) == 0;
    }
}
