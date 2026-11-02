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

// With several public constructors, both modes bind the class row to the first one, as reflection's GetConstructors()
// does: (int number) takes no T, so the row is type-free and the method rows infer C<string> and C<int>. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowOverloadedConstructors<T>
{
    private readonly int _number;

    public TypeFreeClassRowOverloadedConstructors(int number) => _number = number;

    public TypeFreeClassRowOverloadedConstructors(T value, int number) : this(number)
    {
    }

    [Test]
    [Arguments("s")]
    [Arguments(1)]
    public async Task Run(T value)
    {
        await Assert.That(_number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(value!.GetType());
    }
}

// A primary constructor comes first, before an explicit constructor that takes T. The source generator used to skip
// the primary constructor because it has no constructor declaration, bind the row to (T value, int number) and infer
// C<int> from it, while reflection bound the row to the primary constructor. Both modes now bind to the primary
// constructor, so the row is type-free and the method rows infer C<string> and C<int>.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowPrimaryConstructorBesideTypedConstructor<T>(int number)
{
    public TypeFreeClassRowPrimaryConstructorBesideTypedConstructor(T value, int number) : this(number)
    {
    }

    [Test]
    [Arguments("s")]
    [Arguments(1)]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(value!.GetType());
    }
}

// A class row binds to the constructor marked [TestConstructor], the one reflection builds the instance with, not to
// the first public constructor: (T value) takes the row, so it infers C<int>, and the method row runs on it. Same in
// both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowTestConstructorTakesTypeParameter<T>
{
    private readonly string _constructor;

    public TypeFreeClassRowTestConstructorTakesTypeParameter(long number) => _constructor = $"long:{number}";

    [TestConstructor]
    public TypeFreeClassRowTestConstructorTakesTypeParameter(T value) => _constructor = $"T:{value}";

    [Test]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(_constructor).IsEqualTo("T:42");
    }
}

// The [TestConstructor] constructor takes no T, so the class row is type-free even though the first public
// constructor takes T: the method rows infer C<string> and C<bool>, each built through the marked constructor.
// Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowTestConstructorTakesNoTypeParameter<T>
{
    private readonly int _number;

    public TypeFreeClassRowTestConstructorTakesNoTypeParameter(T value) => _number = -1;

    [TestConstructor]
    public TypeFreeClassRowTestConstructorTakesNoTypeParameter(int number) => _number = number;

    [Test]
    [Arguments("s")]
    [Arguments(true)]
    public async Task Run(T value)
    {
        await Assert.That(_number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(value!.GetType());
    }
}

// A method data source that infers no class type runs beside the inferring [Arguments] row on the instantiation that
// row infers, with the instance built from the type-free class row: C<int> runs [Arguments(1)] and the source's 2
// and 3. Reflection mode used to drop the source there. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class TypeFreeClassRowWithUninferredMethodSource<T>(int number)
{
    [Test]
    [Arguments(1)]
    [MethodDataSource(typeof(TypeFreeClassRowSources), nameof(TypeFreeClassRowSources.Values))]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value is 1 or 2 or 3).IsTrue();
    }
}

// An empty class row has no value to supply a type argument, so it is type-free like [Arguments(42)] on
// C<T>(int number): the method rows infer C<int> and C<string>, each built from the empty row. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments]
public class EmptyClassRow<T>
{
    [Test]
    [Arguments(1)]
    [Arguments("s")]
    public async Task Run(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(value!.GetType());
    }
}

// The type-free class row supplies no class type argument, so the null method row's object breaks the struct
// constraint and is reported as a failed test, as without a class row (#7141), rather than dropped. Same in both
// modes. No [EngineTest] attribute: the failing row must not be picked up by the `/**[EngineTest=Pass]` run.
[Arguments(42)]
public class ConstrainedClassWithTypeFreeClassRow<T>(int number) where T : struct
{
    [Test]
    [Arguments(1)]
    [Arguments(null)]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(value).IsEqualTo((T)(object)1);
    }
}

// A CancellationToken declared before the data parameters is supplied by the engine, so [Arguments(1, "s")] binds
// 1 to value and infers C<int>, which meets the struct constraint. The constraint check beside the type-free class
// row must skip the token too, or it reads "s" as T's value and reports a false failure. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class ConstrainedClassWithTypeFreeClassRowAndLeadingToken<T>(int number) where T : struct
{
    [Test]
    [Arguments(1, "s")]
    public async Task Run(CancellationToken token, T value, object extra)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(extra).IsEqualTo("s");
    }
}

public static class TypeFreeClassRowSources
{
    public static IEnumerable<object> Values() => [2, 3];
}
