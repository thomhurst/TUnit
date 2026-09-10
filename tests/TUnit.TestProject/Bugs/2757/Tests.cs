using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2757;

public record Foo<T>(T Value)
{
    public static implicit operator Foo<T>(T value) => new(value);
}

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test1(Foo<int> data)
    {
        Console.WriteLine(data.Value);
    }

    public static IEnumerable<Foo<int>> Data() => [new(1)];
}
