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
