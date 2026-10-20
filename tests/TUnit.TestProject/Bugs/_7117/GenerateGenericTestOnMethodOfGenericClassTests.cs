using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7117;

// [GenerateGenericTest] on a generic method of a generic class closes the method on every class the
// class sources produce, in both modes (#7117).

// The issue's shape: the class row and the method row infer int, which [GenerateGenericTest] also lists.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class MethodGenerateGenericTestOnClassRow<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(arg).IsEqualTo((TMethod)(object)2);
    }
}

// Each class row closes the class; the method's [GenerateGenericTest] type runs on each of them.
[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
[Arguments(2)]
public class MethodGenerateGenericTestWithClassRows<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(bool))]
    public async Task Run<TMethod>()
    {
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(bool));
        await Assert.That(value is "a" or 2).IsTrue();
        await Assert.That(typeof(T)).IsEqualTo(value!.GetType());
    }
}

// A class-level [GenerateGenericTest] with a method row that infers the method's type argument.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class ClassGenerateGenericTestWithMethodRow<T>
{
    [Test]
    [Arguments(1)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That(arg).IsEqualTo((TMethod)(object)1);
    }
}

// A [GenerateGenericTest] type that no method row infers runs every method row, as on a non-generic
// class: Run<int>(2) from the row and Run<long>(2) from the attribute.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class MethodGenerateGenericTestTypeNotInferredByRow<T>(T value)
{
    [Test]
    [GenerateGenericTest(typeof(long))]
    [Arguments(2)]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(typeof(TMethod) == typeof(int) || typeof(TMethod) == typeof(long)).IsTrue();
        await Assert.That(Convert.ToInt64(arg)).IsEqualTo(2L);
    }
}

// A method data source that infers the method's type argument runs on each class a class row closes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class MethodDataSourceOnClassRow<T>(T value)
{
    public static IEnumerable<Func<(string, int)>> Rows()
    {
        yield return () => ("a", 1);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Run<TMethod>(TMethod arg, int count)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(string));
        await Assert.That(arg).IsEqualTo((TMethod)(object)"a");
    }
}

// The same, on each class a class-level [GenerateGenericTest] closes.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(long))]
public class MethodDataSourceOnClassGenerateGenericTest<T>
{
    public static IEnumerable<Func<(string, int)>> Rows()
    {
        yield return () => ("a", 1);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Run<TMethod>(TMethod arg, int count)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(long));
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(string));
        await Assert.That(arg).IsEqualTo((TMethod)(object)"a");
    }
}

// A method data source of the open class whose rows name the class type parameter infers the method
// type argument from the closed class: Run<long> on GenerateGenericTest's long, not Run<T>.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(long))]
public class ClassTypedMethodDataSourceOnClassGenerateGenericTest<T>
{
    public static IEnumerable<Func<(T, int)>> Rows()
    {
        yield return () => (default(T)!, 1);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Run<TMethod>(TMethod arg, int count)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(long));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(long));
        await Assert.That(count).IsEqualTo(1);
    }
}

// A typed data source infers no method type argument on a generic class, in either mode: it runs on the
// method [GenerateGenericTest] type, Run<long>(42), and adds no Run<int>.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class TypedDataSourceOnClassGenerateGenericTest<T>
{
    [Test]
    [GenerateGenericTest(typeof(long))]
    [IntSource7117]
    public async Task Run<TMethod>(TMethod arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(long));
        await Assert.That(Convert.ToInt64(arg)).IsEqualTo(42L);
    }
}

// A method data source whose inferred type breaks the method's constraint, paired with a class row:
// one failed test per row the source yields, as in reflection mode. No [EngineTest]: it fails on purpose.
[Arguments(1)]
public class MethodDataSourceConstraintViolationOnClassRow<T>(T value)
{
    public static IEnumerable<Func<(string, int)>> Rows()
    {
        yield return () => ("a", 1);
        yield return () => ("b", 2);
    }

    [Test]
#pragma warning disable TUnit0001 // the analyzer rejects string for TMethod; the test checks how discovery reports it
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task Run<TMethod>(TMethod arg, int count) where TMethod : struct
    {
        _ = value;
        await Assert.That(arg).IsNotEqualTo(default(TMethod));
    }
}

// IInfersType<T> whose type breaks the method's constraint, paired with a class row: one failed test
// per row of every method data source, as in reflection mode. No [EngineTest]: it fails on purpose.
[Arguments(1)]
public class MatrixConstraintViolationOnClassRow<T>(T value)
{
    [Test]
    [MatrixDataSource]
    public async Task Run<TMethod>([Matrix<string>("a", "b")] TMethod arg) where TMethod : struct
    {
        _ = value;
        await Assert.That(arg).IsNotEqualTo(default(TMethod));
    }
}

public class IntSource7117 : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => 42;
    }
}
