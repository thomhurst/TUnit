using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7108: source-gen mode only inferred a generic test
/// method's type arguments from <c>IEnumerable&lt;Func&lt;(...)&gt;&gt;</c> data sources, so every other
/// row shape dropped the test silently. Each test here must be discovered in both modes.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericMethodNonTupleMethodDataSourceTests
{
    public static IEnumerable<Func<int>> FuncRows()
    {
        yield return () => 1;
    }

    public static IEnumerable<int> PlainRows()
    {
        yield return 2;
    }

    public static string[] ArrayRows() => ["three"];

    public static async IAsyncEnumerable<int> AsyncRows()
    {
        await Task.Yield();
        yield return 4;
    }

    public static Task<IEnumerable<int>> TaskRows() => Task.FromResult<IEnumerable<int>>([5]);

    public static IEnumerable<TestDataRow<int>> TestDataRows()
    {
        yield return new TestDataRow<int>(6);
    }

    public static IEnumerable<List<int>> ListRows()
    {
        yield return [7];
    }

    public static IEnumerable<(int, string)> PlainTupleRows()
    {
        yield return (8, "eight");
    }

    public static IEnumerable<int> RowsWithArgument(int value)
    {
        yield return value;
    }

    public static IEnumerable<int[]> IntArrayRows()
    {
        yield return [10, 11];
    }

    // Declared before the parameterless overload: Arguments must pick this one, as reflection mode does
    public static IEnumerable<string> OverloadedRows(int value)
    {
        yield return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static IEnumerable<int> OverloadedRows()
    {
        yield return -1;
    }

    public static Task<IAsyncEnumerable<int>> TaskOfAsyncRows() => Task.FromResult(AsyncRows());

    // Arguments = { 13 } is an int: the long overload is picked, as reflection's binder widens int to long
    public static IEnumerable<string> WideningRows()
    {
        yield return "parameterless";
    }

    public static IEnumerable<long> WideningRows(long value)
    {
        yield return value;
    }

    // Declared before the int overload: an exact argument type beats an implicit conversion
    public static IEnumerable<double> FloatWideningRows(double value)
    {
        yield return value;
    }

    public static IEnumerable<long> LongRows(long value)
    {
        yield return value;
    }

    public static IEnumerable<int> NullRows(int value)
    {
        yield return value;
    }

    public static IEnumerable<string> NullRows(string? value)
    {
        yield return value ?? "null";
    }

    public static IEnumerable<int> NullableRows(int? value)
    {
        yield return value ?? -1;
    }

    public static readonly int[] FieldRows = [19];

    public static IEnumerable<int> CancellationTokenOnlyRows(CancellationToken cancellationToken)
    {
        yield return 18;
    }

    public static IEnumerable<int> NullOptionalRows(int count)
    {
        yield return count;
    }

    public static IEnumerable<string> NullOptionalRows(string? text, int count = 1)
    {
        yield return (text ?? "null") + count;
    }

    public static IEnumerable<string> NullWideningRows(int first, int second)
    {
        yield return "int";
    }

    public static IEnumerable<long> NullWideningRows(string? text, long number)
    {
        yield return number;
    }

    public static IEnumerable<string> ExactTypeRows(long value)
    {
        yield return "long";
    }

    public static IEnumerable<int> ExactTypeRows(int value)
    {
        yield return value;
    }

    // Reflection only binds overloads with one parameter per argument, so two strings pick (object, object)
    public static IEnumerable<string> OptionalOverloadRows(object first, object second)
    {
        yield return $"{first}{second}";
    }

    public static IEnumerable<int> OptionalOverloadRows(string first, string second, int count = 1)
    {
        yield return count;
    }

    // A single overload with a missing optional parameter gets its default
    public static IEnumerable<int> OptionalParameterRows(string text, int count = 2)
    {
        yield return text.Length + count;
    }

    // A single overload with a missing CancellationToken gets CancellationToken.None
    public static IEnumerable<int> CancellationTokenRows(int value, CancellationToken cancellationToken)
    {
        yield return cancellationToken.CanBeCanceled ? -value : value;
    }

    [Test]
    [MethodDataSource(nameof(FuncRows))]
    public async Task Func_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
    }

    [Test]
    [MethodDataSource(nameof(PlainRows))]
    public async Task Plain_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)2);
    }

    [Test]
    [MethodDataSource(nameof(ArrayRows))]
    public async Task Array_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"three");
    }

    [Test]
    [MethodDataSource(nameof(AsyncRows))]
    public async Task Async_Enumerable_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)4);
    }

    [Test]
    [MethodDataSource(nameof(TaskRows))]
    public async Task Task_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)5);
    }

    [Test]
    [MethodDataSource(nameof(TestDataRows))]
    public async Task Test_Data_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)6);
    }

    [Test]
    [MethodDataSource(nameof(ListRows))]
    public async Task Nested_Generic_Row<T>(List<T> value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value.Count).IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(PlainTupleRows))]
    public async Task Plain_Tuple_Row<T1, T2>(T1 number, T2 text)
    {
        await Assert.That(typeof(T1)).IsEqualTo(typeof(int));
        await Assert.That(typeof(T2)).IsEqualTo(typeof(string));
        await Assert.That(number).IsEqualTo((T1)(object)8);
        await Assert.That(text).IsEqualTo((T2)(object)"eight");
    }

    [Test]
    [MethodDataSource(nameof(RowsWithArgument), Arguments = new object[] { 9 })]
    public async Task Data_Method_With_Arguments<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)9);
    }

    [Test]
    [MethodDataSource(nameof(IntArrayRows))]
    public async Task Array_Element_Row<T>(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Length).IsEqualTo(2);
    }

    [Test]
    [MethodDataSource(nameof(OverloadedRows), Arguments = new object[] { 12 })]
    public async Task Overload_Selected_By_Arguments<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"12");
    }

    [Test]
    [MethodDataSource(nameof(WideningRows), Arguments = new object[] { 13 })]
    public async Task Numeric_Argument_Is_Widened<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(long));
        await Assert.That(value).IsEqualTo((T)(object)13L);
    }

    [Test]
    [MethodDataSource(nameof(FloatWideningRows), Arguments = new object[] { 1.5f })]
    public async Task Float_Argument_Is_Widened_To_Double<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(double));
        await Assert.That(value).IsEqualTo((T)(object)1.5d);
    }

    [Test]
    [MethodDataSource(nameof(LongRows), Arguments = new object[] { 16L })]
    public async Task Long_Argument_Keeps_Its_Type<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(long));
        await Assert.That(value).IsEqualTo((T)(object)16L);
    }

    [Test]
    [MethodDataSource(nameof(NullableRows), Arguments = new object[] { 17 })]
    public async Task Argument_Is_Wrapped_In_Nullable<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)17);
    }

    [Test]
    [MethodDataSource(nameof(FieldRows))]
    public async Task Field_Data_Source<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)19);
    }

    [Test]
    [MethodDataSource(nameof(CancellationTokenOnlyRows))]
    public async Task Cancellation_Token_Is_Passed_Without_Arguments<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)18);
    }

    [Test]
    [MethodDataSource(nameof(ExactTypeRows), Arguments = new object[] { 15 })]
    public async Task Exact_Argument_Type_Beats_Conversion<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)15);
    }

    [Test]
    [MethodDataSource(nameof(OptionalOverloadRows), Arguments = new object[] { "a", "b" })]
    public async Task Exact_Parameter_Count_Overload_Wins<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"ab");
    }

    // TUnit0001 requires one argument per parameter. With it suppressed, a missing optional parameter or
    // CancellationToken is filled in at runtime, in both modes.
