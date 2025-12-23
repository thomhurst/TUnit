namespace TUnit.TestProject;

public class NotDiscoverableTests
{
    [Test]
    [NotDiscoverable]
    public void Test_WithNotDiscoverable_ShouldNotAppearInDiscovery()
    {
        // This test should execute but not appear in test explorer
    }

    [Test]
    [NotDiscoverable("Infrastructure test")]
    public void Test_WithNotDiscoverableAndReason_ShouldNotAppearInDiscovery()
    {
        // This test should execute but not appear in test explorer
    }

    [Test]
    public void Test_WithoutNotDiscoverable_ShouldAppearInDiscovery()
    {
        // This test should appear normally in test explorer
    }
}

[NotDiscoverable]
public class NotDiscoverableClassTests
{
    [Test]
    public void Test_InNotDiscoverableClass_ShouldNotAppearInDiscovery()
    {
        // All tests in this class should be hidden from discovery
    }

    [Test]
    public void AnotherTest_InNotDiscoverableClass_ShouldNotAppearInDiscovery()
    {
        // All tests in this class should be hidden from discovery
    }
}

public class ConditionalNotDiscoverableAttribute : NotDiscoverableAttribute
{
    public ConditionalNotDiscoverableAttribute() : base("Conditionally hidden") { }

    public override Task<bool> ShouldHide(TestRegisteredContext context)
    {
        // Only hide if environment variable is set
        return Task.FromResult(Environment.GetEnvironmentVariable("HIDE_TEST") == "true");
    }
}

public class ConditionalNotDiscoverableTests
{
    [Test]
    [ConditionalNotDiscoverable]
    public void Test_WithConditionalNotDiscoverable_HidesBasedOnCondition()
    {
        // This test is hidden only when HIDE_TEST=true
    }
}
