namespace TUnit.TestProject.Bugs._7222;

[Arguments<int>(1)]
public class GenericInstanceSourceTests<TClass>(TClass value)
{
    public IEnumerable<string> Rows()
    {
        yield return value!.ToString()!;
        yield return "b";
    }

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    [InstanceMethodDataSource(nameof(MoreRows))]
    public void Valid<T>(T item) where T : class
    {
    }

    public IEnumerable<string> MoreRows() => ["extra"];

    public IEnumerable<Tuple<int, string>> TupleRows() => [Tuple.Create(1, "tuple")];

    [Test]
    [InstanceMethodDataSource(nameof(TupleRows))]
    public void ReferenceTuple<T>(T first, string second)
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected int");
    }

    [Test]
    [InstanceMethodDataSource(nameof(TupleRows))]
    public void PreservedTuple<T>(Tuple<T, string> row)
    {
        if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected int");
    }

    // Intentional failures: checked in both modes by Issue7222Tests.
    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void StructOnly<T>(T item) where T : struct
    {
    }
}

[Arguments(1)]
public class NonGenericInstanceSourceTests(int value)
{
    public IEnumerable<int> Rows() => [value, 2];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void Valid<T>(T item) where T : struct
    {
    }
}

public class WholeReferenceTupleSourceTests
{
    public static IEnumerable<Tuple<int, string>> Rows() => [Tuple.Create(1, "tuple")];

    [Test]
    [GenerateGenericTest(typeof(Tuple<int, string>))]
    // #7622: the analyzer spreads the row before substituting the explicit generic type.
#pragma warning disable TUnit0001
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public void WholeRow<T>(T row)
    {
        if (row is not Tuple<int, string> { Item1: 1, Item2: "tuple" })
            throw new InvalidOperationException("Expected the whole reference tuple");
    }
}

[GenerateGenericTest(typeof(int))]
public class ExplicitClassInstanceSourcesTests<TClass>
{
    public IEnumerable<string> Rows() => ["first"];
    public IEnumerable<string> MoreRows() => ["second"];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    [InstanceMethodDataSource(nameof(MoreRows))]
    public void BothSources<T>(T item) where T : class
    {
        if (item is not string) throw new InvalidOperationException("Expected string");
    }
}

public class InferredReferenceTupleSourceTests
{
    public IEnumerable<Tuple<int>> InstanceRows() => [Tuple.Create(1)];
    public static IEnumerable<Tuple<int>> StaticRows() => [Tuple.Create(2)];

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void InstanceElement<T>(T value) where T : struct
    {
        if (value is not int) throw new InvalidOperationException("Expected int");
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    public void StaticElement<T>(T value) where T : struct
    {
        if (value is not int) throw new InvalidOperationException("Expected int");
    }
}

[Arguments<int>(1)]
public class MixedClassRowSourcesTests<TClass>(TClass value)
{
    public IEnumerable<string> InstanceRows() => [value!.ToString()!];
    public static IEnumerable<string> StaticRows() => ["static"];

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    [MethodDataSource(nameof(StaticRows))]
    public void InstanceFirst<T>(T item) where T : class
    {
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void StaticFirst<T>(T item) where T : class
    {
    }
}

[GenerateGenericTest(typeof(int))]
public class MixedExplicitClassSourcesTests<TClass>
{
    public IEnumerable<string> InstanceRows() => ["instance"];
    public static IEnumerable<string> StaticRows() => ["static"];

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    [MethodDataSource(nameof(StaticRows))]
    public void InstanceFirst<T>(T item) where T : class
    {
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void StaticFirst<T>(T item) where T : class
    {
    }
}

// #7622: the analyzer spreads reference tuples instead of binding the whole first parameter.
#pragma warning disable TUnit0001
public class WholeTupleWithTrailingParametersTests
{
    public static IEnumerable<Tuple<int, string>> StaticRows() => [Tuple.Create(1, "tuple")];
    public IEnumerable<Tuple<int, string>> InstanceRows() => StaticRows();

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void Optional<T>(Tuple<T, string> row, int extra = 5)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 5)
            throw new InvalidOperationException("Expected whole tuple and default value");
    }

    [Test]
    [MethodDataSource(nameof(StaticRows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public void Params<T>(Tuple<T, string> row, params int[] extra)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra.Length != 0)
            throw new InvalidOperationException("Expected whole tuple and empty params");
    }


}

#pragma warning restore TUnit0001

public class NestedTupleWithTrailingParametersTests
{
    public static IEnumerable<((int, string), int)> ValueRows() => [((1, "tuple"), 7)];
    public IEnumerable<Tuple<Tuple<int, string>, int>> ReferenceRows() => [Tuple.Create(Tuple.Create(1, "tuple"), 7)];

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
    [InstanceMethodDataSource(nameof(ReferenceRows))]
    public void ReferenceOptional<T>(Tuple<T, string> row, int extra = 5) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple" || extra != 7)
            throw new InvalidOperationException("Expected nested tuple and supplied column");
    }

    public IEnumerable<Tuple<Tuple<int, string>, int[]>> ReferenceParamsRows() => [Tuple.Create(Tuple.Create(1, "tuple"), new[] { 7 })];

