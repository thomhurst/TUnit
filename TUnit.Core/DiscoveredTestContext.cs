#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.Core;

public class DiscoveredTestContext
{
    internal Dictionary<string, string>? Properties;
    public TestContext TestContext { get; }
    public TestDetails TestDetails => TestContext.TestDetails;

    internal DiscoveredTestContext(TestContext testContext)
    {
        TestContext = testContext;
    }

    public void AddProperty(string key, string value)
    {
        var properties = Properties ??= new Dictionary<string, string>();
        properties.Add(key, value);
    }
}