using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3627;

/// <summary>
/// Reproduction test for GitHub issue #3627: StateBag not working with DependsOn
/// https://github.com/thomhurst/TUnit/issues/3627
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class StateBagWithDependsOnTests
{
    [Test]
    public async Task FirstTest_SetupData()
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(100));

        TestContext.Current!.StateBag.Items["MessageSentAt"] = DateTime.UtcNow;
        TestContext.Current.StateBag.Items["TestData"] = "Hello from FirstTest";
    }

    [Test]
    [DependsOn(nameof(FirstTest_SetupData))]
    public async Task SecondTest_AccessDependentData()
    {
        var dependentTests = TestContext.Current!.Dependencies.GetTests(nameof(FirstTest_SetupData));

        await Assert.That(dependentTests).HasCount().EqualTo(1);

        var firstTestContext = dependentTests[0];

        await Assert.That(firstTestContext.StateBag.Items).ContainsKey("MessageSentAt");
        await Assert.That(firstTestContext.StateBag.Items).ContainsKey("TestData");

        var messageSentAt = firstTestContext.StateBag.Items["MessageSentAt"];
        var testData = firstTestContext.StateBag.Items["TestData"];

        await Assert.That(messageSentAt).IsNotNull();
        await Assert.That(testData).IsEqualTo("Hello from FirstTest");

        await Assert.That(TestContext.Current.StateBag.Items).DoesNotContainKey("MessageSentAt");
        await Assert.That(TestContext.Current.StateBag.Items).DoesNotContainKey("TestData");
    }

    [Test]
    [DependsOn(nameof(FirstTest_SetupData))]
    public async Task ThirdTest_DemonstrateMultipleDependencies()
    {
        var dependentTests = TestContext.Current!.Dependencies.GetTests(nameof(FirstTest_SetupData));

        var messageSentAt = dependentTests[0].StateBag.Items["MessageSentAt"] as DateTime?;

        await Assert.That(messageSentAt).IsNotNull();
        await Assert.That(messageSentAt!.Value).IsBeforeOrEqualTo(DateTime.UtcNow);
    }
}
