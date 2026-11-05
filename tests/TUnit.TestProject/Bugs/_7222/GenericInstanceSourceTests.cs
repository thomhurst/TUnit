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
