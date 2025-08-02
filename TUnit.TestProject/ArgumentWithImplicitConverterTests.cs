using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ArgumentWithImplicitConverterTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public void Explicit(ExplicitInteger integer)
    {
        Console.WriteLine(integer);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public void Implicit(ImplicitInteger integer)
    {
        Console.WriteLine(integer);
    }
}

public readonly struct ExplicitInteger(int i)
{
    public static explicit operator ExplicitInteger(int i) => new(i);

    public override string ToString()
    {
        return i.ToString();
    }
}


public readonly struct ImplicitInteger(int i)
{
    public static implicit operator ImplicitInteger(int i) => new(i);

    public override string ToString()
    {
        return i.ToString();
    }
}
