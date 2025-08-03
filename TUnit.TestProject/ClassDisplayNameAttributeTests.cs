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