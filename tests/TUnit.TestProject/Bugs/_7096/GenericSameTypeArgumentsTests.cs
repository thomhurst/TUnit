using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7096;

/// <summary>
/// Several [Arguments] rows that infer the same generic type argument must each produce a test.
/// Source-gen mode used to keep only the first row per inferred type (#7096).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericMethodSameTypeArgumentsTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task SameType<T>(T value)
    {
        await Assert.That(value).IsTypeOf<int>();
    }

    [Test]
    [Arguments(1)]
    [Arguments("a")]
    [Arguments(2)]
    [Arguments("b")]
    public async Task MixedTypes<T>(T value)
    {
        await Assert.That(value is int or string).IsTrue();
    }
}
