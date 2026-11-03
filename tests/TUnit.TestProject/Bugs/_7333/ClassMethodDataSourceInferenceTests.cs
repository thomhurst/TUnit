namespace TUnit.TestProject.Bugs._7333;

public static class Rows
{
    public static IEnumerable<int> Integers() => [3];
    public static IEnumerable<int> MoreIntegers() => [4];
    public static Task<IEnumerable<Func<string>>> Strings() => Task.FromResult<IEnumerable<Func<string>>>([() => "three"]);
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class ExternalRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That(value).IsEqualTo((T)(object)3);
}

[MethodDataSource(nameof(Integers))]
public class OwnRows<T>(T value)
{
    public static IEnumerable<int> Integers() => [3];
    [Test]
    public async Task Test() => await Assert.That(value).IsEqualTo((T)(object)3);
}

[MethodDataSource(typeof(Rows), nameof(Rows.Strings))]
public class AsyncFactoryRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That(value).IsEqualTo((T)(object)"three");
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
[MethodDataSource(typeof(Rows), nameof(Rows.MoreIntegers))]
public class MultipleSources<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsNotNull();
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class GenericMethod<T>(T value)
{
    [Test]
    [Arguments("method")]
    [Arguments(5)]
    public async Task Test<U>(U methodValue)
    {
        await Assert.That(value).IsEqualTo((T)(object)3);
        await Assert.That((object?)methodValue).IsNotNull();
    }
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class MethodRows<T>(T value)
{
    [Test]
    [Arguments("first")]
    [Arguments("second")]
    public async Task Test(string methodValue)
    {
        await Assert.That(value).IsEqualTo((T)(object)3);
        await Assert.That((object?)methodValue).IsNotNull();
    }
}

// Discovery must report a class whose own member cannot supply its type arguments.
[MethodDataSource(nameof(Values))]
public class UninferableOwnRows<T>(T value)
{
    public static IEnumerable<T> Values() => [];
    [Test]
    public void Test() => _ = value;
}

// The int class row has no matching method row and must not create an argument-free test.
[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class UnmatchedMethodRows<T>(T value)
{
    [Test]
    [Arguments("method")]
    public async Task Test(T methodValue)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(methodValue).IsEqualTo((T)(object)"method");
        _ = value;
    }
}

// A class-inferred entry must retain its method source without any method [Arguments].
[MethodDataSource(nameof(Integers))]
public class ClassAndMethodSources<T>(T value)
{
    public static IEnumerable<int> Integers() => [3, 4];

    [Test]
    [MethodDataSource(nameof(Integers))]
    public async Task Test(T methodValue)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsNotNull();
        await Assert.That((object?)methodValue).IsNotNull();
    }
}

public static class PolymorphicRows
{
    public static IEnumerable<IComparable> Integers() => [2];
    public static IEnumerable<int?> NullableIntegers() => [2];
}

// The declared interface must not replace inference from the integer row value.
[Arguments(1)]
[MethodDataSource(typeof(PolymorphicRows), nameof(PolymorphicRows.Integers))]
public class PolymorphicExternalRows<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsNotNull();
    }
}

public static class ArrayRows
{
    public static IEnumerable<int[]> Integers() => [new[] { 3 }];
    public static IEnumerable<string[]> Strings() => [new[] { "three" }];
    public static IEnumerable<int?[]> NullableIntegers() => [new int?[] { 3, null }];
    public static IEnumerable<int[][]> NestedIntegers() => [new[] { new[] { 3 } }];
    public static IEnumerable<object[]> CovariantStrings() => [new[] { "source" }];
}

[MethodDataSource(typeof(ArrayRows), nameof(ArrayRows.Integers))]
public class ExternalArrayRows<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int[]));
        await Assert.That(((int[])(object)value!)[0]).IsEqualTo(3);
    }
}

[MethodDataSource(typeof(ArrayRows), nameof(ArrayRows.Strings))]
public class ExternalSealedArrayRows<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string[]));
        await Assert.That(((string[])(object)value!)[0]).IsEqualTo("three");
    }
}

[MethodDataSource(typeof(ArrayRows), nameof(ArrayRows.NestedIntegers))]
public class ExternalNestedArrayRows<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int[][]));
        await Assert.That(((int[][])(object)value!)[0][0]).IsEqualTo(3);
    }
}

