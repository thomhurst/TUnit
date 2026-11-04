using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7224;

[EngineTest(ExpectedResult.Pass)]
public class MethodSourceClassInferenceTests<T>
{
    public static IEnumerable<int> SingleRows() => [1];
    public static IEnumerable<(int, string)> TupleRows() => [(1, "one")];
    public static int[] ArrayRows => [1];
    public static int[] FieldRows = [1];
    public static Task<IEnumerable<int>> TaskRows() => Task.FromResult<IEnumerable<int>>([1]);
    public static ValueTask<int[]> ValueTaskRows() => new(new[] { 1 });
    public static async IAsyncEnumerable<int> AsyncRows()
    {
        await Task.Yield();
        yield return 1;
    }
    public static IEnumerable<int> OverloadedRows(int value) => [value];
    public static IEnumerable<string> OverloadedRows(string value) => [value];

    public static IEnumerable<TestDataRow<string[]>> WrappedRows() => [new(new[] { "one" })];
    public static IEnumerable<TestDataRow<string[]>> WrappedPairRows() => [new(new[] { "one", "two" })];
    public static IEnumerable<List<int>> ListRows() => [new() { 1 }];
    public static IEnumerable<InferenceDerived<int>> DerivedRows() => [new()];
    public static IEnumerable<object> UntypedRows() => [1];

    [Test, MethodDataSource(nameof(WrappedRows))]
    public async Task WrappedArray(T value) => await Assert.That(value).IsEqualTo((T)(object)"one");

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayPair(T first, T second)
    {
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(second).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayLaterParameter(string label, T value)
    {
        await Assert.That(label).IsEqualTo("one");
        await Assert.That(value).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(ListRows))]
    public async Task Interface(IEnumerable<T> values) => await Assert.That(values.Single()).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(DerivedRows))]
    public async Task BaseType(InferenceBase<T> value) => await Assert.That(typeof(T)).IsEqualTo(typeof(int));

    [Test, Arguments(1), MethodDataSource(nameof(UntypedRows))]
    public async Task MixedSources(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(SingleRows))]
    public async Task Single(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(TupleRows))]
    public async Task Tuple(CancellationToken token, T value, string name)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(name).IsEqualTo("one");
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test, MethodDataSource(nameof(ArrayRows))]
    public async Task Array(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(FieldRows))]
    public async Task Field(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(TaskRows))]
    public async Task TaskSource(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(ValueTaskRows))]
    public async Task ValueTaskSource(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(AsyncRows))]
    public async Task AsyncSource(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(nameof(OverloadedRows), Arguments = [1])]
    public async Task Overloaded(T value) => await Assert.That(value).IsEqualTo((T)(object)1);

    [Test, MethodDataSource(typeof(ExternalRows), nameof(ExternalRows.Rows))]
    public async Task External(T value) => await Assert.That(value).IsEqualTo((T)(object)1);
}

public static class ExternalRows
{
    public static IEnumerable<int> Rows() => [1];
}

[EngineTest(ExpectedResult.Pass)]
public class MultipleMethodSourceClassInferenceTests<T>
{
    public static IEnumerable<int> IntRows() => [1];
    public static IEnumerable<string> StringRows() => ["one"];
    public static IEnumerable<int> MoreIntRows() => [1];

    [Test, MethodDataSource(nameof(IntRows)), MethodDataSource(nameof(StringRows))]
    public async Task SeparateInstantiations(T value) => await Assert.That(value is int or string).IsTrue();

    [Test, MethodDataSource(nameof(IntRows)), MethodDataSource(nameof(MoreIntRows))]
    public async Task SameInstantiation(T value) => await Assert.That(value).IsEqualTo((T)(object)1);
}

public class InferenceBase<T>;
public class InferenceDerived<T> : InferenceBase<T>;

[EngineTest(ExpectedResult.Pass)]
public class NullableMethodSourceClassInferenceTests<T> where T : struct
{
    public static IEnumerable<int> Rows() => [1];

    #pragma warning disable TUnit0001 // Nullable generic class inference is supported at runtime; analyzer follow-up: #7573.
    [Test, MethodDataSource(nameof(Rows))]
    #pragma warning restore TUnit0001
    public async Task NullableValue(T? value) => await Assert.That(value).IsEqualTo((T)(object)1);
}
