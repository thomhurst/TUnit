using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7155;

// A null and a value-type value for the same type parameter infer object in both modes, in either order.
// A value type cannot hold the null, so Run<int> would turn it into 0 (#7155).
[EngineTest(ExpectedResult.Pass)]
public class NullAndValueTypeGenericMethod
{
    [Test]
    [Arguments(1, null)]
    [Arguments(null, 1)]
    public async Task Run<T>(T first, T second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(first is null ? second : first).IsEqualTo(1);
        await Assert.That(first is null || second is null).IsTrue();
    }
}

// The same rows with [GenerateGenericTest(typeof(object))]: the rows infer object, the listed type, so they
// run once as Run<object> instead of also as Run<int>.
[EngineTest(ExpectedResult.Pass)]
public class NullAndValueTypeGenerateGenericTestMethod
{
    [Test]
    [GenerateGenericTest(typeof(object))]
    [Arguments(null, 1)]
    public async Task Run<T>(T first, T second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(first).IsNull();
        await Assert.That(second).IsEqualTo(1);
    }
}
