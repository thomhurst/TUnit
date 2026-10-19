using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7120;

// [Arguments(null)] passes a null params array, which ArgumentsAttribute stores as one null value. On a
// generic method it infers object in both modes, so the row runs as Run<object>(null) (#7120).
[EngineTest(ExpectedResult.Pass)]
public class NullArgumentsGenericMethod
{
    [Test]
    [Arguments(null)]
    public async Task Run<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(value).IsNull();
    }
}

// A null element beside other values infers object too, with no [GenerateGenericTest] to fall back on.
[EngineTest(ExpectedResult.Pass)]
public class NullElementGenericMethod
{
    [Test]
    [Arguments(null, 1)]
    public async Task Run<T>(T value, int other)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(value).IsNull();
        await Assert.That(other).IsEqualTo(1);
    }
}

// Each row infers its own type: Run<string>("a") and Run<object>(null).
[EngineTest(ExpectedResult.Pass)]
public class NullAndTypedRowsGenericMethod
{
    [Test]
    [Arguments("a")]
    [Arguments(null)]
    public async Task Run<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(value is null ? typeof(object) : typeof(string));
    }
}
