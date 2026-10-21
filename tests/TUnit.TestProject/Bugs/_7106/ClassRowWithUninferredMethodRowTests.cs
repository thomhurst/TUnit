using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// A class-level row establishes the instantiation, and a method-level row that infers no type
// (here a null value) still runs on it, in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
public class ClassRowWithUninferredMethodRow<T>(T classValue)
{
    [Test]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(classValue).IsEqualTo((T)(object)"a");
        await Assert.That(value).IsNull();
    }
}

// The same, with a [GenerateGenericTest] listing the type the class row infers.
[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
[GenerateGenericTest(typeof(string))]
public class GenerateGenericTestWithClassRowAndUninferredMethodRow<T>(T classValue)
{
    [Test]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(classValue).IsEqualTo((T)(object)"a");
        await Assert.That(value).IsNull();
    }
}

// A class row plus method rows that infer the class row's type and none at all: every method row runs.
[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
public class ClassRowWithInferredAndUninferredMethodRows<T>(T classValue)
{
    [Test]
    [Arguments("b")]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(classValue).IsEqualTo((T)(object)"a");
        await Assert.That(value is null or "b").IsTrue();
    }
}
