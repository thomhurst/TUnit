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

public class RepeatedNullableTupleTests
{
    public static IEnumerable<(int, long)> ValueRows() => [(1, 2L)];
    public static IEnumerable<(int, long?)> NullableRows() => [(1, 2L), (1, null)];
    public static IEnumerable<Tuple<int, long>> ReferenceRows() => [Tuple.Create(3, 4L)];
    public static IEnumerable<Tuple<int, long?>> NullableReferenceRows() =>
        [Tuple.Create<int, long?>(3, 4L), Tuple.Create<int, long?>(3, null)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    [MethodDataSource(nameof(NullableRows))]
    public void Value<T>((T, T?) row) where T : struct
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 1L)
            || row.Item2 is not null && !Equals(row.Item2, 2L))
            throw new InvalidOperationException("Expected long inference through nullable element and converted first element");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    [MethodDataSource(nameof(NullableReferenceRows))]
    public void Reference<T>(Tuple<T, T?> row) where T : struct
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 3L)
            || row.Item2 is not null && !Equals(row.Item2, 4L))
            throw new InvalidOperationException("Expected long inference through nullable reference-tuple element");
    }
}

public class RepeatedNestedGenericTupleTests
{
    public static IEnumerable<(int, List<long>)> ValueRows() => [(1, new List<long> { 2 })];
    public static IEnumerable<Tuple<int, List<long>>> ReferenceRows() => [Tuple.Create(3, new List<long> { 4 })];
    public static IEnumerable<(int, long[])> ArrayRows() => [(5, new long[] { 6 })];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, List<T>) row)
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 1L) || !Equals(row.Item2[0], 2L))
            throw new InvalidOperationException("Expected long inference through a nested generic element");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, List<T>> row)
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 3L) || !Equals(row.Item2[0], 4L))
            throw new InvalidOperationException("Expected long inference through a reference tuple generic element");
    }

    [Test]
    [MethodDataSource(nameof(ArrayRows))]
    public void Array<T>((T, T[]) row)
    {
        if (typeof(T) != typeof(long) || !Equals(row.Item1, 5L) || !Equals(row.Item2[0], 6L))
            throw new InvalidOperationException("Expected long inference through an array element");
    }
}

// Ordinary parameters retain their existing inference policy; generic-base traversal is tuple-only.
public class OrdinaryGenericBaseInferenceTests
{
    public class Base<T> { }
    public sealed class Derived : Base<int> { }
    public static IEnumerable<Derived> Rows() => [new Derived()];

    [Test]
    // Intentionally exercises uninferable ordinary generic-base parameters.
#pragma warning disable TUnit0001
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public void Ordinary<T>(Base<T> value) { }
}

public class TupleNestedInterfaceBaseTests
{
    public class Base<T> { }
    public sealed class Derived : Base<int> { }
    public static IEnumerable<(List<Derived>, int)> Rows() => [(new() { new Derived() }, 7)];
    public static IEnumerable<Tuple<List<Derived>, int>> ReferenceRows() => [Tuple.Create(new List<Derived> { new() }, 7)];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void Value<T>((IEnumerable<Base<T>>, int) row)
    {
        if (typeof(T) != typeof(int) || row.Item1.Single() is not Derived || row.Item2 != 7)
            throw new InvalidOperationException("Expected base inference through an interface candidate");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<IEnumerable<Base<T>>, int> row)
    {
        if (typeof(T) != typeof(int) || row.Item1.Single() is not Derived || row.Item2 != 7)
            throw new InvalidOperationException("Expected reference tuple base inference through an interface candidate");
    }
}

