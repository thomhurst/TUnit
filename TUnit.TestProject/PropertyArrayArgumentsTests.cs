using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class PropertyArrayArgumentsTests
{
    // Test array property with array argument
    [Arguments(new int[] { 1, 2, 3 })]
    public required int[] ArrayProperty { get; set; }

    // Test single property with single value
    [Arguments("single string")]
    public required string StringProperty { get; set; }

    [Test]
    public async Task TestArrayProperties()
    {
        await Assert.That(ArrayProperty).IsNotNull();
        await Assert.That(ArrayProperty).HasCount().EqualTo(3);
        await Assert.That(ArrayProperty[0]).IsEqualTo(1);
        await Assert.That(ArrayProperty[1]).IsEqualTo(2);
        await Assert.That(ArrayProperty[2]).IsEqualTo(3);
        await Assert.That(StringProperty).IsEqualTo("single string");
    }
}