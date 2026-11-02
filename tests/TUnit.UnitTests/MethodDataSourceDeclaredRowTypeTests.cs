using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Keeps <see cref="MethodDataSourceAttribute.GetDeclaredRowType"/>, which reflection discovery uses to infer a generic
/// class's type arguments from a member's declared type (#7208), in step with
/// <see cref="MethodDataSourceAttribute.GetRowsFromRuntimeValueAsync"/>, which reads the member's value at run time:
/// every row the runtime reader yields must have the declared row type.
/// </summary>
public class MethodDataSourceDeclaredRowTypeTests
{
    public static IEnumerable<Func<(object Value, Type MemberType, Type RowType)>> Shapes()
    {
        yield return () => (new[] { 1, 2 }, typeof(int[]), typeof(int));
        yield return () => (new List<string> { "a", "b" }, typeof(IEnumerable<string>), typeof(string));
        yield return () => (new List<string> { "a" }, typeof(List<string>), typeof(string));
        yield return () => ("single", typeof(string), typeof(string));
        yield return () => (7, typeof(int), typeof(int));
        yield return () => ((1, "a"), typeof((int, string)), typeof((int, string)));
        yield return () => (Task.FromResult<IEnumerable<int>>([1, 2]), typeof(Task<IEnumerable<int>>), typeof(int));
        yield return () => (Task.FromResult("single"), typeof(Task<string>), typeof(string));
        yield return () => (new ValueTask<int[]>([1]), typeof(ValueTask<int[]>), typeof(int));
        yield return () => (AsyncRows(), typeof(IAsyncEnumerable<int>), typeof(int));
        yield return () => (new Func<int>[] { () => 3 }, typeof(Func<int>[]), typeof(int));
        yield return () => (new[] { new TestDataRow<int>(4) }, typeof(TestDataRow<int>[]), typeof(int));
        yield return () => (new List<TestDataRow<Func<string>>> { new(() => "a") }, typeof(IEnumerable<TestDataRow<Func<string>>>), typeof(string));
        yield return () => (new Func<TestDataRow<int>>[] { () => new(5) }, typeof(Func<TestDataRow<int>>[]), typeof(int));
    }

    [Test]
    [MethodDataSource(nameof(Shapes))]
    public async Task Runtime_Rows_Have_The_Declared_Row_Type(object value, Type memberType, Type rowType)
    {
        await Assert.That(MethodDataSourceAttribute.GetDeclaredRowType(memberType)).IsEqualTo(rowType);

        // A tuple row is spread across the parameters, one column per element
        Type[] columnTypes = rowType.IsGenericType && rowType.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
            ? rowType.GetGenericArguments()
            : [rowType];

        var rowCount = 0;
        await foreach (var factory in MethodDataSourceAttribute.GetRowsFromRuntimeValueAsync(value, CreateMetadata()))
        {
            rowCount++;
            var rawRow = await factory();
            await Assert.That(rawRow).IsNotNull();

            // Test building strips a TestDataRow (and invokes a Func inside it) after the reader yields the row
            var (row, _) = TestDataRowUnwrapper.UnwrapArray(rawRow!);
            await Assert.That(row!).Count().IsEqualTo(columnTypes.Length);
            for (var i = 0; i < columnTypes.Length; i++)
            {
                var column = row![i] is Func<int> func ? func() : row[i];
                await Assert.That(columnTypes[i].IsInstanceOfType(column)).IsTrue();
            }
        }

        await Assert.That(rowCount).IsGreaterThan(0);
    }

    [Test]
    [Arguments(typeof(object[][]))]
    [Arguments(typeof(IEnumerable<object[]>))]
    [Arguments(typeof(System.Collections.ArrayList))]
    [Arguments(typeof(IEnumerable<TestDataRow<object[]>>))]
    public async Task Rows_Of_Unknown_Type_Have_No_Declared_Row_Type(Type memberType)
    {
        await Assert.That(MethodDataSourceAttribute.GetDeclaredRowType(memberType)).IsNull();
    }

    private static async IAsyncEnumerable<int> AsyncRows()
    {
        await Task.Yield();
        yield return 1;
    }

    private static DataGeneratorMetadata CreateMetadata() => new()
    {
        // Not read when turning a value into rows.
        TestBuilderContext = null!,
        MembersToGenerate = [],
        TestInformation = null,
        Type = DataGeneratorType.ClassParameters,
        TestSessionId = string.Empty,
        TestClassInstance = null,
        ClassInstanceArguments = null,
    };
}
