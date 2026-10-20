using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7164;

// A typed data source (DataSourceGeneratorAttribute<T>) infers the type arguments of a generic method of a
// generic class, as it does on a non-generic class, in both modes (#7164).

// A class-level [GenerateGenericTest] closes the class; the typed source closes the method: Run<int>(42).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class TypedSourceOnClassGenerateGenericTest<T>
{
    [Test]
    [IntSource7164]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(arg).IsEqualTo((TMethod)(object)42);
    }
}

// A method [GenerateGenericTest] type that the typed source does not infer runs every data source, as on a
// non-generic class: Run<int>(42) from the source and Run<long>(42) from the attribute.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class TypedSourceAndMethodGenerateGenericTest<T>
{
    [Test]
    [GenerateGenericTest(typeof(long))]
    [IntSource7164]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod) == typeof(int) || typeof(TMethod) == typeof(long)).IsTrue();
        await Assert.That(Convert.ToInt64(arg)).IsEqualTo(42L);
    }
}

// A method [GenerateGenericTest] type that the typed source also infers runs once: Run<int>(42).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class TypedSourceAndSameMethodGenerateGenericTest<T>
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [IntSource7164]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(arg).IsEqualTo((TMethod)(object)42);
    }
}

// A class row closes the class; the typed source closes the method: Run<int>(42).
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class TypedSourceOnClassRow<T>(T value)
{
    [Test]
    [IntSource7164]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(arg).IsEqualTo((TMethod)(object)42);
    }
}

// The same with a method [GenerateGenericTest] type the source does not infer: Run<int>(42) and Run<long>(42).
[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
public class ClassRowTypedSourceAndMethodGenerateGenericTest<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(long))]
    [IntSource7164]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"a");
        await Assert.That(typeof(TMethod) == typeof(int) || typeof(TMethod) == typeof(long)).IsTrue();
        await Assert.That(Convert.ToInt64(arg)).IsEqualTo(42L);
    }
}

// Two method type parameters inferred from the source's two type arguments: Run<int, string>(42, x).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(bool))]
public class TwoTypeArgumentTypedSourceOnGenericClass<T>
{
    [Test]
    [IntStringSource7164]
    public async Task Run<TFirst, TSecond>(TFirst first, TSecond second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(bool));
        await Assert.That(typeof(TFirst)).IsEqualTo(typeof(int));
        await Assert.That(typeof(TSecond)).IsEqualTo(typeof(string));
        await Assert.That(first).IsEqualTo((TFirst)(object)42);
        await Assert.That(second).IsEqualTo((TSecond)(object)"x");
    }
}

public class IntSource7164 : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => 42;
    }
}

public class IntStringSource7164 : DataSourceGeneratorAttribute<int, string>
{
    protected override IEnumerable<Func<(int, string)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => (42, "x");
    }
}
