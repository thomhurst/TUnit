#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.Core;

public class DiscoveredTestContext
{
    public TestContext TestContext { get; }
    public TestDetails TestDetails => TestContext.TestDetails;

    internal DiscoveredTestContext(TestContext testContext)
    {
        TestContext = testContext;
    }

    public void AddProperty(string key, string value)
    {
        TestContext.TestDetails.InternalCustomProperties.Add(key, value);
    }
}