    [Test]
    [InstanceMethodDataSource(nameof(ReferenceParamsRows))]
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
    public IEnumerable<Tuple<Tuple<int, int>, Tuple<int, string>>> ReferenceRows() =>
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
    [InstanceMethodDataSource(nameof(ReferenceRows))]
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
    public IEnumerable<Tuple<Tuple<int, object>, string>> ReferenceRows() =>
        [Tuple.Create(Tuple.Create(1, new object()), "extra")];
    public IEnumerable<Tuple<int, string>> CrossFamilyRows() => [Tuple.Create(1, "tuple")];

    [Test]
    [MethodDataSource(nameof(ValueRows))]
    public void ValueAssignable<T>((T, object) row, string extra = "")
    {
        if (typeof(T) != typeof(int) || extra != "extra")
            throw new InvalidOperationException("Expected separate columns and int inference");
    }

    [Test]
    [InstanceMethodDataSource(nameof(ReferenceRows))]
    public void ReferenceAssignable<T>(Tuple<T, object> row, string extra = "")
    {
        if (typeof(T) != typeof(int) || extra != "extra")
            throw new InvalidOperationException("Expected separate columns and int inference");
    }

    [Test]
    [InstanceMethodDataSource(nameof(CrossFamilyRows))]
    public void CrossFamily<T>((T, string) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != "tuple")
            throw new InvalidOperationException("Expected logical tuple element inference");
    }
}

public class SingleTupleElementConversionTests
{
    public static IEnumerable<(int, int)> Rows() => [(1, 2)];

    public IEnumerable<Tuple<int, int>> NarrowReferenceRows() => [Tuple.Create(3, 4)];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void NarrowNumericElement<T>((T, short) row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 2)
            throw new InvalidOperationException("Expected narrowing numeric element conversion");
    }

    [Test]
    [InstanceMethodDataSource(nameof(NarrowReferenceRows))]
    public void NarrowNumericReferenceElement<T>(Tuple<T, short> row)
    {
        if (typeof(T) != typeof(int) || row.Item2 != 4)
            throw new InvalidOperationException("Expected narrowing numeric reference element conversion");
    }

    public class Base<T> { }
    public class Derived : Base<int> { }

    public static IEnumerable<(Derived, int)> BaseRows() => [(new Derived(), 7)];
    public IEnumerable<Tuple<Derived, int>> BaseReferenceRows() => [Tuple.Create(new Derived(), 8)];

    [Test]
    [MethodDataSource(nameof(BaseRows))]
    public void BaseElement<T>((Base<T>, T) row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Derived || !Equals(row.Item2, 7))
            throw new InvalidOperationException("Expected generic base-class element conversion");
    }

    [Test]
    [InstanceMethodDataSource(nameof(BaseReferenceRows))]
    public void BaseReferenceElement<T>(Tuple<Base<T>, T> row)
    {
        if (typeof(T) != typeof(int) || row.Item1 is not Derived || !Equals(row.Item2, 8))
            throw new InvalidOperationException("Expected generic base-class reference element conversion");
    }

    public static IEnumerable<(int[], int)> InterfaceRows() => [(new[] { 5 }, 5)];
    public IEnumerable<Tuple<List<int>, int>> InterfaceReferenceRows() => [Tuple.Create(new List<int> { 6 }, 6)];

    [Test]
    [MethodDataSource(nameof(InterfaceRows))]
    public void InterfaceElement<T>((IEnumerable<T>, T) row)
    {
        if (typeof(T) != typeof(int) || !row.Item1.SequenceEqual(new[] { row.Item2 }))
            throw new InvalidOperationException("Expected generic interface element conversion");
    }

    [Test]
    [InstanceMethodDataSource(nameof(InterfaceReferenceRows))]
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
    public IEnumerable<Tuple<int, string>> OpenNullableReferenceRows() => [Tuple.Create(4, "nullable")];

    [Test]
    [MethodDataSource(nameof(OpenNullableRows))]
    public void OpenNullableElement<T>((T?, string) row) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item1 is not int value || value != 3 || row.Item2 != "nullable")
            throw new InvalidOperationException("Expected open nullable element inference");
    }

    [Test]
    [InstanceMethodDataSource(nameof(OpenNullableReferenceRows))]
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

[GenerateGenericTest(typeof(int))]
public class ClosedClassTupleInferenceTests<TClass>
{
    public static IEnumerable<((int, string), int)> Rows() => [((1, "text"), 7)];

    public static IEnumerable<(TClass, string)> NullableClassRows() => [(default!, "class")];
    public IEnumerable<Tuple<TClass, string>> NullableClassReferenceRows() => [Tuple.Create(default(TClass)!, "class")];

    [Test]
    [MethodDataSource(nameof(NullableClassRows))]
    public void NullableClass<T>((T?, string) row) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item1 is not int value || value != 0 || row.Item2 != "class")
            throw new InvalidOperationException("Expected nullable inference after closing the class");
    }

    [Test]
    [InstanceMethodDataSource(nameof(NullableClassReferenceRows))]
    public void NullableReferenceClass<T>(Tuple<T?, string> row) where T : struct
    {
        if (typeof(T) != typeof(int) || row.Item1 is not int value || value != 0 || row.Item2 != "class")
            throw new InvalidOperationException("Expected nullable reference inference after closing the class");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void ClosedClass<T>((TClass, T) row, int extra = 5)
    {
        if (typeof(T) != typeof(string) || row.Item2 is not "text" || extra != 7)
            throw new InvalidOperationException("Expected closed class types before tuple matching");
    }
}
