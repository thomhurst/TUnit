namespace TUnit.TestProject;

public class TestMixedGenericParameters
{
    [Test]
    [Arguments(42, "42")]
    [Arguments("test", "test")]
    public async Task GenericArgumentsTest<T>(T value, string expected)
    {
        // This test has mixed generic and non-generic parameters
        // The first parameter is generic (T), the second is non-generic (string)
        await Assert.That(value?.ToString()).IsEqualTo(expected);
    }
}