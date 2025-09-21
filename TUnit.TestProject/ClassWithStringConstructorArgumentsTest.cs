using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments("Hello")]
[Arguments("World")]
public class ClassWithStringConstructorArgumentsTest(string title)
{
    [Test]
    public async Task TestWithStringConstructorArgument()
    {
        await Assert.That(title).IsNotNull();
        await Assert.That(title).IsIn("Hello", "World");
    }
}