public class TupleMultiParameterCandidateTests
{
    public interface IPair<T, U> { }
    public sealed class Dual : IPair<int, int>, IPair<long, string> { }
    public static IEnumerable<(string, Dual)> Rows() => [("value", new())];
    public static IEnumerable<Tuple<string, Dual>> ReferenceRows() => [Tuple.Create("value", new Dual())];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void Value<T, U>((T, IPair<U, T>) row)
    {
        if (typeof(T) != typeof(string) || typeof(U) != typeof(long) || row.Item2 is not Dual)
            throw new InvalidOperationException("Expected rejected candidates to leave no partial type bindings");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T, U>(Tuple<T, IPair<U, T>> row)
    {
        if (typeof(T) != typeof(string) || typeof(U) != typeof(long) || row.Item2 is not Dual)
            throw new InvalidOperationException("Expected reference tuple candidate isolation during invocation");
    }
}

// Cross-family reconstruction applies to whole tuple rows, not ordinary spread columns.
public class SpreadCrossFamilyTupleInferenceTests
{
    public static IEnumerable<(Tuple<int, string>, int)> Rows() => [(Tuple.Create(1, "value"), 7)];

    [Test]
#pragma warning disable TUnit0001 // Intentionally uninferable spread column.
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public void Value<T>((T, string) row, int extra) { }
}

public class TupleClosedCandidateArgumentTests
{
    public interface IPair<T, U> { }
    public sealed class Dual : IPair<int, string>, IPair<long, int> { }
    public static IEnumerable<(string, Dual)> Rows() => [("value", new())];
    public static IEnumerable<Tuple<string, Dual>> ReferenceRows() => [Tuple.Create("value", new Dual())];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void Value<T, U>((T, IPair<U, int>) row)
    {
        if (typeof(T) != typeof(string) || typeof(U) != typeof(long) || row.Item2 is not Dual)
            throw new InvalidOperationException("Expected incompatible closed candidate arguments to be rejected");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T, U>(Tuple<T, IPair<U, int>> row)
    {
        if (typeof(T) != typeof(string) || typeof(U) != typeof(long) || row.Item2 is not Dual)
            throw new InvalidOperationException("Expected reference tuple closed candidate validation");
    }
}

public class SpreadMatchingFamilyTupleInferenceTests
{
    public class Base<T> { }
    public sealed class Derived : Base<int> { }
    public static IEnumerable<((Derived, int), int)> Rows() => [((new(), 1), 7)];

