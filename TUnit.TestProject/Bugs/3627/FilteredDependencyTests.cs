using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3627;

/// <summary>
/// Regression test for GitHub issue #3637: Filtered dependent test should also run dependency
/// When filtering to run a single test that depends on another test, the dependency should be
/// automatically included in execution.
/// https://github.com/thomhurst/TUnit/issues/3637
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class FilteredDependencyTests
{
    [Test]
    public async Task BaseTest()
    {
        // This test should run even when filtering for DependentTest only
        await Task.Delay(100);

        // Store data in StateBag to verify this test actually ran
        TestContext.Current!.StateBag.Items["BaseTestExecuted"] = true;
        TestContext.Current.StateBag.Items["BaseTestTimestamp"] = DateTime.UtcNow;
    }

    [Test]
    [DependsOn(nameof(BaseTest))]
    public async Task DependentTest()
    {
        // When filtering to run only this test, BaseTest should also execute
        var dependencies = TestContext.Current!.Dependencies.GetTests(nameof(BaseTest));

        await Assert.That(dependencies).HasCount().EqualTo(1);
        await Assert.That(dependencies[0]).IsNotNull();

        // Verify the dependency actually executed by checking its StateBag
        var baseTestContext = dependencies[0];
        await Assert.That(baseTestContext.StateBag.Items).ContainsKey("BaseTestExecuted");
        await Assert.That(baseTestContext.StateBag.Items["BaseTestExecuted"]).IsEqualTo(true);
        await Assert.That(baseTestContext.StateBag.Items).ContainsKey("BaseTestTimestamp");
    }
}
