namespace TUnit.TestProject.Bugs._2798;

public record Foo
{
    public static implicit operator Foo((int Value1, int Value2) tuple) => new();
}

public class Tests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test1(Foo data)
    {
    }

    public static IEnumerable<Foo> Data() => [new()];
}
