using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2136;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Arguments(true, "True")]
    [Arguments(1, "1")]
    [Arguments(1.1, "1.1")]
    [Arguments("hello", "hello")]
    [Arguments(MyEnum.Item, "Item")]
    public async Task GenericArgumentsTest<T>(T value, string expected)
    {
        // Assert
        // Invariant: the expected strings use invariant formatting (e.g. "1.1"), whatever the machine culture.
        await Assert.That(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)).IsEqualTo(expected);
    }
}

public enum MyEnum
{
    Item,
    Item2
}
