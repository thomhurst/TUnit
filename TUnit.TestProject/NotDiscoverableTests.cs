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
