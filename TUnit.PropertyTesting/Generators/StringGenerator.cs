using TUnit.Core.Interfaces;

namespace TUnit.PropertyTesting.Generators;

/// <summary>
/// Default generator for string values
/// </summary>
public class StringGenerator : IGenerator<string>
{
    private readonly int _minLength;
    private readonly int _maxLength;

    public StringGenerator() : this(0, 20)
    {
    }

    public StringGenerator(int minLength, int maxLength)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    public string Generate(Random random)
    {
        var length = random.Next(_minLength, _maxLength + 1);
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)random.Next('a', 'z' + 1);
        }

        return new string(chars);
    }
}
