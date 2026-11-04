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
    public async Task Nested_Row_Preserves_Whether_A_Wrapper_Was_Removed()
    {
        var wrapped = MethodDataSourceAttribute.GetDeclaredRowType(
            typeof(IEnumerable<TestDataRow<ValueTuple<TestDataRow<int>>>>), out var spreadsArray, out var wrapperRemoved);
        await Assert.That(wrapped).IsEqualTo(typeof(ValueTuple<TestDataRow<int>>));
        await Assert.That(wrapperRemoved).IsTrue();
        await Assert.That(spreadsArray).IsFalse();

        var bare = MethodDataSourceAttribute.GetDeclaredRowType(
            typeof(IEnumerable<ValueTuple<TestDataRow<int>>>), out _, out var bareWrapperRemoved);
        await Assert.That(bare).IsEqualTo(wrapped);
        await Assert.That(bareWrapperRemoved).IsFalse();
    }

    [Test]
    public async Task Wrapped_Reference_Array_Preserves_Its_Spread_Shape()
    {
        await Assert.That(MethodDataSourceAttribute.GetDeclaredRowType(typeof(IEnumerable<TestDataRow<string[]>>))).IsEqualTo(typeof(string[]));

        var rowType = MethodDataSourceAttribute.GetDeclaredRowType(typeof(IEnumerable<TestDataRow<string[]>>), out var spreadsReferenceArray);
        await Assert.That(rowType).IsEqualTo(typeof(string));
        await Assert.That(spreadsReferenceArray).IsTrue();

        MethodDataSourceAttribute.GetDeclaredRowType(typeof(IEnumerable<string[]>), out var spreadsBareArray);
        await Assert.That(spreadsBareArray).IsFalse();
    }

    [Test]
    [Arguments(typeof(Task))]
    [Arguments(typeof(ValueTask))]
    [Arguments(typeof(object[][]))]
    [Arguments(typeof(IEnumerable<object[]>))]
    [Arguments(typeof(System.Collections.ArrayList))]
    [Arguments(typeof(IEnumerable<TestDataRow<object[]>>))]
    public async Task Rows_Of_Unknown_Type_Have_No_Declared_Row_Type(Type memberType)
    {
        await Assert.That(MethodDataSourceAttribute.GetDeclaredRowType(memberType)).IsNull();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Resultless_Source_Is_Awaited_Before_Yielding_A_Null_Row(bool useValueTask)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object source = useValueTask ? new ValueTask(completion.Task) : completion.Task;
        await using var rows = MethodDataSourceAttribute.GetRowsFromRuntimeValueAsync(source, CreateMetadata()).GetAsyncEnumerator();
        var move = rows.MoveNextAsync();
        await Assert.That(move.IsCompleted).IsFalse();
        completion.SetResult();
        await Assert.That(await move).IsTrue();
        var row = await rows.Current();
        await Assert.That(row!).Count().IsEqualTo(1);
        await Assert.That(row![0]).IsNull();
        await Assert.That(await rows.MoveNextAsync()).IsFalse();
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
