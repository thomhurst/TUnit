namespace TUnit.TestProject.Bugs._7638;

// #7622: analyzer spreading does not yet model whole tuples with generic method types.
#pragma warning disable TUnit0001

public class NestedTupleWithTrailingParametersTests
{
    public static IEnumerable<((int, string), int)> ValueRows() => [((1, "tuple"), 7)];
    public static IEnumerable<Tuple<Tuple<int, string>, int>> ReferenceRows() => [Tuple.Create(Tuple.Create(1, "tuple"), 7)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueOptional<T>((T, string) row, int extra = 5) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 7)
            throw new InvalidOperationException("Expected nested tuple and supplied column");
    }

    public static IEnumerable<((int, string), int[])> ValueParamsRows() => [((1, "tuple"), [7])];

    [Test]
    [MethodDataSource(nameof(ValueParamsRows))]
    public void ValueParams<T>((T, string) row, params int[] extra) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra is not [7])
            throw new InvalidOperationException("Expected nested tuple and supplied params column");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceOptional<T>(Tuple<T, string> row, int extra = 5) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 7)
            throw new InvalidOperationException("Expected nested tuple and supplied column");
    }

    public static IEnumerable<Tuple<Tuple<int, string>, int[]>> ReferenceParamsRows() => [Tuple.Create(Tuple.Create(1, "tuple"), new[] { 7 })];

    [Test]
    [MethodDataSource(nameof(ReferenceParamsRows))]
    public void ReferenceParams<T>(Tuple<T, string> row, params int[] extra) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra is not [7])
            throw new InvalidOperationException("Expected nested tuple and supplied params column");
    }
}

public class ClosedNestedTupleConversionTests
{
    public static IEnumerable<((int, (object, int)), (int, int))> Rows() => [((1, (new object(), 2)), (3, 4))];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void ClosedNested<T>((T, (object, int)) pair, (int, int)? extra = null)
    {
        if (typeof(T) != typeof(int) || pair.Item2.Item2 != 2 || extra != (3, 4))
            throw new InvalidOperationException("Expected separate columns with a closed nested tuple");
    }
}

public class RepeatedTupleBindingTests
{
    public static IEnumerable<((string, string), int)> ValueRows() => [(("a", "b"), 7)];
    public static IEnumerable<Tuple<Tuple<int, int>, Tuple<int, string>>> ReferenceRows() =>
        [Tuple.Create(Tuple.Create(1, 2), Tuple.Create(7, "extra"))];
    public static IEnumerable<(int, int)> WholeRows() => [(1, 2)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void RepeatedValue<T>((T, T) row, int extra = 0)
    {
        if (typeof(T) != typeof(string) || extra != 7)
            throw new InvalidOperationException("Expected consistent string binding and separate extra column");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void RepeatedReference<T>(Tuple<T, T> row, Tuple<int, string>? extra = null)
    {
        if (typeof(T) != typeof(int) || extra?.Item2 != "extra")
            throw new InvalidOperationException("Expected consistent int binding and separate extra column");
    }

    [Test]
    [MethodDataSource(nameof(WholeRows))]
    public void RepeatedWhole<T>((T, T) row)
    {
        if (typeof(T) != typeof(int))
            throw new InvalidOperationException("Expected consistent whole-row int binding");
    }
}

public class WholeReferenceTupleSourceTests
{
    public static IEnumerable<Tuple<int, string>> Rows() => [Tuple.Create(1, "tuple")];

    [Test]
    [GenerateGenericTest(typeof(Tuple<int, string>))]
    [MethodDataSource(nameof(Rows))]
    public void WholeRow<T>(T row)
    {
        if (row is not Tuple<int, string> { Item1: 1, Item2: "tuple" })
            throw new InvalidOperationException("Expected the whole reference tuple exactly once");
    }
}

public class InferredReferenceTupleSourceTests
{
    public static IEnumerable<Tuple<int>> Rows() => [Tuple.Create(2)];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void Element<T>(T value) where T : struct
    {
        if (typeof(T) != typeof(int) || !Equals(value, 2))
            throw new InvalidOperationException("Expected first reference-tuple element");
    }
}

public class WholeTupleWithTrailingParametersTests
{
    public static IEnumerable<(int, string)> ValueRows() => [(1, "tuple")];
    public static IEnumerable<Tuple<int, string>> ReferenceRows() => [Tuple.Create(1, "tuple")];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueOptional<T>((T, string) row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole value tuple and default optional value");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueParams<T>((T, string) row, params int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole value tuple and empty params");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueArray<T>((T, string) row, int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole value tuple and empty collecting array");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceOptional<T>(Tuple<T, string> row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole reference tuple and default optional value");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceParams<T>(Tuple<T, string> row, params int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole reference tuple and empty params");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceArray<T>(Tuple<T, string> row, int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole reference tuple and empty collecting array");
    }
}

public class ExplicitWholeTupleInferenceTests
{
    public static IEnumerable<Func<(int, string)>> Rows() => [() => (1, "tuple")];
    public static IEnumerable<Func<((int, string), int)>> NestedRows() => [() => ((1, "tuple"), 7)];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(Rows))]
    public void Optional<T>((T, string) row, int extra = 5)
    {
        if (row.Item1 is not (int or long) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole tuple and optional default");
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(Rows))]
    public void Params<T>((T, string) row, params int[] extra)
    {
        if (row.Item1 is not (int or long) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole tuple and empty params");
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(Rows))]
    public void Array<T>((T, string) row, int[] extra)
    {
        if (row.Item1 is not (int or long) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole tuple and empty trailing array");
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(NestedRows))]
    public void Nested<T>((T, string) row, int extra = 5)
    {
        if (row.Item1 is not (int or long) || row.Item2 != "tuple" || extra != 7)
            throw new InvalidOperationException("Expected nested tuple with supplied column");
    }
}

