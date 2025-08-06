using System.Collections.Generic;
using TUnit.Core;

namespace TUnit.Core.SourceGenerator.Tests;

public record Foo
{
    public static implicit operator Foo((int Value1, int Value2) tuple) => new();
}

public class TupleImplicitOperatorBugTest
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test1(Foo data)
    {
    }

    public static IEnumerable<Foo> Data() => [new()];
}