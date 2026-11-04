using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7573;

[EngineTest(ExpectedResult.Pass)]
public class NullableClassMethodRows<T> where T : struct
{
    public static IEnumerable<U> Values<U>() => [];
    public static IEnumerable<int> Values() => [42];
    public static IEnumerable<int> MoreValues() => [42];
    public static IEnumerable<double> DoubleValues() => [84d];
    public static IEnumerable<int?> NullableValues() => [42, null];
    public static IEnumerable<Func<int>> Factories() => [() => 42];
    public static IEnumerable<Func<(int, int?)>> Tuples() => [() => (42, null)];

    [Test]
    [MethodDataSource(nameof(Values))]
    public async Task Value(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsNotNull();
    }

    [Test]
    [MethodDataSource(nameof(Values))]
    [MethodDataSource(nameof(DoubleValues))]
    [MethodDataSource(nameof(MoreValues))]
    public async Task MultipleSources(T? value)
    {
        // Each source runs only on the class type inferred from its declared row type.
        await Assert.That(value).IsEqualTo(typeof(T) == typeof(int) ? (T)(object)42 : (T)(object)84d);
    }

    [Test]
    [MethodDataSource(nameof(NullableValues))]
    public async Task NullableValue(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value is null || value.Equals(42)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Factories))]
    public async Task Factory(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsNotNull();
    }

    [Test]
    [MethodDataSource(nameof(Tuples))]
    public async Task Tuple(T? first, T? second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class ExplicitNullableClassMethodRows<T> where T : struct
{
    public static IEnumerable<int> OwnRows() => [1];
    public IEnumerable<int> InstanceRows() => [2];

    [Test]
    [MethodDataSource(nameof(OwnRows))]
    [MethodDataSource(nameof(InstanceRows))]
    [MethodDataSource(typeof(NullableClassRowsProvider), nameof(NullableClassRowsProvider.ExternalRows))]
    public async Task RetainsAllSources(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsNotNull();
    }
}

public static class NullableClassRowsProvider
{
    public static IEnumerable<int> ExternalRows() => [3];
}

[EngineTest(ExpectedResult.Pass)]
public class ArrayClassMethodRows<T>
{
    public static IEnumerable<int[]> Rows() => [new[] { 42 }];
    public static IEnumerable<Func<int[]>> Factories() => [() => new[] { 42 }];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task DirectArray(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values).Count().IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(Factories))]
    public async Task FactoryArray(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values).Count().IsEqualTo(1);
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class OpenClassMethodRows<T>
{
    public static IEnumerable<T> Rows() => [default!];
    public static IEnumerable<T[]> Arrays() => [new T[1]];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task OpenRow(T value) => await Assert.That(typeof(T)).IsEqualTo(typeof(int));

    [Test]
    [MethodDataSource(nameof(Arrays))]
    public async Task OpenArray(T[] values) => await Assert.That(typeof(T)).IsEqualTo(typeof(int));
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1d)]
public class ConstructorNullableClassRows<T>(T seed) where T : struct
{
    public static IEnumerable<int> Rows() => [42];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task ConstructorType(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(double));
        await Assert.That(seed).IsEqualTo((T)(object)1d);
        await Assert.That(value).IsEqualTo((T)(object)42d);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1d)]
public class ConstructorNestedNullableClassRows<T>(T seed) where T : struct
{
    public static IEnumerable<Func<int[]>> Rows() => [() => null!];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task NestedConstructorType(T?[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(double));
        await Assert.That(seed).IsEqualTo((T)(object)1d);
        await Assert.That(values).IsNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(double))]
public class ArgumentOverloadClassRows<T>
{
    public static IEnumerable<int> Rows() => [1];
    public static IEnumerable<double> Rows(string text) => [42d];

    [Test]
    [MethodDataSource(nameof(Rows), Arguments = new object[] { "hello" })]
    public async Task SelectedOverload(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(double));
        await Assert.That(value).IsEqualTo((T)(object)42d);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("seed")]
public class TypeFreeConstructorNullableRows<T>(string seed) where T : struct
{
    public static IEnumerable<int> Rows() => [42];
    public static IEnumerable<double> DoubleRows() => [84d];
    public static IEnumerable<int> MoreRows() => [42];

    [Test]
    [MethodDataSource(nameof(Rows))]
    [MethodDataSource(nameof(DoubleRows))]
    [MethodDataSource(nameof(MoreRows))]
    public async Task PartitionsBesideConstructor(T? value)
    {
        await Assert.That(seed).IsEqualTo("seed");
        await Assert.That(value).IsEqualTo(typeof(T) == typeof(int) ? (T)(object)42 : (T)(object)84d);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task InfersBesideConstructor(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(seed).IsEqualTo("seed");
        await Assert.That(value).IsEqualTo((T)(object)42);
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(double))]
public class GenericProviderNullableRows<T> where T : struct
{
    public static IEnumerable<int> Rows() => [1];

    [Test]
    [MethodDataSource<DoubleNullableRowsProvider>(nameof(DoubleNullableRowsProvider.Rows))]
    public async Task UsesExternalProvider(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(double));
        await Assert.That(value).IsEqualTo((T)(object)42d);
    }
}

public class DoubleNullableRowsProvider
{
    public static IEnumerable<double> Rows() => [42d];
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("seed")]
public class TypeFreeConstructorUninferredRows<T>(string seed)
{
    public static IEnumerable<int> Rows() => [1];
    public IEnumerable<int> InstanceRows() => [2];

    [Test]
    [MethodDataSource(nameof(Rows))]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public async Task RetainsInstanceBesideConstructor(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(seed).IsEqualTo("seed");
        await Assert.That(value!.Equals(1) || value.Equals(2)).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class ExplicitUntypedClassRows<T>
{
    public static IEnumerable<int> Rows() => [1];

    [Test]
    [MethodDataSource(nameof(Rows))]
    [AdditionalUntypedRow]
    public async Task RetainsUntypedSource(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value!.Equals(1) || value.Equals(2)).IsTrue();
    }
}

public class AdditionalUntypedRowAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata metadata)
        => [() => [2]];
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class LiteralAndMethodClassRows<T> where T : struct
{
    public static IEnumerable<int> IntRows() => [42];
    public static IEnumerable<double> DoubleRows() => [84d];

    [Test]
    [Arguments(null)]
    [MethodDataSource(nameof(IntRows))]
    [MethodDataSource(nameof(DoubleRows))]
    public async Task PartitionsBesideLiteral(T? value)
    {
        if (value is null)
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        }
        else
        {
            await Assert.That(value).IsEqualTo(typeof(T) == typeof(int) ? (T)(object)42 : (T)(object)84d);
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class PartialConstructorNullableClassRows<T, U>(T seed) where T : struct
{
    public static IEnumerable<Func<(int?[], string)>> Rows() => [() => ([42], "label")];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task PartialConstructorTypes(T?[] values, U label)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(U)).IsEqualTo(typeof(string));
        await Assert.That(seed).IsEqualTo((T)(object)1);
        await Assert.That(values[0]).IsEqualTo((T)(object)42);
        await Assert.That(label!.ToString()).IsEqualTo("label");
    }
}
