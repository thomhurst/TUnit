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

public class ExplicitMethodInstanceSourceTests
{
    public IEnumerable<int> Rows() => [1];
    [Test]
    [GenerateGenericTest(typeof(long))]
    [InstanceMethodDataSource(nameof(Rows))]
    public void Explicit<T>(T item)
    {
        if (typeof(T) != typeof(long)) throw new InvalidOperationException("Expected explicit long");
    }
}

// The analyzer cannot close TClass before comparing the static row with T?.
#pragma warning disable TUnit0001
[GenerateGenericTest(typeof(int))]
public class ExplicitClassMethodSourceCollisionTests<TClass>
{
    public static IEnumerable<TClass> StaticRows() => [default!];
    public IEnumerable<int> InstanceRows() => [1];

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void BothSources<T>(T? item) where T : struct
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected explicit int");
    }
}

[Arguments<int>(1)]
public class ClassRowMethodSourceCollisionTests<TClass>(TClass value)
{
    public static IEnumerable<TClass> StaticRows() => [default!];
    public IEnumerable<int> InstanceRows()
    {
        _ = value;
        return [1];
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void BothSources<T>(T? item) where T : struct
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected explicit int");
    }
}

#pragma warning restore TUnit0001

// Factory tuples exercise the static inference path in reflection discovery too.
public class ExplicitFactoryTupleInstanceCollisionTests
{
    public static IEnumerable<Func<(int, string)>> StaticRows() => [() => (1, "static")];
    public IEnumerable<Func<(int, string)>> InstanceRows() => [() => (2, "instance")];

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void StaticFirst<T>(T item, string label)
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected explicit int");
        if (label != (Equals(item, 1) ? "static" : "instance")) throw new InvalidOperationException("Unexpected row");
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    [MethodDataSource(nameof(StaticRows))]
    public void InstanceFirst<T>(T item, string label)
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected explicit int");
        if (label != (Equals(item, 1) ? "static" : "instance")) throw new InvalidOperationException("Unexpected row");
    }
}
