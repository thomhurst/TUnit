namespace TUnit.Core;

/// <summary>
/// Factory for creating TestContext instances from discovery data
/// </summary>
public static class TestContextFactory
{
    /// <summary>
    /// Creates a TestContext from discovery data
    /// </summary>
    public static TestContext CreateFromDiscovery(
        DiscoveredTestContext discoveryContext,
        CancellationToken cancellationToken,
        IServiceProvider serviceProvider)
    {
        var testContext = new TestContext(
            discoveryContext.TestName,
            discoveryContext.DisplayName,
            cancellationToken,
            serviceProvider)
        {
            TestDetails = discoveryContext.TestDetails,
            Phase = TestPhase.Execution
        };

        discoveryContext.TransferTo(testContext);

        return testContext;
    }
}