    [Test]
#pragma warning disable TUnit0001 // Intentionally uninferable spread column.
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public void Value<T>((Base<T>, int) row, int extra) { }
}


public class TupleVariantCandidateTests
{
    public interface IPair<T, out U> { }
    public interface IConsumer<T, in U> { }
    public sealed class Source : IPair<int, string>, IConsumer<int, object> { }
    public static IEnumerable<(Source, int)> ValueRows() => [(new Source(), 7)];
    public static IEnumerable<Tuple<Source, int>> ReferenceRows() => [Tuple.Create(new Source(), 7)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((IPair<T, object>, int) row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Source || row.Item2 != 7)
            throw new InvalidOperationException("Expected covariant interface conversion");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<IPair<T, object>, int> row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Source || row.Item2 != 7)
            throw new InvalidOperationException("Expected covariant interface conversion");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ContravariantValue<T>((IConsumer<T, string>, int) row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Source || row.Item2 != 7)
            throw new InvalidOperationException("Expected contravariant interface conversion");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ContravariantReference<T>(Tuple<IConsumer<T, string>, int> row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Source || row.Item2 != 7)
            throw new InvalidOperationException("Expected contravariant interface conversion");
    }
}


public class TupleInvariantBaseCandidateTests
{
    public interface IBox<T> { }
    public class Base<T, U> { }
    public sealed class Derived : Base<int, string> { }
    public sealed class Dual : IBox<Derived>, IBox<Base<long, object>> { }
    public static IEnumerable<(Dual, int)> ValueRows() => [(new Dual(), 7)];
    public static IEnumerable<Tuple<Dual, int>> ReferenceRows() => [Tuple.Create(new Dual(), 7)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((IBox<Base<T, object>>, int) row)
    {
        if (typeof(T) != typeof(long) || row.Item1 is not Dual || row.Item2 != 7)
            throw new InvalidOperationException("Expected compatible invariant base candidate");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<IBox<Base<T, object>>, int> row)
    {
        if (typeof(T) != typeof(long) || row.Item1 is not Dual || row.Item2 != 7)
            throw new InvalidOperationException("Expected compatible invariant base candidate");
    }
}


public class TupleConstrainedCandidateTests
{
    public interface IFoo { }
    public sealed class Foo : IFoo { }
    public interface IPair<A, B> where A : B { }
    public sealed class Dual : IPair<string, object>, IPair<Foo, IFoo> { }
    public static IEnumerable<(Dual, int)> ValueRows() => [(new Dual(), 7)];
    public static IEnumerable<Tuple<Dual, int>> ReferenceRows() => [Tuple.Create(new Dual(), 7)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((IPair<T, IFoo>, int) row) where T : IFoo
    {
        if (typeof(T) != typeof(Foo) || row.Item1 is not Dual || row.Item2 != 7)
            throw new InvalidOperationException("Expected constraint-invalid candidate to be skipped");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<IPair<T, IFoo>, int> row) where T : IFoo
    {
        if (typeof(T) != typeof(Foo) || row.Item1 is not Dual || row.Item2 != 7)
            throw new InvalidOperationException("Expected reference tuple constraint-invalid candidate to be skipped");
    }
}

// Singleton tuple reconstruction has the analyzer false positive tracked by #7622.
#pragma warning disable TUnit0001
public class SingletonTupleInferenceTests
{
    public static IEnumerable<Tuple<int>> ReferenceRows() => [Tuple.Create(7)];
    public static IEnumerable<ValueTuple<int>> ValueRows() => [new(7)];

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceToValue<T>(ValueTuple<T> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 7))
            throw new InvalidOperationException("Expected singleton reference-to-value tuple reconstruction");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueToReference<T>(Tuple<T> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 7))
            throw new InvalidOperationException("Expected singleton value-to-reference tuple reconstruction");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueToValue<T>(ValueTuple<T> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 7))
            throw new InvalidOperationException("Expected singleton value tuple inference");
    }
}

#pragma warning restore TUnit0001


public class OpenTupleRuntimeCastTests
{
    public static IEnumerable<(int, object)> ValueRows() => [(1, new List<int> { 7 })];
    public static IEnumerable<Tuple<int, object>> ReferenceRows() => [Tuple.Create(1, (object)new List<int> { 7 })];
    public static IEnumerable<(object, int)> ReversedRows() => [(new List<int> { 7 }, 1)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, List<T>) row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 1) || !Equals(row.Item2.Single(), 7))
            throw new InvalidOperationException("Expected open element runtime cast after inference");
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, List<T>> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 1) || !Equals(row.Item2.Single(), 7))
            throw new InvalidOperationException("Expected reference tuple open element runtime cast");
    }

    [Test]
    [MethodDataSource(nameof(ReversedRows))]
    public void Reversed<T>((List<T>, T) row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1.Single(), 7) || !Equals(row.Item2, 1))
            throw new InvalidOperationException("Expected inference from a later element after a runtime cast");
    }

    public static IEnumerable<(int, IEnumerable<int>)> InterfaceRows() => [(1, new List<int> { 7 })];
    public static IEnumerable<Tuple<int, IEnumerable<int>>> ReferenceInterfaceRows() =>
        [Tuple.Create(1, (IEnumerable<int>)new List<int> { 7 })];
    public static IEnumerable<(IEnumerable<int>, int)> ReversedInterfaceRows() => [(new List<int> { 7 }, 1)];
    public class Base { }
    public sealed class Derived<T> : Base { }
    public static IEnumerable<(int, Base)> BaseRows() => [(1, new Derived<int>())];

    [Test]
    [MethodDataSource(nameof(InterfaceRows))]
    public void Interface<T>((T, List<T>) row) => Value(row);

    [Test]
    [MethodDataSource(nameof(ReferenceInterfaceRows))]
    public void ReferenceInterface<T>(Tuple<T, List<T>> row) => Reference(row);

    [Test]
    [MethodDataSource(nameof(ReversedInterfaceRows))]
    public void ReversedInterface<T>((List<T>, T) row) => Reversed(row);

    [Test]
    [MethodDataSource(nameof(BaseRows))]
    public void BaseCast<T>((T, Derived<T>) row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1, 1) || row.Item2 is null)
            throw new InvalidOperationException("Expected base-typed element runtime cast after inference");
    }

}


