using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7209;

// The class row's value goes to an int constructor parameter, not to T, so it supplies no class type
// argument. T comes from the method rows, as it does without a class row: C<int> for [Arguments(1)]
// and C<object> for [Arguments(null)] (#7141). Each instance is constructed from the class row
// (number = 42), never from a method row. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class ClassRowInfersNoTypeArgument<T>(int number)
{
    [Test]
    [Arguments(null)]
    [Arguments(1)]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(value is null ? typeof(object) : typeof(int));
        await Assert.That((object?)value is null or 1).IsTrue();
    }
}

// A class row that does infer T keeps supplying it: the method rows run on C<string>, and the null row
// infers nothing of its own.
[EngineTest(ExpectedResult.Pass)]
[Arguments("class")]
public class ClassRowInfersTypeArgument<T>(T classValue)
{
    [Test]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)classValue).IsEqualTo("class");
        await Assert.That(value).IsNull();
    }
}

// A type-free class row beside another class data source: the method row infers C<int>, and the instance
// comes from both class sources (number = 42, then 43), not from the other source alone. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
[MethodDataSource(typeof(TypeFreeClassRowSources), nameof(TypeFreeClassRowSources.FortyThree))]
public class TypeFreeClassRowBesideOtherSource<T>(int number)
{
    [Test]
    [Arguments(1)]
    public async Task Run(T value)
    {
        await Assert.That(number is 42 or 43).IsTrue();
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(1);
    }
}

// A method source that infers no class type argument runs on the instantiation the method row infers, as
// it does with any other class row: C<int> runs Run(1) and Run(2). Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithUninferredMethodSource<T>(int number)
{
    [Test]
    [Arguments(1)]
    [MethodDataSource(typeof(TypeFreeClassRowSources), nameof(TypeFreeClassRowSources.Two))]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value is 1 or 2).IsTrue();
    }
}

// A [MethodDataSource] member of the test class infers T from its declared type, as without a class row: C<int>
// runs Run(5), built from the class row (number = 42). Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithInferringMethodSource<T>(int number)
{
    public static IEnumerable<int> Rows() => [5];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(5);
    }
}

// Beside only a type-free class row, a method row that infers types breaking the class's constraints fails, as
// without a class row: [Arguments(null)] infers object, which is not a struct. [Arguments(1)] runs on C<int>.
// No [EngineTest]: TUnit.Engine.Tests targets it by name, because it has a failing row.
[Arguments(42)]
public class TypeFreeClassRowConstraintViolation<T>(int number) where T : struct
{
    [Test]
    [Arguments(1)]
    [Arguments(null)]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That((object?)value).IsEqualTo(1);
    }
}

// A [MethodDataSource] member of the test class beside a method row that infers T runs on the instantiation the row
// infers, not on one inferred from its own declared type: C<int> runs Run(1) and Run(2), never C<object>. Same in
// both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithInferringRowAndMethodSource<T>(int number)
{
    public static IEnumerable<object> Rows() => [2];

    [Test]
    [Arguments(1)]
    [MethodDataSource(nameof(Rows))]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value is 1 or 2).IsTrue();
    }
}

// The engine supplies the CancellationToken, so the row's values bind to the other parameters: T is int, which
// satisfies the struct constraint, and the null goes to object, not to T. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithCancellationToken<T>(int number) where T : struct
{
    [Test]
    [Arguments(1, null)]
    public async Task Run(CancellationToken token, T value, object? other)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That((object)value).IsEqualTo(1);
        await Assert.That(other).IsNull();
    }
}

// A typed data source infers T from its declared type, as without a class row: ClassDataSource<int> runs on C<int>,
// built from the class row (number = 42). Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithTypedMethodSource<T>(int number)
{
    [Test]
    [ClassDataSource<int>]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(0);
    }
}

// Beside only a type-free class row, a parameter attribute that infers T (Matrix<int> implements IInfersType<int>)
// supplies the instantiation, as in the source generator: both matrix values run on C<int>, each instance built from
// the class row (number = 42). Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithInferringParameterAttribute<T>(int number)
{
    [Test]
    [MatrixDataSource]
    public async Task Run([Matrix<int>(1, 2)] T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsNotNull();
    }
}

// Beside only a type-free class row, a method row whose parameter contains T, not only T itself, still reports the
// types that break the class's constraints: new int[] { 1 } for T[] infers int, which is not a class. Run(["a"])
// runs on C<string>. No [EngineTest]: TUnit.Engine.Tests targets it by name, because it has a failing row.
[Arguments(42)]
public class TypeFreeClassRowArrayConstraintViolation<T>(int number) where T : class
{
    [Test]
    [Arguments(new object[] { new[] { "a" } })]
    [Arguments(new object[] { new[] { 1 } })]
    public async Task Run(T[] values)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Length).IsEqualTo(1);
    }
}

public static class TypeFreeClassRowSources
{
    public static IEnumerable<int> FortyThree() => [43];

    public static IEnumerable<int> Two() => [2];
}
