using TUnit.Assertions;

namespace TUnit.TestProject;

public sealed class DeferredStaticSourceAttribute(string memberName) : MethodDataSourceAttribute(memberName);

public class DerivedSourceDeferredRowsTests
{
    private static int _rowsRun;

    public static IEnumerable<(string, string)> StringRows() => [("built-in", "row")];
    public static IEnumerable<(object, object)> ObjectRows() => [("derived", "row")];

    [Test]
    [GenerateGenericTest(typeof(string))]
    [MethodDataSource(nameof(StringRows))]
    [DeferredStaticSource(nameof(ObjectRows))]
    public async Task Both_sources_join_explicit_types<T>(T first, T second) where T : IComparable<T>
    {
        Interlocked.Increment(ref _rowsRun);
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)second).IsEqualTo("row");
    }

    [After(HookType.Class)]
    public static async Task Both_rows_ran()
    {
        await Assert.That(_rowsRun).IsEqualTo(2);
    }
}
