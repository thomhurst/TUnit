using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class CustomDisplayNameTests
{
    public const string SameClassConstant = "My constant";

    [Test]
    [DisplayName("A super important test!")]
    public async Task Test()
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("A super important test!");
    }

    [Test]
    [DisplayName("Another super important test!")]
    public async Task Test2()
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("Another super important test!");
    }

    [Test]
    [Arguments("foo", 1, true)]
    [Arguments("bar", 2, false)]
    [DisplayName("Test with: $value1 $value2 $value3!")]
    public async Task Test3(string value1, int value2, bool value3)
    {
        await Assert.That(TestContext.Current!.GetDisplayName())
            .IsEqualTo("Test with: foo 1 True!")
            .Or
            .IsEqualTo("Test with: bar 2 False!");
    }

    [Test]
    [MethodDataSource(nameof(Method))]
    [DisplayName("Test using MethodDataSource")]
    public async Task MethodDataSourceTest(string foo)
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("Test using MethodDataSource");
    }

    [Test]
    [MyGenerator]
    public async Task PasswordTest(string password)
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("PasswordTest(REDACTED)");
    }

    [Test]
    [DisplayName($"My test {SameClassConstant}")]
    public async Task SameClassConstantTest()
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("My test My constant");
    }

    [Test]
    [DisplayName($"My test {DifferentClassConstants.Constant}")]
    public async Task DifferentClassConstantTest()
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("My test My constant");
    }

    [Test]
    [DisplayName($"My test {NestedClassConstants.Constant}")]
    public async Task NestedClassConstantTest()
    {
        await Assert.That(TestContext.Current!.GetDisplayName()).IsEqualTo("My test My constant");
    }

    public class MyGenerator : DataSourceGeneratorAttribute<string>, ITestDiscoveryEventReceiver
    {
        protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => "Super Secret Password";
        }

        public ValueTask OnTestDiscovered(DiscoveredTestContext context)
        {
            context.SetDisplayName($"{context.TestDetails.TestName}(REDACTED)");
            return default;
        }

        public int Order => 0;
    }

    public static string Method() => "bar";

    public int Order => 0;

    public static class NestedClassConstants
    {
        public const string Constant = "My constant";
    }
}

public static class DifferentClassConstants
{
    public const string Constant = "My constant";
}
