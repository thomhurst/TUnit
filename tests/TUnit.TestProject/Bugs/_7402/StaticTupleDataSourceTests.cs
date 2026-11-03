using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class StaticTupleDataSourceTests
{
    public static (int, int) NumericValue() => (1, 2);
    public static IEnumerable<(int, int)> NumericRows() => [NumericValue()];
    public static Task<(int, int)> NumericTask() => Task.FromResult(NumericValue());
    public static ValueTask<(int, int)> NumericValueTask() => new(NumericValue());
    public static async IAsyncEnumerable<(int, int)> NumericAsyncRows()
    {
        await Task.Yield();
        yield return NumericValue();
    }

    public static (int, int, int, int, int, int, int, int) LongNumericValue() => (1, 2, 3, 4, 5, 6, 7, 8);

    [Test]
    [MethodDataSource(nameof(LongNumericValue))]
    public async Task Long_Tuple_Elements_Can_Be_Converted((long, long, long, long, long, long, long, long) value)
    {
        await Assert.That(value).IsEqualTo((1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L));
    }

    [Test]
    [MethodDataSource(nameof(NumericValue))]
    [MethodDataSource(nameof(NumericRows))]
    [MethodDataSource(nameof(NumericTask))]
    [MethodDataSource(nameof(NumericValueTask))]
    [MethodDataSource(nameof(NumericAsyncRows))]
    public async Task Tuple_Elements_Can_Be_Converted((long, long) value)
    {
        await Assert.That(value.Item1).IsEqualTo(1L);
        await Assert.That(value.Item2).IsEqualTo(2L);
    }

    [Test]
    [MethodDataSource(nameof(NumericValue))]
    public async Task Tuple_Conversion_With_Trailing_Token((long, long) value, CancellationToken token)
    {
        await Assert.That(value).IsEqualTo((1L, 2L));
        await Assert.That(token).IsEqualTo(TestContext.Current!.Execution.CancellationToken);
    }

    [Test]
    [MethodDataSource(nameof(NumericValue))]
    public async Task Tuple_Conversion_With_Leading_Token(CancellationToken token, (long, long) value)
    {
        await Assert.That(value).IsEqualTo((1L, 2L));
        await Assert.That(token).IsEqualTo(TestContext.Current!.Execution.CancellationToken);
    }

    public static Tuple<int, int, int, int, int, int, int, Tuple<int>> LongReferenceValue()
        => Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8);

    [Test]
    [MethodDataSource(nameof(LongReferenceValue))]
    public async Task Long_Reference_Tuple_Elements_Can_Be_Converted(
        Tuple<long, long, long, long, long, long, long, Tuple<long>> value)
    {
        await Assert.That(value).IsEqualTo(Tuple.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L));
    }

    public static Tuple<char> CharacterReferenceValue() => Tuple.Create('A');

    [Test]
    [MethodDataSource(nameof(CharacterReferenceValue))]
    public async Task Reference_Tuple_Character_Can_Be_Converted_To_Decimal(Tuple<decimal> value)
    {
        await Assert.That(value.Item1).IsEqualTo(65m);
    }

    public sealed class ImplicitTupleValue
    {
        public static implicit operator (int, int)(ImplicitTupleValue value) => (1, 2);
    }

    public sealed class ExplicitTupleValue
    {
        public static explicit operator (int, int)(ExplicitTupleValue value) => (4, 5);
    }

    public static (ImplicitTupleValue, int) ImplicitNestedValue()
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        {
            // Method-source-only conversion types require explicit AOT registration (#7511).
            TUnit.Core.Converters.AotConverterRegistry.Register<ImplicitTupleValue, (int, int)>(value => value);
        }

        return (new(), 3);
    }

    public static Tuple<ImplicitTupleValue> NullableTupleOperatorRow()
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        {
            TUnit.Core.Converters.AotConverterRegistry.Register<ImplicitTupleValue, (int, int)>(value => value);
        }

        return Tuple.Create(new ImplicitTupleValue());
    }

    public static IEnumerable<Tuple<ImplicitTupleValue>> NullableTupleOperatorRows() => [NullableTupleOperatorRow()];

    [Test]
    [MethodDataSource(nameof(NullableTupleOperatorRow))]
    [MethodDataSource(nameof(NullableTupleOperatorRows))]
    public async Task Reference_Tuple_Uses_Implicit_Conversion_To_Nullable_Tuple(Tuple<(int, int)?> value)
    {
        await Assert.That(value.Item1).IsEqualTo(((int, int)?)(1, 2));
    }

    public static (ExplicitTupleValue, int) ExplicitNestedValue()
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        {
            TUnit.Core.Converters.AotConverterRegistry.Register<ExplicitTupleValue, (int, int)>(value => ((int, int))value);
        }

        return (new(), 6);
    }

    [Test]
    [MethodDataSource(nameof(ImplicitNestedValue))]
    public async Task Nested_Tuple_Uses_Implicit_Conversion(((int, int), int) value)
    {
        await Assert.That(value).IsEqualTo(((1, 2), 3));
    }

    [Test]