// object[] can hold a string[] row, so retain runtime partitioning for covariant arrays.
[MethodDataSource(nameof(Strings))]
[MethodDataSource(typeof(ArrayRows), nameof(ArrayRows.CovariantStrings))]
public class CovariantExternalArrayRows<T>(T value)
{
    public static IEnumerable<string[]> Strings() => [new[] { "seed" }];

    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string[]));
        await Assert.That(((string[])(object)value!)[0]).IsNotNull();
    }
}

// A nullable value boxes as its underlying type when reflection infers the class types.
[Arguments(1)]
[MethodDataSource(typeof(PolymorphicRows), nameof(PolymorphicRows.NullableIntegers))]
public class NullableExternalRows<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsNotNull();
    }
}

// Nullable array elements stay inside the array rather than being boxed as class rows.
[MethodDataSource(typeof(ArrayRows), nameof(ArrayRows.NullableIntegers))]
public class ExternalNullableArrayRows<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int?[]));
        await Assert.That(((int?[])(object)value!)[0]).IsEqualTo(3);
        await Assert.That(((int?[])(object)value!)[1]).IsNull();
    }
}

// Inference extracts T from T[], but only the supplied array is boxed as a row.
[MethodDataSource(typeof(ArrayRows), nameof(ArrayRows.NullableIntegers))]
public class ExternalNullableArrayParameterRows<T>(T[] values)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int?));
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That((object?)values[0]).IsEqualTo(3);
        await Assert.That((object?)values[1]).IsNull();
    }
}

public static class AdditionalExternalRows
{
    public static IEnumerable<(int, string)> PositionalRows() => [(2, "3")];
    public static IEnumerable<string?> NullRows() => [null];
    public static IEnumerable<(int, object)> UnrelatedColumns() => [(3, "other")];
}

// Fixed external row types match constructor positions, not unrelated leading columns.
[Arguments(1, 2)]
[MethodDataSource(typeof(AdditionalExternalRows), nameof(AdditionalExternalRows.PositionalRows))]
public class ExternalConstructorPositionRows<T>(int id, T value)
{
    [Test]
    public async Task Test()
    {
        if (typeof(T) == typeof(int))
        {
            await Assert.That(id).IsEqualTo(1);
            await Assert.That((object?)value).IsEqualTo(2);
        }
        else
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(string));
            await Assert.That(id).IsEqualTo(2);
            await Assert.That((object?)value).IsEqualTo("3");
        }
    }
}

// A typed external source must retain runtime filtering of its all-null rows.
[Arguments("seed")]
[MethodDataSource(typeof(AdditionalExternalRows), nameof(AdditionalExternalRows.NullRows))]
public class ExternalNullRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsEqualTo("seed");
}

// Generic provider attributes retain external null-row filtering in generated code.
[Arguments("seed")]
[MethodDataSource<GenericExternalProvider>(nameof(GenericExternalProvider.NullRows))]
public class GenericProviderNullRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsEqualTo("seed");
}

// An unrelated polymorphic column does not determine any class type argument.
[MethodDataSource(typeof(AdditionalExternalRows), nameof(AdditionalExternalRows.UnrelatedColumns))]
public class ExternalUnrelatedColumnRows<T>(T value, object other)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(3);
        await Assert.That(other).IsEqualTo("other");
    }
}


public sealed class FreshResource : IDisposable
{
    private int _used;

    public void Use()
    {
        if (Interlocked.Exchange(ref _used, 1) != 0)
        {
            throw new InvalidOperationException("A factory resource was reused or already disposed.");
        }
    }

    public void Dispose() => Interlocked.Exchange(ref _used, 1);
}

public static class ReviewRows
{
    public static IEnumerable<Func<FreshResource>> Resources() => [() => new FreshResource()];
    public static IEnumerable<int> EmptyIntegers() => [];
}

// Each method row must fetch a fresh factory value, including after the first is disposed.
[MethodDataSource(typeof(ReviewRows), nameof(ReviewRows.Resources))]
public class ExternalFreshFactoryRows<T>(T value) : IDisposable
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [NotInParallel]
    public void Test(int methodRow)
    {
        _ = methodRow;
        ((FreshResource)(object)value!).Use();
    }

    public void Dispose() => ((IDisposable)value!).Dispose();
}

// An empty external source adds no row to the explicit seed, even for value types.
[Arguments(1)]
[MethodDataSource(typeof(ReviewRows), nameof(ReviewRows.EmptyIntegers))]
public class ExternalEmptyValueRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsEqualTo(1);
}

