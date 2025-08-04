using System.Collections.Generic;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public record Foo<T>(T Value)
{
    public static implicit operator Foo<T>(T value) => new(value);
}

[EngineTest(ExpectedResult.Pass)]
public class GenericImplicitConverterTests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test1(Foo<int> data)
    {
        Console.WriteLine($"Data: {data.Value}");
    }

    public static IEnumerable<Foo<int>> Data() => [new(1), new(2), new(3)];
}