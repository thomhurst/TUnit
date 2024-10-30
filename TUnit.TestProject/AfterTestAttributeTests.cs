using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class AfterTestAttributeTests
{
    private static readonly string Filename = $"{Guid.NewGuid():N}-AfterTestAttributeTests.txt";
    
    [Test]
    [WriteFileAfterTest]
    public async Task Test()
    {
        await Assert.That(File.Exists(Filename)).IsFalse();
    }

    public class WriteFileAfterTestAttribute : Attribute, ITestEndEventReceiver
    {
        public async ValueTask OnTestEnd(TestContext testContext)
        {
            Console.WriteLine(@"Writing file inside WriteFileAfterTestAttribute!");
            await File.WriteAllTextAsync(Filename, "Foo!");
        }
    }
}