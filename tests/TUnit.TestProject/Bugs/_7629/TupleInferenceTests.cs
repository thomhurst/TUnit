namespace TUnit.TestProject.Bugs._7629;

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

public class OpenTupleAssignabilityTests
{
    public static IEnumerable<((int, object), string)> ValueRows() => [((1, new object()), "extra")];
    public static IEnumerable<Tuple<Tuple<int, object>, string>> ReferenceRows() =>
        [Tuple.Create(Tuple.Create(1, new object()), "extra")];
    public static IEnumerable<Tuple<int, string>> CrossFamilyRows() => [Tuple.Create(1, "tuple")];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueAssignable<T>((T, object) row, string extra = "")
    {
        if (typeof(T) != typeof(int) || extra != "extra")
            throw new InvalidOperationException("Expected separate columns and int inference");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceAssignable<T>(Tuple<T, object> row, string extra = "")
    {
        if (typeof(T) != typeof(int) || extra != "extra")
            throw new InvalidOperationException("Expected separate columns and int inference");
    }

    [Test]
    // TUnit0001 currently spreads a reference tuple before matching an open value-tuple parameter.
#pragma warning disable TUnit0001
    [MethodDataSource(nameof(CrossFamilyRows))]
#pragma warning restore TUnit0001
    public void CrossFamily<T>((T, string) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple")
            throw new InvalidOperationException("Expected logical tuple element inference");
    }
}

public class SingleTupleElementConversionTests
{
    public static IEnumerable<(int, int)> Rows() => [(1, 2)];

    public static IEnumerable<Tuple<int, int>> NarrowReferenceRows() => [Tuple.Create(3, 4)];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void NarrowNumericElement<T>((T, short) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 2)
            throw new InvalidOperationException("Expected narrowing numeric element conversion");
    }

    [Test]
    [MethodDataSource(nameof(NarrowReferenceRows))]
    public void NarrowNumericReferenceElement<T>(Tuple<T, short> row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 4)
            throw new InvalidOperationException("Expected narrowing numeric reference element conversion");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void EnumElement<T>((T, DayOfWeek) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != DayOfWeek.Tuesday)
            throw new InvalidOperationException("Expected integral-to-enum element conversion");
    }

    [Test]
    [MethodDataSource(nameof(NarrowReferenceRows))]
    public void EnumReferenceElement<T>(Tuple<T, DayOfWeek> row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != DayOfWeek.Thursday)
            throw new InvalidOperationException("Expected integral-to-enum reference element conversion");
    }

    public static IEnumerable<(int, bool)> ConvertibleRows() => [(1, true)];
    public static IEnumerable<Tuple<int, int>> ConvertibleReferenceRows() => [Tuple.Create(2, 1)];

    [Test]
    [MethodDataSource(nameof(ConvertibleRows))]
    public void ConvertibleElement<T>((T, int) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 1)
            throw new InvalidOperationException("Expected IConvertible element conversion");
    }

    [Test]
    [MethodDataSource(nameof(ConvertibleReferenceRows))]
    public void ConvertibleReferenceElement<T>(Tuple<T, bool?> row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != true)
            throw new InvalidOperationException("Expected nullable IConvertible reference element conversion");
    }

    public static IEnumerable<(int, string)> StringRows() => [(1, "2")];
    public static IEnumerable<Tuple<int, string>> StringReferenceRows() => [Tuple.Create(3, "4.5")];

    [Test]
    [MethodDataSource(nameof(StringRows))]
    public void StringElement<T>((T, int) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 2)
            throw new InvalidOperationException("Expected parsed string element conversion");
    }

    [Test]
    [MethodDataSource(nameof(StringReferenceRows))]
    public void StringReferenceElement<T>(Tuple<T, decimal?> row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 4.5m)
            throw new InvalidOperationException("Expected parsed nullable string reference element conversion");
    }

    public class Source
    {
        public int Value { get; init; }
        public static implicit operator Destination(Source source) => new() { Value = source.Value };
    }

    public class Destination { public int Value { get; init; } }
    public static IEnumerable<(int, Source)> OperatorRows() => [(1, new Source { Value = 9 })];
    public static IEnumerable<Tuple<int, Source>> OperatorReferenceRows() => [Tuple.Create(2, new Source { Value = 10 })];

    [Test]
    [MethodDataSource(nameof(OperatorRows))]
    public void OperatorElement<T>((T, Destination) row)
    {
        if (typeof(T) != typeof(int) || row.Item2.Value != 9)
            throw new InvalidOperationException("Expected user-defined element conversion");
    }

    [Test]
    [MethodDataSource(nameof(OperatorReferenceRows))]
    public void OperatorReferenceElement<T>(Tuple<T, Destination> row)
    {
        if (typeof(T) != typeof(int) || row.Item2.Value != 10)
            throw new InvalidOperationException("Expected user-defined reference element conversion");
    }

    public class Base<T> { }
    public class Derived : Base<int> { }

    public static IEnumerable<(Derived, int)> BaseRows() => [(new Derived(), 7)];
    public static IEnumerable<Tuple<Derived, int>> BaseReferenceRows() => [Tuple.Create(new Derived(), 8)];

    [Test]
    [MethodDataSource(nameof(BaseRows))]
    public void BaseElement<T>((Base<T>, T) row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Derived || !Equals(row.Item2, 7))
            throw new InvalidOperationException("Expected generic base-class element conversion");
    }

    [Test]
    [MethodDataSource(nameof(BaseReferenceRows))]
    public void BaseReferenceElement<T>(Tuple<Base<T>, T> row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Derived || !Equals(row.Item2, 8))
            throw new InvalidOperationException("Expected generic base-class reference element conversion");
    }

    public static IEnumerable<(int[], int)> InterfaceRows() => [(new[] { 5 }, 5)];
    public static IEnumerable<Tuple<List<int>, int>> InterfaceReferenceRows() => [Tuple.Create(new List<int> { 6 }, 6)];

    [Test]
    [MethodDataSource(nameof(InterfaceRows))]
    public void InterfaceElement<T>((IEnumerable<T>, T) row)
    {
        if (typeof(T) != typeof(int) || !row.Item1.SequenceEqual(new[] { row.Item2 }))
            throw new InvalidOperationException("Expected generic interface element conversion");
    }

    [Test]
    [MethodDataSource(nameof(InterfaceReferenceRows))]
    public void InterfaceReferenceElement<T>(Tuple<IEnumerable<T>, T> row)
    {
        if (typeof(T) != typeof(int) || !row.Item1.SequenceEqual(new[] { row.Item2 }))
            throw new InvalidOperationException("Expected generic interface reference element conversion");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void NullableElement<T>((T, int?) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 2)
            throw new InvalidOperationException("Expected nullable element conversion");
    }

    public static IEnumerable<(int, string)> OpenNullableRows() => [(3, "nullable")];
    public static IEnumerable<Tuple<int, string>> OpenNullableReferenceRows() => [Tuple.Create(4, "nullable")];

    [Test]
    [MethodDataSource(nameof(OpenNullableRows))]
    public void OpenNullableElement<T>((T?, string) row) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item1 is not int value || value != 3 || row.Item2 != "nullable")
            throw new InvalidOperationException("Expected open nullable element inference");
    }

    [Test]
    [MethodDataSource(nameof(OpenNullableReferenceRows))]
    public void OpenNullableReferenceElement<T>(Tuple<T?, string> row) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item1 is not int value || value != 4 || row.Item2 != "nullable")
            throw new InvalidOperationException("Expected open nullable reference element inference");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void NumericElement<T>((T, long) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 2)
            throw new InvalidOperationException("Expected numeric element conversion");
    }
}


