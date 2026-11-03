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
