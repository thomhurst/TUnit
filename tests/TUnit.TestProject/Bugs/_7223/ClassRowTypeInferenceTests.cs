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

// A Type value infers System.Type, the attribute argument's declared type, in both modes: reflection used to take the
// value's private RuntimeType as T.
[EngineTest(ExpectedResult.Pass)]
[Arguments(typeof(int))]
public class TypeValueClassRowInference<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Type));
        await Assert.That((object?)value).IsEqualTo(typeof(int));
    }
}

// A null says nothing about T even when it is cast to an array type, so the later string infers T in both modes:
// source generation used to fix T as int from the null int[], so construction failed.
[EngineTest(ExpectedResult.Pass)]
[Arguments((int[])null!, "x")]
public class NullArrayBeforeValueClassRowInference<T>(T[]? values, T other)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values).IsNull();
        await Assert.That((object?)other).IsEqualTo("x");
    }
}

// The row infers T through the [TestConstructor] the instance is built with, not through the first constructor: the
// string[] binds to the bare T, so T is string[] in both modes. Inferring through the T[] constructor gave string, and
// the array then could not bind to the marked constructor's parameter.
[EngineTest(ExpectedResult.Pass)]
[Arguments<string[]>(["x"])]
public class TestConstructorClassRowInference<T>
{
    private readonly T _value;

    public TestConstructorClassRowInference(T[] values)
    {
        _value = values[0];
    }

    [TestConstructor]
    public TestConstructorClassRowInference(T value)
    {
        _value = value;
    }

    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string[]));
        await Assert.That(_value is string[] { Length: 1 } array && array[0] == "x").IsTrue();
    }
}

// A typed null row also infers T through the [TestConstructor]: the declared string[] binds to the bare T, so T is
// string[] in both modes. Reflection used to read the null through the first constructor's T[] and infer string. The
// first constructor takes two parameters so that building the instance from a null argument is not ambiguous (#7365).
// The analyzer still checks the row against the first constructor (#7377).
#pragma warning disable TUnit0001
[EngineTest(ExpectedResult.Pass)]
[Arguments<string[]>(null!)]
public class TestConstructorNullTypedClassRowInference<T>
#pragma warning restore TUnit0001
{
    private readonly T? _value;

    public TestConstructorNullTypedClassRowInference(T[] values, int index)
    {
        _value = values[index];
    }

    [TestConstructor]
    public TestConstructorNullTypedClassRowInference(T? value)
    {
        _value = value;
    }

    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string[]));
        await Assert.That(_value).IsNull();
    }
}

// With several public constructors and no [TestConstructor], the row infers T through the first declared one, the
// constructor both modes build the instance with when the constructors take the same number of parameters: the
// string[] binds to T[], so T is string. Constructors with different parameter counts are #7365.
[EngineTest(ExpectedResult.Pass)]
[Arguments<string[]>(["x"])]
public class MultipleConstructorsClassRowInference<T>
{
    private readonly T _value;

    public MultipleConstructorsClassRowInference(T[] values)
    {
        _value = values[0];
    }

    public MultipleConstructorsClassRowInference(T value)
    {
        _value = value;
    }

    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(_value is "x").IsTrue();
    }
}
