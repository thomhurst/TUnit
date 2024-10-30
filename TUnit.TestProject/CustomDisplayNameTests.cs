using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.TestProject;

public class CustomDisplayNameTests
{
    [Test]
    [DisplayName("A super important test!")]
    public async Task Test()
    {
        await Assert.That(TestContext.Current!.GetTestDisplayName()).IsEqualTo("A super important test!");
    }
    
    [Test]
    [DisplayName("Another super important test!")]
    public async Task Test2()
    {
        await Assert.That(TestContext.Current!.GetTestDisplayName()).IsEqualTo("Another super important test!");
    }
    
    [Test]
    [Arguments("foo", 1, true)]
    [Arguments("bar", 2, false)]
    [DisplayName("Test with: $value1 $value2 $value3!")]
    public async Task Test3(string value1, int value2, bool value3)
    {
        await Assert.That(TestContext.Current!.GetTestDisplayName()).IsEqualTo("Test with: foo 1 True!")
            .Or.IsEqualTo("Test with: bar 2 False!");
    }
    
    [Test]
    [MyGenerator]
    public async Task PasswordTest(string password)
    {
        await Assert.That(TestContext.Current!.GetTestDisplayName()).IsEqualTo("PasswordTest(REDACTED)");
    }
    
    public class MyGenerator : DataSourceGeneratorAttribute<string>, ITestDiscoveryEventReceiver
    {
        public override IEnumerable<string> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return "Super Secret Password";
        }

        public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
        {
            discoveredTestContext.SetDisplayName($"{discoveredTestContext.TestDetails.TestName}(REDACTED)");
        }
    }
}