public class ExplicitBareReferenceTupleInferenceTests
{
    public static IEnumerable<Func<Tuple<int, string>>> Rows() => [() => Tuple.Create(1, "tuple")];

    [Test, GenerateGenericTest(typeof(Tuple<int, string>)), MethodDataSource(nameof(Rows))]
    public void WholeRow<T>(T row)
    {
        if (typeof(T) != typeof(Tuple<int, string>) || row is not Tuple<int, string> { Item1: 1, Item2: "tuple" })
            throw new InvalidOperationException("Expected only the whole reference-tuple instantiation");
    }
}

public class ExplicitDirectTupleInferenceTests
{
    public static IEnumerable<(int, string)> ValueRows() => [(1, "tuple")];
    public static IEnumerable<Tuple<int, string>> ReferenceRows() => [Tuple.Create(1, "tuple")];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, string) row) => Verify(row.Item1, row.Item2);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, string> row) => Verify(row.Item1, row.Item2);

    private static void Verify<T>(T value, string text)
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(long)
            || value is not (int or long) || Convert.ToInt64(value) != 1 || text != "tuple")
            throw new InvalidOperationException("Expected both explicit long and inferred int direct tuple instantiations");
    }
}

public class ExplicitWrappedTupleInferenceTests
{
    public static IEnumerable<TestDataRow<(int, string)>> Rows() => [new((1, "tuple"))];
    public static IEnumerable<TestDataRow<Func<(int, string)>>> InnerRows() => [new(() => (1, "tuple"))];
    public static IEnumerable<Func<TestDataRow<Func<(int, string)>>>> OuterRows() => [() => new(() => (1, "tuple"))];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(Rows))]
    public void Direct<T>((T, string) row, int extra = 5) => Verify(row, extra);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(InnerRows))]
    public void Inner<T>((T, string) row, int extra = 5) => Verify(row, extra);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(OuterRows))]
    public void Outer<T>((T, string) row, int extra = 5) => Verify(row, extra);

    private static void Verify<T>((T, string) row, int extra)
    {
        if (typeof(T) != typeof(int) || Convert.ToInt64(row.Item1) != 1 || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected inferred int whole row and optional default");
    }
}

