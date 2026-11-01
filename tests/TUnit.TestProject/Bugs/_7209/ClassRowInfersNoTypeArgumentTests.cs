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

public static class TypeFreeClassRowSources
{
    public static IEnumerable<int> FortyThree() => [43];

    public static IEnumerable<int> Two() => [2];
}