// Without a default or explicit constructor, use the overload that inferred the class types.
#pragma warning disable TUnit0052 // Deliberately exercises implicit constructor selection.
[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class ExternalOverloadedConstructorRows<T>
{
    private readonly T _value;

    public ExternalOverloadedConstructorRows(T value) => _value = value;

    public ExternalOverloadedConstructorRows(T value, string label)
        => throw new InvalidOperationException("The longer constructor does not match the row.");

    [Test]
    public async Task Test() => await Assert.That((object?)_value).IsEqualTo(3);
}

#pragma warning restore TUnit0052

// Empty own-class sources add no fallback row, while valid null rows still run.
[Arguments(1)]
[MethodDataSource(nameof(Rows))]
public class OwnEmptyValueRows<T>(T value)
{
    public static IEnumerable<int> Rows() => [];

    [Test]
    public async Task Test() => await Assert.That((object?)value).IsEqualTo(1);
}

[MethodDataSource(nameof(Rows))]
public class OwnNullRows<T>(T value)
{
    public static IEnumerable<string?> Rows() => [null];

    [Test]
    public async Task Test() => await Assert.That((object?)value).IsNull();
}

public class BaseRow;
public sealed class DerivedRow : BaseRow;

public static class UnsealedRows
{
    public static IEnumerable<BaseRow> Rows() => [new DerivedRow()];
}

// An unsealed declared row type cannot replace the runtime type of the derived value.
[MethodDataSource(nameof(Seeds))]
[MethodDataSource(typeof(UnsealedRows), nameof(UnsealedRows.Rows))]
public class UnsealedExternalRows<T>(T value)
{
    public static IEnumerable<DerivedRow> Seeds() => [new DerivedRow()];

    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(DerivedRow));
        await Assert.That((object?)value).IsTypeOf<DerivedRow>();
    }
}

#pragma warning disable TUnit0001 // Rows infer T even when construction takes no arguments.
// The value overload determines T, but the default constructor initializes the instance.
#pragma warning disable TUnit0052 // Deliberately exercises implicit constructor selection.
[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class ExternalParameterlessConstructorRows<T>
{
    public ExternalParameterlessConstructorRows() { }

    public ExternalParameterlessConstructorRows(T value)
        => throw new InvalidOperationException("This overload is for type inference only.");

    [Test]
    public async Task Test() => await Assert.That(typeof(T)).IsEqualTo(typeof(int));
}

[MethodDataSource(nameof(Rows))]
public class OwnParameterlessConstructorRows<T>
{
    public static IEnumerable<int> Rows() => [3];

    public OwnParameterlessConstructorRows() { }

    public OwnParameterlessConstructorRows(T value)
        => throw new InvalidOperationException("This overload is for type inference only.");

    [Test]
    public async Task Test() => await Assert.That(typeof(T)).IsEqualTo(typeof(int));
}
#pragma warning restore TUnit0052

// An explicit constructor still takes priority over the parameterless overload.
[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class ExplicitConstructorRows<T>
{
    private readonly T _value;

    public ExplicitConstructorRows()
        => throw new InvalidOperationException("The explicit constructor must take priority.");

    [TestConstructor]
    public ExplicitConstructorRows(T value) => _value = value;

    [Test]
    public async Task Test() => await Assert.That((object?)_value).IsEqualTo(3);
}

#pragma warning restore TUnit0001

public sealed class GenericExternalProvider
{
    public static IEnumerable<string?> NullRows() => [null];
}

// Explicit types retain null rows even when source inference creates the entry first.
[GenerateGenericTest(typeof(string))]
[MethodDataSource(typeof(GenericExternalProvider), nameof(GenericExternalProvider.NullRows))]
public class ExplicitExternalNullRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsNull();
}

// Only the method row infers string: retain the raw empty class row so construction
// reports its missing argument, rather than silently dropping the test.
[MethodDataSource(typeof(ReviewRows), nameof(ReviewRows.EmptyIntegers))]
public class MethodOnlyEmptyClassRows<T>
{
    public MethodOnlyEmptyClassRows(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }
    }

    [Test]
    [Arguments("method")]
    public void ReportsMissingConstructorValue(T argument) => _ = argument;
}