public class ExplicitSpreadColumnInferenceTests
{
    public static IEnumerable<(int, long[])> ArrayRows() => [(1, [2L])];
    public static IEnumerable<(int, long)> NullableRows() => [(1, 2L)];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ArrayRows))]
    public void Array<T>(T first, T[] extra)
    {
        if (typeof(T) != typeof(long) || Convert.ToInt64(first) != 1 || extra.Length != 1 || Convert.ToInt64(extra[0]) != 2)
            throw new InvalidOperationException("Expected only long inferred from the array column");
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(NullableRows))]
    public void Nullable<T>(T first, T? extra) where T : struct
    {
        if (typeof(T) != typeof(long) || Convert.ToInt64(first) != 1 || Convert.ToInt64(extra) != 2)
            throw new InvalidOperationException("Expected only long inferred from the nullable column");
    }
}

#pragma warning restore TUnit0001

// #7622: analyzer spreading does not yet model whole tuples with generic method types.
#pragma warning disable TUnit0001
public class DerivedRowWholeTupleInferenceTests
{
    public sealed record Row : TestDataRow<(int, string)>
    {
        public Row() : base((1, "tuple")) { }
    }

    public static IEnumerable<Row> Rows() => [new()];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(Rows))]
    public void Run<T>((T, string) row)
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(long)
            || Convert.ToInt64(row.Item1) != 1 || row.Item2 != "tuple")
            throw new InvalidOperationException("Expected both explicit long and inferred int from a derived row");
    }
}

public class AsyncWholeTupleInferenceTests
{
    public static Task<IEnumerable<Func<(int, string)>>> TaskRows() =>
        Task.FromResult<IEnumerable<Func<(int, string)>>>([() => (1, "tuple")]);

    public static ValueTask<IEnumerable<Func<(int, string)>>> ValueTaskRows() =>
        new((IEnumerable<Func<(int, string)>>)[() => (1, "tuple")]);

    public static async IAsyncEnumerable<Func<(int, string)>> AsyncRows()
    {
        await Task.CompletedTask;
        yield return () => (1, "tuple");
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(TaskRows))]
    public void TaskOptional<T>((T, string) row, int extra = 5) => Verify(row, extra);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ValueTaskRows))]
    public void ValueTaskOptional<T>((T, string) row, int extra = 5) => Verify(row, extra);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(AsyncRows))]
    public void AsyncOptional<T>((T, string) row, int extra = 5) => Verify(row, extra);

    private static void Verify<T>((T, string) row, int extra)
    {
        if (typeof(T) != typeof(int) || Convert.ToInt64(row.Item1) != 1 || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected inferred int whole row and optional default");
    }
}
#pragma warning restore TUnit0001

// #7622: analyzer whole-row matching is tracked separately.
#pragma warning disable TUnit0001
public class WrappedWholeTupleInferenceTests
{
    public static IEnumerable<TestDataRow<(int, string)>> ValueRows() => [new((1, "tuple"))];
    public static IEnumerable<TestDataRow<Tuple<int, string>>> ReferenceRows() => [new(Tuple.Create(1, "tuple"))];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueOptional<T>((T, string) row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole value tuple and default optional value");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueParams<T>((T, string) row, params int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole value tuple and empty params");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueArray<T>((T, string) row, int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole value tuple and empty collecting array");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceOptional<T>(Tuple<T, string> row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole reference tuple and default optional value");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceParams<T>(Tuple<T, string> row, params int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole reference tuple and empty params");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceArray<T>(Tuple<T, string> row, int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole reference tuple and empty collecting array");
    }
}
#pragma warning restore TUnit0001

#pragma warning disable TUnit0001 // #7622: whole-tuple rows with omitted tails.
public class SingletonWholeTupleInferenceTests
{
    public static IEnumerable<ValueTuple<int>> ValueRows() => [new(1)];
    public static IEnumerable<Tuple<int>> ReferenceRows() => [Tuple.Create(1)];

    [Test, MethodDataSource(nameof(ValueRows))]
    public void ValueOptional<T>(ValueTuple<T> row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 1) || extra != 5)
            throw new InvalidOperationException("Expected whole singleton value tuple and default tail");
    }

    [Test, MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceOptional<T>(Tuple<T> row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 1) || extra != 5)
            throw new InvalidOperationException("Expected whole singleton reference tuple and default tail");
    }
}
#pragma warning restore TUnit0001
