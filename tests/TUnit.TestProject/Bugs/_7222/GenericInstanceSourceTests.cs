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
