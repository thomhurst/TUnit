namespace TUnit.TestProject.Bugs._7163;

// A generic test method whose type arguments cannot be inferred from a data source's declared type
// must not vanish in source-generated mode. Reflection mode infers them from each row's run-time
// values; source-generated mode cannot, so it reports each row as a failed test that says how to fix
// it, instead of registering no test at all (#7163).
//
// These classes carry no [EngineTest] attribute because their outcome differs between the modes.
// TUnit.Engine.Tests targets them by name.

// The issue's repro: rows typed `object` say nothing about T.
public class ObjectRowsTests
{
    public static IEnumerable<object> Rows()
    {
        yield return 1;
        yield return 2;
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Generic<T>(T value) => await Assert.That((object?)value).IsNotNull();
}

// `object[]` rows, spread across two type parameters.
public class ObjectArrayRowsTests
{
    public static IEnumerable<object[]> Rows()
    {
        yield return [1, "a"];
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Generic<T1, T2>(T1 first, T2 second)
    {
        await Assert.That((object?)first).IsNotNull();
        await Assert.That((object?)second).IsNotNull();
    }
}

// One source infers the type arguments and another does not. The inferable row keeps running; the
// other source's rows must not be dropped.
public class PartlyInferableTests
{
    public static IEnumerable<object> Rows()
    {
        yield return 1;
    }

    [Test]
    [Arguments(5)]
    [MethodDataSource(nameof(Rows))]
    public async Task Generic<T>(T value) => await Assert.That((object?)value).IsNotNull();
}

// With [GenerateGenericTest] the type arguments are listed, so the rows run on that type in both modes.
public class GenerateGenericTestWithObjectRowsTests
{
    public static IEnumerable<object> Rows()
    {
        yield return 1;
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(Rows))]
    public async Task Generic<T>(T value) => await Assert.That((object?)value).IsNotNull();
}

// No data source at all: nothing can infer T in either mode.
public class NoDataSourceTests
{
    [Test]
    public async Task Generic<T>() => await Assert.That(typeof(T)).IsNotNull();
}

// An [Arguments] row and a method data source that infer the same type arguments share one
// instantiation, so both run instead of the data source being reported as uninferable.
public class ArgumentsAndDataSourceWithSameTypesTests
{
    public static IEnumerable<Func<(int, string)>> Rows()
    {
        yield return () => (2, "b");
    }

    [Test]
    [Arguments(1, "a")]
    [MethodDataSource(nameof(Rows))]
    public async Task Generic<T>(T value, string label)
    {
        await Assert.That((object?)value).IsNotNull();
        await Assert.That(label).IsNotNull();
    }
}

// Two method data sources declared with the same type infer the same type arguments; both run.
public class TwoDataSourcesWithSameTypesTests
{
    public static IEnumerable<Func<(int, string)>> First()
    {
        yield return () => (1, "a");
    }

    public static IEnumerable<Func<(int, string)>> Second()
    {
        yield return () => (2, "b");
    }

    [Test]
    [MethodDataSource(nameof(First))]
    [MethodDataSource(nameof(Second))]
    public async Task Generic<T>(T value, string label)
    {
        await Assert.That((object?)value).IsNotNull();
        await Assert.That(label).IsNotNull();
    }
}

// The failing entry's metadata names the return type outside the method, where T does not exist,
// so a return type that nests T must still compile.
public class NestedGenericReturnTypeTests
{
    [Test]
    public Task<T[]> Generic<T>() => Task.FromResult(Array.Empty<T>());
}
