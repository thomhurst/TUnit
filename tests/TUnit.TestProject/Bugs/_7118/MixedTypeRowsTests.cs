using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7118;

// A generic class whose class-level [Arguments] rows infer different type arguments must produce the
// same tests in source-generated and reflection modes. A method-level row that infers the class's type
// arguments runs only on the instantiation those types make, never on one it cannot be converted to (#7118).

// The issue's shape: the method's own type parameter is independent of the class's, so every class row
// runs against every method row.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
public class MixedRowsGenericMethod<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test<TMethod>(TMethod arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<TMethod>();
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments).IsEquivalentTo(new object?[] { value });
    }
}

// The method's parameter uses the class's type parameter, so each method row pairs only with the class
// row of its own type: MixedRowsClassTypeMethod<int>.Test(1) and MixedRowsClassTypeMethod<string>.Test("a").
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
public class MixedRowsClassTypeMethod<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsEqualTo(value);
    }
}

// Several class and method rows per type: every class row of a type runs against every method row of it.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
[Arguments(2)]
public class MixedRowsSeveralRowsPerType<T>(T value)
{
    [Test]
    [Arguments(10)]
    [Arguments("b")]
    [Arguments(20)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<T>();
    }
}

// [Arguments(null)] infers no type, so it runs on every class row's instantiation.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
public class MixedRowsWithUninferredMethodRow<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    [Arguments(null)]
    public async Task Test(T? arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg is null || arg is T).IsTrue();
    }
}

// The method row "a" infers string, but no class row makes a string instantiation, so it has no
// constructor arguments. It fails with an explanation instead of running against the int class row
// (reflection mode) or with its own values passed to the constructor (source-generated mode).
[EngineTest(ExpectedResult.Failure)]
[Arguments(1)]
public class MethodRowWithoutMatchingClassRow<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsEqualTo(value);
    }
}

// The class row "a" makes a string instantiation, but the method's only row infers int. No method row
// runs on the string instantiation, so it yields no test rather than one called without arguments.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
public class ClassRowWithoutMatchingMethodRow<T>(T value)
{
    [Test]
    [Arguments(1)]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsEqualTo(value);
    }
}

// [GenerateGenericTest] lists the int the class row already infers, so that instantiation stands for
// both and, with no int method row, yields no test. The string method row has no class row and fails.
[EngineTest(ExpectedResult.Failure)]
[Arguments(1)]
[GenerateGenericTest(typeof(int))]
public class GenerateGenericTestOfClassRowType<T>(T value)
{
    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsEqualTo(value);
    }
}

// [GenerateGenericTest] lists object, which no row infers, so every method row runs on that
// instantiation with the class row. The int method row runs on the class row's int instantiation, and
// the string method row, which no class row matches, fails.
[EngineTest(ExpectedResult.Failure)]
[Arguments(1)]
[GenerateGenericTest(typeof(object))]
public class GenerateGenericTestOfUninferredType<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(arg is int or string).IsTrue();
    }
}

// A class-level typed source supplies the constructor arguments of the instantiation its declared type
// matches, so the method row inferring string runs on it: Test(2) on <int> with the class row, and Test("a")
// on <string> with the typed source's row. A typed source of another type (int here) does not count.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[StringClassSource7118]
public class MethodRowMatchedByTypedClassSource<T>(T value)
{
    [Test]
    [Arguments(2)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        if (typeof(T) == typeof(string))
        {
            await Assert.That(value).IsEqualTo((T)(object)"s");
            await Assert.That(arg).IsEqualTo((T)(object)"a");
        }
        else
        {
            await Assert.That(value).IsEqualTo((T)(object)1);
            await Assert.That(arg).IsEqualTo((T)(object)2);
        }
    }
}

public class StringClassSource7118 : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "s";
    }
}

// The class row "a" makes a string instantiation that the method's only [Arguments] row does not infer.
// The method's [MethodDataSource] infers no type, so its rows still run on that instantiation, as on the
// int one: Test(1) and Test(null) on <int>, Test(null) on <string>. Its declared IEnumerable<object?> made
// source-generated mode add an object instantiation that no class row infers, with four more tests.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
public class ClassRowWithUninferredMethodSource<T>(T value)
{
    public static IEnumerable<object?> Rows() => [null];

    [Test]
    [Arguments(1)]
    [MethodDataSource(nameof(Rows))]
    public async Task Test(T? arg)
    {
        await Assert.That((object?)value).IsNotNull();
        await Assert.That(arg is null || (object)arg is 0 or 1).IsTrue();
    }
}

// As MethodRowMatchedByTypedClassSource, with a method row that infers no type. The typed source joins the
// string instantiation, so [Arguments(null)] runs there as on the int one: Test(2) and Test(null) on <int>,
// Test("a") and Test(null) on <string>. Source-generated mode used to drop Test(null) on <string>.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[StringClassSource7118]
public class UninferredMethodRowWithTypedClassSource<T>(T value)
{
    [Test]
    [Arguments(2)]
    [Arguments("a")]
    [Arguments(null)]
    public async Task Test(T? arg)
    {
        if (typeof(T) == typeof(string))
        {
            await Assert.That((object?)value).IsEqualTo("s");
            await Assert.That(arg is null || (object)arg is "a").IsTrue();
        }
        else
        {
            // T? of an unconstrained T is T itself, so the null row reaches an int parameter as 0
            await Assert.That((object?)value).IsEqualTo(1);
            await Assert.That((object?)arg is 0 or 2).IsTrue();
        }
    }
}

// A class row [Arguments<string>(null)] declares string, so its null value makes a string instantiation, and
// the method row "a" runs on it: Test("a") on <string> with a null constructor value.
[EngineTest(ExpectedResult.Pass)]
[Arguments<string>(null)]
public class NullTypedClassRow<T>(T? value)
{
    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)value).IsNull();
        await Assert.That((object?)arg).IsEqualTo("a");
    }
}

// A class row [Arguments<string[]>(null)] binds its null value to the constructor's T[] parameter, so it infers T
// as string, not string[], and the method row "a" runs on <string> with a null constructor value.
[EngineTest(ExpectedResult.Pass)]
#pragma warning disable TUnit0001 // the analyzer does not map string[] through the T[] parameter
[Arguments<string[]>(null)]
#pragma warning restore TUnit0001
public class ArrayParameterNullTypedClassRow<T>(T[]? values)
{
    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values).IsNull();
        await Assert.That((object?)arg).IsEqualTo("a");
    }
}
