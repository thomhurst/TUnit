using System.Diagnostics.CodeAnalysis;
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

    private static int _wrappedFactoryCalls;

    public static int WrappedFactoryCalls => Volatile.Read(ref _wrappedFactoryCalls);

    public static IEnumerable<object> WrappedFactory() => [new TestDataRow<Func<int>>(() => Interlocked.Increment(ref _wrappedFactoryCalls))];

    public static IEnumerable<object> WrappedCounterFactory() => [new TestDataRow<Func<Issue7210Counter>>(() => new Issue7210Counter())];
}

public sealed class Issue7210Counter
{
    private int _count;

    public int Increment() => Interlocked.Increment(ref _count);
}

// Rows of an untyped source, whose Skip setting the test should keep in both modes
public sealed class SkippedMixedRowsAttribute : UntypedDataSourceGeneratorAttribute
{
    // The engine reads Skip through reflection, so a Native AOT build must keep its getter (see DataSourceMetadataExtractor)
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SkippedMixedRowsAttribute))]
    public SkippedMixedRowsAttribute()
    {
    }

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

// A wrapped factory is invoked once: the value it returned to infer the row's instantiation is the value the test gets
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.WrappedFactory))]
public class WrappedFactoryInvokedOnce<T>(T value)
{
    [Test]
    [Arguments(1)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That((int)(object)value!).IsEqualTo(1);
        await Assert.That(Issue7210Sources.WrappedFactoryCalls).IsEqualTo(1);
    }
}

// Every test built from a wrapped factory row gets its own object: only the first reuses the value read to infer the row
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(Issue7210Sources), nameof(Issue7210Sources.WrappedCounterFactory))]
public class WrappedFactoryFreshPerTest<T>(T value)
{
    public static IEnumerable<Func<Issue7210Counter>> Counters() => [() => new(), () => new(), () => new()];

    [Test]
    [MethodDataSource(nameof(Counters))]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<Issue7210Counter>();
        await Assert.That(((Issue7210Counter)(object)value!).Increment()).IsEqualTo(1);
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

// Custom untyped sources keep the nulls as constructor positions rather than treating an array as one value.
public sealed class NullLeadingRowAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => [null, 5];
    }
}

public sealed class AllNullRowAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => [null, null];
    }
}

// The typed row seeds the AOT instantiation and disables AllRows fallback. Both rows must run.
[EngineTest(ExpectedResult.Pass)]
[Arguments(null, 7)]
[NullLeadingRow]
public class NullLeadingRowInfersFirstNonNull<T>(T? value, T number) where T : struct
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsNull();
        await Assert.That((int)(object)number).IsIn(5, 7);
    }
}

// Neither null supplies a type: the method supplies T and AllRows retains both constructor positions.
[EngineTest(ExpectedResult.Pass)]
[AllNullRow]
public class AllNullRowsUseAllRowsFallback<T>(T? first, T? second) where T : class
{
    [Test]
    [Arguments("method")]
    public async Task Test(T arg)
    {
        await Assert.That(first).IsNull();
        await Assert.That(second).IsNull();
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
    }
}

// A later non-null value cannot infer both type parameters, so method inference uses AllRows.
[EngineTest(ExpectedResult.Pass)]
[NullLeadingRow]
public class NullLeadingTwoParameterRowUsesFallback<TFirst, TSecond>(TFirst? first, TSecond second)
    where TFirst : class
{
    [Test]
    [Arguments("method", 1)]
    public async Task Test(TFirst firstArg, TSecond secondArg)
    {
        await Assert.That(first).IsNull();
        await Assert.That(typeof(TFirst)).IsEqualTo(typeof(string));
        await Assert.That(typeof(TSecond)).IsEqualTo(typeof(int));
        await Assert.That((int)(object)second!).IsEqualTo(5);
    }
}

// With an Arguments row, null source values cannot supply the unmatched int instantiation (NoClassRow).
[EngineTest(ExpectedResult.Failure)]
[Arguments("class", "class")]
[AllNullRow]
public class AllNullRowsUseNoClassRowFallback<T>(T? first, T? second)
{
    [Test]
    [Arguments("method")]
    [Arguments(1)]
    public async Task Test(T arg)
    {
        await Assert.That(first).IsEqualTo((T)(object)"class");
        await Assert.That(second).IsEqualTo((T)(object)"class");
    }
}
