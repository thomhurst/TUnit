using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class IntAssertions
{
    [GenerateAssertion]
    public static bool IsEven(this int value)
    {
        return value % 2 == 0;
    }

    [GenerateAssertion]
    public static bool IsOdd(this int value)
    {
        return value % 2 != 0;
    }
}
