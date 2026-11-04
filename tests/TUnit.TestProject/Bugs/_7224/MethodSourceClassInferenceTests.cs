using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7224;

[EngineTest(ExpectedResult.Pass)]
public class MethodSourceClassInferenceTests<T>
{
    public static IEnumerable<TestDataRow<ValueTuple<TestDataRow<int>>>> NestedRows()
        => [new(new ValueTuple<TestDataRow<int>>(new(42)))];

    [Test, MethodDataSource(nameof(NestedRows))]
    public async Task NestedWrappedTuple(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(TestDataRow<int>));
        await Assert.That(((TestDataRow<int>)(object)value!).Data).IsEqualTo(42);
    }

    public static IEnumerable<ValueTuple<TestDataRow<int>>> BareTupleRows()
        => [new(new(42))];

    [Test, MethodDataSource(nameof(BareTupleRows))]
    public async Task BareWrappedTuple(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)42);
    }

    #pragma warning disable TUnit0001 // Omitted plain trailing arrays are supported at runtime; analyzer follow-up: #7587.
    [Test, MethodDataSource(nameof(SingleRows))]
    public async Task ScalarEmptyPlainTrailing(T first, T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)1);
        await Assert.That(rest.Length).IsEqualTo(0);
    }

    [Test, MethodDataSource(nameof(TupleRows))]
    public async Task TupleEmptyPlainTrailing(T first, string label, T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)1);
        await Assert.That(label).IsEqualTo("one");
        await Assert.That(rest.Length).IsEqualTo(0);
    }

    #pragma warning restore TUnit0001

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
    public static IEnumerable<Tuple<string, int>> ReferenceTupleRows() => [System.Tuple.Create("one", 1)];
    public static IEnumerable<List<int>> ListRows() => [new() { 1 }];
    public static IEnumerable<InferenceDerived<int>> DerivedRows() => [new()];
    public static IEnumerable<object> UntypedRows() => [1];

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayPlainTrailing(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Length).IsEqualTo(2);
    }

    [Test, MethodDataSource(nameof(WrappedRows))]
    public async Task WrappedArrayEmptyPlainTrailing(T first, IEnumerable<T>[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(rest.Length).IsEqualTo(0);
    }

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

    [Test, MethodDataSource(nameof(WrappedRows))]
    public async Task WrappedArrayOptional(T first, IEnumerable<T>? rest = null)
    {
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(rest).IsNull();
    }

    [Test, MethodDataSource(nameof(WrappedRows))]
    public async Task WrappedArrayEmptyParams(T first, params IEnumerable<T>[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(rest.Length).IsEqualTo(0);
    }

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArraySuppliedOptional(string label, T value = default!)
    {
        await Assert.That(label).IsEqualTo("one");
        await Assert.That(value).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayParamsCollection(params IEnumerable<T> values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Cast<string>().ToArray()).IsEquivalentTo(new[] { "one", "two" });
    }

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayLaterParamsCollection(string label, params IReadOnlyList<T> values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(label).IsEqualTo("one");
        await Assert.That(values.Single()).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayParams(params T[] values)
    {
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo((T)(object)"one");
        await Assert.That(values[1]).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedArrayLaterParams(string label, params T[] values)
    {
        await Assert.That(label).IsEqualTo("one");
        await Assert.That(values.Single()).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(ReferenceTupleRows))]
    public async Task ReferenceTuple(Tuple<T, int> value)
    {
        await Assert.That(value.Item1).IsEqualTo((T)(object)"one");
        await Assert.That(value.Item2).IsEqualTo(1);
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

[EngineTest(ExpectedResult.Pass)]
public class MethodSourceMethodInferenceTests
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one", "two" })];

    [Test, MethodDataSource(nameof(Rows))]
    public async Task WrappedArrayParams<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[1]).IsEqualTo((T)(object)"two");
    }

    [Test, MethodDataSource(nameof(Rows))]
    public async Task WrappedArrayPlainTrailing<T>(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Length).IsEqualTo(2);
    }

    public static IEnumerable<TestDataRow<Func<int>[]>> DelegateRows() => [new(new Func<int>[] { () => 42 })];

    [Test, MethodDataSource(nameof(DelegateRows))]
    public async Task WrappedDelegateArray<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Func<int>));
        await Assert.That(((Func<int>)(object)values.Single()!)()).IsEqualTo(42);
    }

    [Test, MethodDataSource(nameof(DelegateRows))]
    public async Task WrappedDelegateArrayWithFirst<T>(T first, params T[] rest)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Func<int>));
        await Assert.That(((Func<int>)(object)first!)()).IsEqualTo(42);
        await Assert.That(rest.Length).IsEqualTo(0);
    }

    [Test, MethodDataSource(typeof(ExternalWrappedRows), nameof(ExternalWrappedRows.Rows))]
    public async Task ExternalWrappedArray<T>(params T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Uri));
        await Assert.That(((Uri)(object)values.Single()!).Host).IsEqualTo("example.com");
    }

}

[EngineTest(ExpectedResult.Pass)]
public class WrappedArrayPartialOptionalInferenceTests<T, U>
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one", "two" })];

    [Test, MethodDataSource(nameof(Rows))]
    public async Task OptionalFillsMissingType(T first, U second = default!, IEnumerable<T>? omitted = null)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(U)).IsEqualTo(typeof(string));
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(second).IsEqualTo((U)(object)"two");
        await Assert.That(omitted).IsNull();
    }
}

public static class ExternalWrappedRows
{
    public static IEnumerable<TestDataRow<Uri[]>> Rows() => [new(new[] { new Uri("https://example.com") })];
}

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorInferenceTests<T>(params T[] values)
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one", "two" })];

    [Test, Arguments("seed")]
    public async Task ConstructorArray(T seed)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[1]).IsEqualTo((T)(object)"two");
    }
}
