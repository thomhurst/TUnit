namespace TUnit.TestProject.Bugs._7222;

[Arguments<int>(1)]
public class GenericInstanceSourceTests<TClass>(TClass value)
{
    public IEnumerable<string> Rows()
    {
        yield return value!.ToString()!;
        yield return "b";
    }

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    [InstanceMethodDataSource(nameof(MoreRows))]
    public void Valid<T>(T item) where T : class
    {
    }

    public IEnumerable<string> MoreRows() => ["extra"];

    public IEnumerable<Tuple<int, string>> TupleRows() => [Tuple.Create(1, "tuple")];

    [Test]
    [InstanceMethodDataSource(nameof(TupleRows))]
    public void ReferenceTuple<T>(T first, string second)
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected int");
    }

    [Test]
    [InstanceMethodDataSource(nameof(TupleRows))]
    public void PreservedTuple<T>(Tuple<T, string> row)
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected int");
    }

    // Intentional failures: checked in both modes by Issue7222Tests.
    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void StructOnly<T>(T item) where T : struct
    {
    }
}

[Arguments(1)]
public class NonGenericInstanceSourceTests(int value)
{
    public IEnumerable<int> Rows() => [value, 2];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void Valid<T>(T item) where T : struct
    {
    }
}

public class WholeReferenceTupleSourceTests
{
    public static IEnumerable<Tuple<int, string>> Rows() => [Tuple.Create(1, "tuple")];

    [Test]
    [GenerateGenericTest(typeof(Tuple<int, string>))]
    // #7622: the analyzer spreads the row before substituting the explicit generic type.
#pragma warning disable TUnit0001
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public void WholeRow<T>(T row)
    {
        if (row is not Tuple<int, string> { Item1: 1, Item2: "tuple" })
            throw new InvalidOperationException("Expected the whole reference tuple");
    }
}

[GenerateGenericTest(typeof(int))]
public class ExplicitClassInstanceSourcesTests<TClass>
{
    public IEnumerable<string> Rows() => ["first"];
    public IEnumerable<string> MoreRows() => ["second"];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    [InstanceMethodDataSource(nameof(MoreRows))]
    public void BothSources<T>(T item) where T : class
    {
        if (item is not string) throw new InvalidOperationException("Expected string");
    }
}

public class InferredReferenceTupleSourceTests
{
    public IEnumerable<Tuple<int>> InstanceRows() => [Tuple.Create(1)];
    public static IEnumerable<Tuple<int>> StaticRows() => [Tuple.Create(2)];

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void InstanceElement<T>(T value) where T : struct
    {
        if (value is not int) throw new InvalidOperationException("Expected int");
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    public void StaticElement<T>(T value) where T : struct
    {
        if (value is not int) throw new InvalidOperationException("Expected int");
    }
}

[Arguments<int>(1)]
public class MixedClassRowSourcesTests<TClass>(TClass value)
{
    public IEnumerable<string> InstanceRows() => [value!.ToString()!];
    public static IEnumerable<string> StaticRows() => ["static"];

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    [MethodDataSource(nameof(StaticRows))]
    public void InstanceFirst<T>(T item) where T : class
    {
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void StaticFirst<T>(T item) where T : class
    {
    }
}

[GenerateGenericTest(typeof(int))]
public class MixedExplicitClassSourcesTests<TClass>
{
    public IEnumerable<string> InstanceRows() => ["instance"];
    public static IEnumerable<string> StaticRows() => ["static"];

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    [MethodDataSource(nameof(StaticRows))]
    public void InstanceFirst<T>(T item) where T : class
    {
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void StaticFirst<T>(T item) where T : class
    {
    }
}

// #7622: the analyzer spreads reference tuples instead of binding the whole first parameter.
#pragma warning disable TUnit0001
public class WholeTupleWithTrailingParametersTests
{
    public static IEnumerable<Tuple<int, string>> StaticRows() => [Tuple.Create(1, "tuple")];
    public IEnumerable<Tuple<int, string>> InstanceRows() => StaticRows();

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void Optional<T>(Tuple<T, string> row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole tuple and default value");
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void Params<T>(Tuple<T, string> row, params int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole tuple and empty params");
    }


}

#pragma warning restore TUnit0001