#pragma warning disable TUnit0001 // #7622: analyzer tuple reconstruction and whole-row matching.
public class NestedSingletonBoundaryTests
{
    public static IEnumerable<Tuple<ValueTuple<int>>> Rows() => [Tuple.Create(new ValueTuple<int>(7))];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void ReferenceOuter(Tuple<Tuple<int>> row)
    {
        if (row.Item1.Item1 != 7)
            throw new InvalidOperationException("Expected nested singleton reconstruction");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void ValueOuter(ValueTuple<Tuple<int>> row)
    {
        if (row.Item1.Item1 != 7)
            throw new InvalidOperationException("Expected preserved whole singleton reconstruction");
    }
}

public class WrappedNestedSingletonInferenceTests
{
    public static IEnumerable<TestDataRow<Tuple<ValueTuple<int>>>> Rows() =>
        [new(Tuple.Create(new ValueTuple<int>(7)))];
    public static IEnumerable<TestDataRow<ValueTuple<Tuple<int>>>> ValueRows() =>
        [new(new ValueTuple<Tuple<int>>(Tuple.Create(7)))];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void ReferenceOuter<T>(Tuple<Tuple<T>> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1.Item1, 7))
            throw new InvalidOperationException("Expected wrapped nested singleton inference");
    }

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueOuter<T>(ValueTuple<ValueTuple<T>> row)
    {
        if (typeof(T) != typeof(int) || !Equals(row.Item1.Item1, 7))
            throw new InvalidOperationException("Expected wrapped value nested singleton inference");
    }
}

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


// #7622: analyzer does not model deferred tuple-element runtime casts.
#pragma warning disable TUnit0001
public class OpenTupleArrayRuntimeCastTests
{
    public static IEnumerable<(int, object)> ValueRows() => [(1, new[] { 7 })];
    public static IEnumerable<Tuple<int, object>> ReferenceRows() => [Tuple.Create(1, (object)new[] { 7 })];
    public static IEnumerable<(int, IEnumerable<int>)> InterfaceRows() => [(1, new[] { 7 })];
    public static IEnumerable<(object, int)> ReversedRows() => [(new[] { 7 }, 1)];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, T[]) row) => Verify(row.Item1, row.Item2);

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, T[]> row) => Verify(row.Item1, row.Item2);

    [Test]
    [MethodDataSource(nameof(InterfaceRows))]
    public void Interface<T>((T, T[]) row) => Verify(row.Item1, row.Item2);

    [Test]
    [MethodDataSource(nameof(ReversedRows))]
    public void Reversed<T>((T[], T) row) => Verify(row.Item2, row.Item1);

    private static void Verify<T>(T first, T[] array)
    {
        if (typeof(T) != typeof(int) || !Equals(first, 1) || array.Length != 1 || !Equals(array[0], 7))
            throw new InvalidOperationException("Expected array runtime cast after tuple inference");
    }
}

#pragma warning restore TUnit0001

public class NestedTupleCandidateBindingTests
{
    public interface IBox<T> { }
    public sealed class Dual : IBox<(int, int)>, IBox<(long, string)> { }
    public sealed class NestedDual : IBox<Tuple<(int, int)>>, IBox<Tuple<(long, string)>> { }

