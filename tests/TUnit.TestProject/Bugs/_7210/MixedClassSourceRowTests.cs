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

    public static IEnumerable<object> SevenAndWrappedEight() => [7, new TestDataRow<int>(8)];

    public static IEnumerable<object> NoRows() => [];
}

// Rows of an untyped source, whose Skip setting the test should keep in both modes
public sealed class SkippedMixedRowsAttribute : UntypedDataSourceGeneratorAttribute
{
    public string? Skip { get; set; }

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [7];
        yield return () => ["s"];
    }
}

// A row wrapped in TestDataRow<T> infers from the value it wraps, so both rows run on the int instantiation
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.SevenAndWrappedEight))]
public class WrappedRowInfersItsValue<T>(T value)
{
    [Test]
    [Arguments(1)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(arg).IsTypeOf<T>();
        await Assert.That((int)(object)value!).IsIn(7, 8);
    }
}

// An empty source with SkipIfEmpty still reports a skipped test when every class row is used
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.NoRows), SkipIfEmpty = true)]
public class EmptySourceWithSkipIfEmpty<T>(T value)
{
    [Test]
    [Arguments(1)]
    public void Test(T arg)
    {
        throw new InvalidOperationException($"Should have been skipped, but ran with '{value}' and '{arg}'");
    }
}

// The Skip setting of a custom untyped source applies to the rows it gives each instantiation
[EngineTest(ExpectedResult.Pass)]
[SkippedMixedRows(Skip = "Skipped by its class data source")]
public class CustomSourceSettingsKept<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public void Test(T arg)
    {
        throw new InvalidOperationException($"Should have been skipped, but ran with '{value}' and '{arg}'");
    }
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
