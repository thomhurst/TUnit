using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7206;

// A typed data source (DataSourceGeneratorAttribute<T>) closes a generic test method over its declared type
// argument, not over the run-time type of the values it returns, in both modes and without a method-level
// [GenerateGenericTest] (#7206). Sources that infer no type keep the run-time inference.

// The issue's repro: the source is declared as object and returns a string, so Run<object>("str").
[EngineTest(ExpectedResult.Pass)]
public class ObjectSourceOnNonGenericClass
{
    [Test]
    [ObjectSource7206]
    public async Task Run<U>(U value)
    {
        await Assert.That(typeof(U)).IsEqualTo(typeof(object));
        await Assert.That(value).IsEqualTo((U)(object)"str");
    }
}

// The same on a generic class closed by a class-level [GenerateGenericTest].
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class ObjectSourceOnGenericClass<T>
{
    [Test]
    [ObjectSource7206]
    public async Task Run<U>(U value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(U)).IsEqualTo(typeof(object));
        await Assert.That(value).IsEqualTo((U)(object)"str");
    }
}

// An [Arguments] row beside the typed source keeps inferring its own type: Run<object>(str) and Run<int>(1).
[EngineTest(ExpectedResult.Pass)]
public class ObjectSourceAndArgumentsRow
{
    [Test]
    [ObjectSource7206]
    [Arguments(1)]
    public async Task Run<U>(U value)
    {
        if (value is int)
        {
            await Assert.That(typeof(U)).IsEqualTo(typeof(int));
        }
        else
        {
            await Assert.That(typeof(U)).IsEqualTo(typeof(object));
        }
    }
}

// The same on a generic class closed by a class-level [GenerateGenericTest].
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class GenericClassObjectSourceAndArgumentsRow<T>
{
    [Test]
    [ObjectSource7206]
    [Arguments(1)]
    public async Task Run<U>(U value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        if (value is int)
        {
            await Assert.That(typeof(U)).IsEqualTo(typeof(int));
        }
        else
        {
            await Assert.That(typeof(U)).IsEqualTo(typeof(object));
        }
    }
}

// Two typed sources declaring the same type share one instantiation, and each still runs:
// Run<object>(str) and Run<object>(other).
[EngineTest(ExpectedResult.Pass)]
public class TwoObjectSources
{
    [Test]
    [ObjectSource7206]
    [OtherObjectSource7206]
    public async Task Run<U>(U value)
    {
        await Assert.That(typeof(U)).IsEqualTo(typeof(object));
    }
}

// Generic class rows: the typed source runs on each class row's instantiation.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
public class ObjectSourceOnClassRows<T>(T classValue)
{
    [Test]
    [ObjectSource7206]
    public async Task Run<U>(U value)
    {
        await Assert.That(classValue).IsTypeOf<T>();
        await Assert.That(typeof(U)).IsEqualTo(typeof(object));
    }
}

public class ObjectSource7206 : DataSourceGeneratorAttribute<object>
{
    protected override IEnumerable<Func<object>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "str";
    }
}

public class OtherObjectSource7206 : DataSourceGeneratorAttribute<object>
{
    protected override IEnumerable<Func<object>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "other";
    }
}
