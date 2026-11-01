using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7210;

// A class-level data source whose rows have different types, and whose row types the source generator cannot see
// (it returns IEnumerable<object>), runs each row on the generic class instantiation that the row's values infer,
// in both modes. Reflection mode groups the rows at discovery; source-generated mode splits them at run time (#7210).

public static class Issue7210Sources
{
    public static IEnumerable<object> Mixed() => ["s", 7];

    public static IEnumerable<object> Seven() => [7];

    public static IEnumerable<object> Text() => ["s"];
}

// Row 7 runs on the int instantiation beside the [Arguments(1)] class row, and only "s" runs on the string one.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.Mixed))]
public class ArgumentsRowAndMixedSource<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<T>();

        if (value is string text)
        {
            await Assert.That(text).IsEqualTo("s");
        }
        else
        {
            await Assert.That((int)(object)value!).IsIn(1, 7);
        }
    }
}

// Without class-level [Arguments] rows, each instantiation still takes only the rows that infer it.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.Mixed))]
public class MixedSourceOnly<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<T>();
        await Assert.That(value!.ToString()).IsEqualTo(value is string ? "s" : "7");
    }
}

// Rows split across two sources: each instantiation takes its rows from the source whose rows infer it, and none
// from the other, since a row of the other source does infer it.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.Seven))]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.Text))]
public class RowsSplitAcrossSources<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<T>();
        await Assert.That(value!.ToString()).IsEqualTo(value is string ? "s" : "7");
    }
}

// No class row infers double, and the class has a class-level [Arguments] row, so the method row 2.5 fails with
// an explanation in both modes, while the int rows run on the int instantiation.
[EngineTest(ExpectedResult.Failure)]
[Arguments(1)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.Mixed))]
public class MethodRowTypeNoClassRowInfers<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments(2.5)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<T>();
    }
}
