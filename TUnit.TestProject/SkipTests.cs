using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SkipTests
{
    [Test]
    [Skip("Just because.")]
    public void Test()
    {
    }
}

public class AfterTestAttributeTests
{
    private static readonly string Filename = $"{Guid.NewGuid():N}-AfterTestAttributeTests.txt";
    
    [Test]
    [WriteFileAfterTest]
    public async Task Test()
    {
        await Assert.That(File.Exists(Filename)).Is.False();
    }

    private class WriteFileAfterTestAttribute : Attribute, IAfterTestAttribute
    {
        public async Task OnAfterTest(TestContext testContext)
        {
            await File.WriteAllTextAsync(Filename, "Foo!");
        }
    }
}