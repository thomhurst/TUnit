using System.Collections.Generic;
using TUnit.Core;

namespace TUnit.Core.SourceGenerator.Tests;

public record Foo
{
    public static implicit operator Foo((int Value1, int Value2) tuple) => new();
}

public record Bar  
{
    public static implicit operator Bar((int A, string B, double C) tuple) => new();
}

public class TupleImplicitOperatorBugTest
{
    [Test]
    [MethodDataSource(nameof(Data2))]
    public void Test1(Foo data)
    {
    }

    [Test]
    [MethodDataSource(nameof(Data3))]
    public void Test2(Bar data)
    {
    }

    public static IEnumerable<(int, int)> Data2() => [(1, 2), (3, 4)];
    public static IEnumerable<(int, string, double)> Data3() => [(1, "test", 3.14), (2, "hello", 2.71)];
}