using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7223;

// A non-null class row infers T through a T[] constructor parameter as its element type, as a null row of the same
// declared type does (#7149), so the method row runs on ArrayParameterClassRowInference<string>.
[EngineTest(ExpectedResult.Pass)]
[Arguments<string[]>(["x"])]
public class ArrayParameterClassRowInference<T>(T[] values)
{
    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values is ["x"]).IsTrue();
        await Assert.That((object?)arg).IsEqualTo("a");
    }
}

// A null row and a non-null row of the same declared type infer the same T, so both class rows join one
// instantiation and the method row runs once per class row.
[EngineTest(ExpectedResult.Pass)]
[Arguments<string[]>(["x"])]
[Arguments<string[]>(null!)]
public class MixedNullAndValueArrayClassRows<T>(T[]? values)
{
    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values is null || values is ["x"]).IsTrue();
        await Assert.That((object?)arg).IsEqualTo("a");
    }
}

// T is inferred from the value bound to the T[] parameter, not from the first value of the row: reflection used to
// take the int as T.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1, new[] { "x" })]
public class ArrayParameterAfterOtherClassRowInference<T>(int count, T[] values)
{
    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That((object?)arg).IsEqualTo("a");
    }
}

// An untyped null says nothing about T, so the later value infers it in both modes: source generation used to take
// the null's object type as T once a bare T was inferred from the first value only.
[EngineTest(ExpectedResult.Pass)]
[Arguments(null, "x")]
public class NullBeforeValueClassRowInference<T>(T? first, T second) where T : class, IComparable
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(first).IsNull();
        await Assert.That((object)second).IsEqualTo("x");
    }
}

// The int[] fixes T as int, because it cannot bind to a short[]; the short after it binds to the int parameter. A
// later bare T used to replace the array's type in source generation, so construction failed.
[EngineTest(ExpectedResult.Pass)]
[Arguments(new[] { 1 }, (short)2)]
public class ArrayBeforeBareClassRowInference<T>(T[] values, T count)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That((object?)count).IsEqualTo(2);
    }
}

// The same row with the bare T first: the array still fixes T as int, which the short binds to.
[EngineTest(ExpectedResult.Pass)]
[Arguments((short)2, new[] { 1 })]
public class BareBeforeArrayClassRowInference<T>(T count, T[] values)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)count).IsEqualTo(2);
        await Assert.That(values.Length).IsEqualTo(1);
    }
}

// A bare T that only untyped nulls bind runs on object in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(null, null)]
public class AllNullClassRowInference<T>(T? first, T? second)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That((object?)first).IsNull();
        await Assert.That((object?)second).IsNull();
    }
}
