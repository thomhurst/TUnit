namespace TUnit.TestProject;

public class NullableArgumentsTest
{
    [Test]
    [Arguments(1, 1)]
    [Arguments(1, null)]
    public async Task TestCase(decimal v1, decimal? v2)
    {
        await Assert.That(v1).IsEqualTo(v2.GetValueOrDefault());
    }
}