#pragma warning disable TUnit0001 // Exercise the runtime explicit conversion fallback.
    [MethodDataSource(nameof(ExplicitNestedValue))]
#pragma warning restore TUnit0001
    public async Task Nested_Tuple_Uses_Explicit_Conversion(((int, int), int) value)
    {
        await Assert.That(value).IsEqualTo(((4, 5), 6));
    }

    public static (object?, int) NullNestedValue() => (null, 3);

    [Test]
#pragma warning disable TUnit0001 // Exercise runtime null-to-default conversion for a nested tuple.
    [MethodDataSource(nameof(NullNestedValue))]
#pragma warning restore TUnit0001
    public async Task Null_Nested_Tuple_Uses_Default(((int, int), int) value)
    {
        await Assert.That(value).IsEqualTo(((0, 0), 3));
    }

    public static ((int, int), int) NestedNumericValue() => ((1, 2), 3);

    [Test]
    [MethodDataSource(nameof(NestedNumericValue))]
    public async Task Nested_Tuple_Elements_Can_Be_Converted(((long, long), long) value)
    {
        await Assert.That(value).IsEqualTo(((1L, 2L), 3L));
    }

    public static ((int, int, int, int, int, int, int, int), int) NestedLongNumericValue()
        => ((1, 2, 3, 4, 5, 6, 7, 8), 9);

    [Test]
    [MethodDataSource(nameof(NestedLongNumericValue))]
    public async Task Nested_Long_Tuple_Elements_Can_Be_Converted(((long, long, long, long, long, long, long, long), long) value)
    {
        await Assert.That(value).IsEqualTo(((1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L), 9L));
    }

    [Test]
    [MethodDataSource(nameof(Value))]
    public async Task Converted_Tuple_Holding_Null_Stays_A_Tuple(Tuple<object?> value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value.Item1).IsNull();
    }

    public static ValueTuple<string?> NullValueTuple() => new((string?)null);

    [Test]
    [MethodDataSource(nameof(NullValueTuple))]
    public async Task Converted_Value_Tuple_Holds_Null(ValueTuple<object?> value)
    {
        await Assert.That(value.Item1).IsNull();
    }

    public static Tuple<string?> Value() => Tuple.Create<string?>(null);
    public static IEnumerable<Tuple<string?>> Rows() => [Value()];
    public static Task<Tuple<string?>> TaskRow() => Task.FromResult(Value());
    public static ValueTask<Tuple<string?>> ValueTaskRow() => new(Value());
    public static async IAsyncEnumerable<Tuple<string?>> AsyncRows()
    {
        await Task.Yield();
        yield return Value();
    }

    [Test]
    [MethodDataSource(nameof(Value))]
    [MethodDataSource(nameof(Rows))]
    [MethodDataSource(nameof(TaskRow))]
    [MethodDataSource(nameof(ValueTaskRow))]
    [MethodDataSource(nameof(AsyncRows))]
    public async Task Tuple_Holding_Null_Stays_A_Tuple(Tuple<string?>? value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value!.Item1).IsNull();
    }
}
