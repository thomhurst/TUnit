using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[DisplayName("Custom Class Display Name")]
public class ClassDisplayNameAttributeTests
{
    [Test]
    public async Task Test()
    {
        // This test should inherit the class display name as a prefix or part of the test name
        await Assert.That(TestContext.Current!.GetDisplayName())
            .DoesNotContain("ClassDisplayNameAttributeTests");
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("TestValue")]
[DisplayName("Class with parameter: $value")]
public class ClassDisplayNameWithParametersTests(string value)
{
    [Test]
    public async Task Test()
    {
        // This test should show the class display name with parameter substitution
        var displayName = TestContext.Current!.GetDisplayName();
        await Assert.That(displayName)
            .Contains("TestValue");
    }
}