#pragma warning disable TUnit0001
    [Test]
    [MethodDataSource(nameof(OptionalParameterRows), Arguments = new object[] { "abc" })]
    public async Task Missing_Optional_Parameter_Uses_Default<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)5);
    }

    [Test]
    [MethodDataSource(nameof(CancellationTokenRows), Arguments = new object[] { 14 })]
    public async Task Missing_Cancellation_Token_Is_None<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)14);
    }

    // The analyzer does not yet treat a null argument as matching the string? overload (#7168)
    [Test]
    [MethodDataSource(nameof(NullRows), Arguments = new object?[] { null })]
    public async Task Null_Argument_Skips_Value_Type_Overload<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"null");
    }

    [Test]
    [MethodDataSource(nameof(NullOptionalRows), Arguments = new object?[] { null })]
    public async Task Null_Argument_With_Missing_Optional_Parameter<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"null1");
    }

    [Test]
    [MethodDataSource(nameof(NullWideningRows), Arguments = new object?[] { null, 20 })]
    public async Task Null_Argument_With_Widened_Argument<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(long));
        await Assert.That(value).IsEqualTo((T)(object)20L);
    }
#pragma warning restore TUnit0001

    [Test]
    [MethodDataSource(nameof(TaskOfAsyncRows))]
    public async Task Task_Of_Async_Enumerable_Is_One_Row<T>(T value)
    {
        await Assert.That(value is IAsyncEnumerable<int>).IsTrue();
    }
}