public class BoxedTupleElementConversionTests
{
    public static IEnumerable<(int, object)> ValueRows() => [(1, (object)2)];
    public static IEnumerable<Tuple<int, object>> ReferenceRows() => [Tuple.Create(3, (object)4)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, int) row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 1) || row.Item2 != 2)
            throw new InvalidOperationException("Expected boxed closed element conversion and int inference");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, int> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 3) || row.Item2 != 4)
            throw new InvalidOperationException("Expected boxed reference element conversion and int inference");
    }
}

public class WholeReferenceTupleSourceTests
{
    public static IEnumerable<Tuple<int, string>> Rows() => [Tuple.Create(1, "tuple")];

    [Test]
    [GenerateGenericTest(typeof(Tuple<int, string>))]
#pragma warning disable TUnit0001 // #7622: analyzer does not substitute explicit generic types before spreading.
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
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

public class TupleInterfaceCandidateIsolationTests
{
    public sealed class DualEnumerable : IEnumerable<int>, IEnumerable<string>
    {
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 1 }).GetEnumerator();
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => ((IEnumerable<string>)new[] { "value" }).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((IEnumerable<string>)this).GetEnumerator();
    }

    public static IEnumerable<(string, DualEnumerable)> ValueRows() => [("value", new DualEnumerable())];
    public static IEnumerable<Tuple<string, DualEnumerable>> ReferenceRows() => [Tuple.Create("value", new DualEnumerable())];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, IEnumerable<T>) row)
    {
        if (typeof(T) != typeof(string) || !row.Item2.SequenceEqual(new[] { row.Item1 }))
            throw new InvalidOperationException("Expected rejected interface candidates to leave string binding intact");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, IEnumerable<T>> row)
    {
        if (typeof(T) != typeof(string) || !row.Item2.SequenceEqual(new[] { row.Item1 }))
            throw new InvalidOperationException("Expected rejected reference interface candidates to leave string binding intact");
    }
}

// #7622: analyzer whole-reference-tuple matching is tracked separately.
#pragma warning disable TUnit0001
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
#pragma warning restore TUnit0001

public class RepeatedConvertibleTupleTests
{
    public static IEnumerable<(int, long)> ValueRows() => [(1, 2L)];
    public static IEnumerable<Tuple<int, long>> ReferenceRows() => [Tuple.Create(3, 4L)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, T) row)
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 1L) || !Equals(row.Item2, 2L))
            throw new InvalidOperationException("Expected long inference and converted first element");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, T> row)
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 3L) || !Equals(row.Item2, 4L))
            throw new InvalidOperationException("Expected long inference and converted reference elements");
    }
}
