using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// Every class-level and method-level [Arguments] row on a generic test class must produce its own
// test, whatever type it infers: the cross product of class rows and method rows, as for a
// non-generic class. Rows that inferred an already-seen type used to be dropped (#7106).

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[Arguments(3)]
public class GenericClassSameTypeRows<T>(T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments).IsEquivalentTo(new object?[] { value });
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("a")]
[Arguments(2)]
[Arguments("b")]
public class GenericClassMixedTypeRows<T>(T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value is int or string).IsTrue();
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments).IsEquivalentTo(new object?[] { value });
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericClassSameTypeMethodRows<T>
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Run(T value)
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericClassMixedTypeMethodRows<T>
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    [Arguments(2)]
    public async Task Run(T value)
    {
        await Assert.That(value is int or string).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
public class GenericClassRowsTimesGenericMethodRows<T>(T value)
{
    [Test]
    [Arguments("x")]
    [Arguments("y")]
    public async Task Run<T2>(T2 other)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(other).IsTypeOf<string>();
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments).IsEquivalentTo(new object?[] { value });
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
public class GenericClassRowsTimesMethodRows<T>(T value)
{
    [Test]
    [Arguments(7)]
    [Arguments(8)]
    public async Task Run(int other)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(other is 7 or 8).IsTrue();
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments).IsEquivalentTo(new object?[] { value });
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(5)]
public class GenericClassRowWithMethodRow<T>(T value)
{
    [Test]
    [Arguments(7)]
    public async Task Run(int other)
    {
        await Assert.That(value).IsEqualTo((T)(object)5);
        await Assert.That(other).IsEqualTo(7);
    }
}