// Method rows merge the explicit types into the filtered class-row group.
[GenerateGenericTest(typeof(string))]
[MethodDataSource(typeof(ExplicitMethodRowsProvider), nameof(ExplicitMethodRowsProvider.Rows))]
public class ExplicitExternalNullRowsWithMethodRows<T>(T value)
{
    [Test]
    [Arguments("method")]
    public async Task Test(T argument)
    {
        await Assert.That((object?)value).IsEqualTo("seed");
        await Assert.That((object?)argument).IsEqualTo("method");
    }
}

public static class ExplicitMethodRowsProvider
{
    public static IEnumerable<string?> Rows() => ["seed", null];
}

// Without a non-null class row, reflection uses the explicit instantiation's raw source.
[GenerateGenericTest(typeof(string))]
[MethodDataSource(typeof(GenericExternalProvider), nameof(GenericExternalProvider.NullRows))]
public class ExplicitExternalOnlyNullRowsWithMethodRows<T>(T value)
{
    [Test]
    [Arguments("method")]
    public async Task Test(T argument)
    {
        await Assert.That((object?)value).IsNull();
        await Assert.That((object?)argument).IsEqualTo("method");
    }
}

[GenerateGenericTest(typeof(string))]
[MethodDataSource(nameof(Rows))]
public class ExplicitOwnNullRowsWithMethodRows<T>(T value)
{
    public static IEnumerable<string?> Rows() => ["seed", null];

    [Test]
    [Arguments("method")]
    public async Task Test(T argument)
    {
        await Assert.That((object?)value is null or "seed").IsTrue();
        await Assert.That((object?)argument).IsEqualTo("method");
    }
}

[GenerateGenericTest(typeof(int))]
[MethodDataSource(typeof(ExplicitArrayRowsProvider), nameof(ExplicitArrayRowsProvider.Rows))]
public class ExplicitArrayRowsWithMethodRows<T>(T[] values)
{
    [Test]
    [Arguments(3)]
    public async Task Test(T argument)
    {
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That((object?)values[0]).IsEqualTo(1);
        await Assert.That((object?)argument).IsEqualTo(3);
    }
}

public static class ExplicitArrayRowsProvider
{
    public static IEnumerable<int[]?> Rows() => [new[] { 1 }, null];
}

[GenerateGenericTest(typeof(int))]
[Arguments("tag", 1)]
[Arguments(2, 3)]
public class ExplicitConstructorMappedArguments<T>(object tag, T value)
{
    [Test]
    [Arguments(4)]
    public async Task Test(T argument)
    {
        await Assert.That((object?)value is 1 or 3).IsTrue();
        await Assert.That(tag is "tag" or 2).IsTrue();
        await Assert.That((object?)argument).IsEqualTo(4);
    }
}

[GenerateGenericTest(typeof(string))]
[Arguments("seed")]
[MethodDataSource(nameof(Rows))]
public class ExplicitDeferredOwnNullRows<T>(T value)
{
    public static IEnumerable<T?> Rows() => [default];

    [Test]
    [Arguments("method")]
    public async Task Test(T argument)
    {
        await Assert.That((object?)value is null or "seed").IsTrue();
        await Assert.That((object?)argument).IsEqualTo("method");
    }
}

[GenerateGenericTest(typeof(string))]
[MethodDataSource(typeof(ExplicitMethodRowsProvider), nameof(ExplicitMethodRowsProvider.Rows))]
public class ExplicitExternalNullRowsWithGenericMethod<T>(T value)
{
    [Test]
    [Arguments(1)]
    public async Task Test<U>(U argument)
    {
        await Assert.That((object?)value).IsEqualTo("seed");
        await Assert.That((object?)argument).IsEqualTo(1);
    }
}

[MethodDataSource(typeof(ReviewRows), nameof(ReviewRows.EmptyIntegers))]
public class MatchingMethodOnlyEmptyClassRows<T>
{
    public MatchingMethodOnlyEmptyClassRows(T value)
        => throw new InvalidOperationException("Missing constructor value must be reported.");

    [Test]
    [Arguments(1)]
    public void ReportsMissingConstructorValue(T argument) => _ = argument;
}

// Explicit types cover sources whose object declaration cannot infer a closed class.
[GenerateGenericTest(typeof(int))]
[MethodDataSource(typeof(ExplicitUninferableProvider), nameof(ExplicitUninferableProvider.Rows))]
public class ExplicitUninferableRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsEqualTo(1);
}

public static class ExplicitUninferableProvider
{
    public static IEnumerable<object> Rows() => [1];
}
