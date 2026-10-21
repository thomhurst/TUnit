using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7161;

// A method-level [Arguments] row that infers a generic class's type arguments, where no class-level
// [Arguments] row infers them, still runs on that instantiation. Its constructor arguments come from
// the class-level data, never from the method row's own values, in both modes (#7161).

public static class Issue7161Sources
{
    public static IEnumerable<int> Five() => [5];
}

// The class's types come from a class-level [MethodDataSource], which the source generator cannot
// type at compile time. Each method row runs on the instantiation it infers, constructed from the
// data source's 5 (converted to "5" for the string instantiation).
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7161Sources), nameof(Issue7161Sources.Five))]
public class ClassMethodDataSourceWithMethodRows<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsTypeOf<T>();
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(value!.ToString()).IsEqualTo("5");
    }
}

// The string instantiation comes from [GenerateGenericTest], so its constructor arguments come from the
// class row 5 (converted to "5"), as for any [GenerateGenericTest] instantiation, not from the method row.
[EngineTest(ExpectedResult.Pass)]
[Arguments(5)]
[GenerateGenericTest(typeof(string))]
public class GenerateGenericTestTypeOnlyMethodRowsInfer<T>(T value)
{
    [Test]
    [Arguments(5)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsTypeOf<T>();
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(value!.ToString()).IsEqualTo("5");
    }
}

// The generic [Arguments<int>] form is a class-level [Arguments] row too, so the method row "a", whose
// string instantiation no class row infers, fails with an explanation in both modes, as with [Arguments].
[EngineTest(ExpectedResult.Failure)]
[Arguments<int>(1)]
public class GenericArgumentsClassRowWithUnmatchedMethodRow<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsEqualTo(value);
    }
}

// A class-level [Arguments<int[]>] row is one value, an array, so the class is built from that array
// rather than from its elements.
[EngineTest(ExpectedResult.Pass)]
[Arguments<int[]>(new[] { 1, 2 })]
public class GenericArgumentsArrayClassRow<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int[]>();
        await Assert.That((int[])(object)value!).IsEquivalentTo(new[] { 1, 2 });
    }
}