    public static IEnumerable<(string, Dual)> ValueRows() => [("value", new Dual())];
    public static IEnumerable<Tuple<string, Dual>> ReferenceRows() => [Tuple.Create("value", new Dual())];
    public static IEnumerable<(string, NestedDual)> NestedValueRows() => [("value", new NestedDual())];
    public static IEnumerable<Tuple<string, NestedDual>> NestedReferenceRows() => [Tuple.Create("value", new NestedDual())];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void Value<T, U>((T, IBox<(U, T)>) row)
    {
        Verify<T, U>(row.Item1);
    }

    [Test]
    [MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T, U>(Tuple<T, IBox<(U, T)>> row)
    {
        Verify<T, U>(row.Item1);
    }

    [Test]
    [MethodDataSource(nameof(NestedValueRows))]
    public void NestedValue<T, U>((T, IBox<Tuple<(U, T)>>) row)
    {
        Verify<T, U>(row.Item1);
    }

    [Test]
    [MethodDataSource(nameof(NestedReferenceRows))]
    public void NestedReference<T, U>(Tuple<T, IBox<Tuple<(U, T)>>> row)
    {
        Verify<T, U>(row.Item1);
    }

    private static void Verify<T, U>(T value)
    {
        if (typeof(T) != typeof(string) || typeof(U) != typeof(long) || !Equals(value, "value"))
            throw new InvalidOperationException("Expected nested candidate to preserve the string binding and infer long");
    }
}

// #7622: analyzer spreads whole tuple rows with optional or collecting tails.
#pragma warning disable TUnit0001
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

#pragma warning restore TUnit0001

// #7622: analyzer spreads whole tuple rows before explicit method-type inference.
#pragma warning disable TUnit0001
public class ExplicitCrossFamilyTupleInferenceTests
{
    public static IEnumerable<Func<Tuple<int, string>>> ReferenceRows() => [() => Tuple.Create(1, "tuple")];
    public static IEnumerable<Func<(int, string)>> ValueRows() => [() => (1, "tuple")];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceToValue<T>((T, string) row)
    {
        Verify(row.Item1, row.Item2);
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceToReference<T>(Tuple<T, string> row)
    {
        Verify(row.Item1, row.Item2);
    }

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ValueRows))]
    public void ValueToReference<T>(Tuple<T, string> row)
    {
        Verify(row.Item1, row.Item2);
    }

    private static void Verify<T>(T value, string text)
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(long)
            || value is not (int or long) || Convert.ToInt64(value) != 1 || text != "tuple")
            throw new InvalidOperationException("Expected both explicit long and inferred int tuple instantiations");
    }
}

#pragma warning restore TUnit0001

// #7622: analyzer spreads whole reference-tuple rows for a bare type parameter.
#pragma warning disable TUnit0001
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
#pragma warning restore TUnit0001

// #7622: analyzer spreads whole tuple rows before explicit method-type inference.
#pragma warning disable TUnit0001
public class ExplicitDirectTupleInferenceTests
{
    public static IEnumerable<(int, string)> ValueRows() => [(1, "tuple")];
    public static IEnumerable<Tuple<int, string>> ReferenceRows() => [Tuple.Create(1, "tuple")];

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ValueRows))]
    public void Value<T>((T, string) row) => Verify(row.Item1, row.Item2);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ReferenceRows))]
    public void Reference<T>(Tuple<T, string> row) => Verify(row.Item1, row.Item2);

    [Test, GenerateGenericTest(typeof(long)), MethodDataSource(nameof(ReferenceRows))]
    public void ReferenceToValue<T>((T, string) row) => Verify(row.Item1, row.Item2);

    private static void Verify<T>(T value, string text)
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(long)
            || value is not (int or long) || Convert.ToInt64(value) != 1 || text != "tuple")
            throw new InvalidOperationException("Expected both explicit long and inferred int direct tuple instantiations");
    }
}
#pragma warning restore TUnit0001
