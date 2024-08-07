#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.Core;

public class DiscoveredTestContext
{
    internal Dictionary<string, string>? Properties;
    public TestDetails TestDetails { get; }

    internal DiscoveredTestContext(TestDetails testDetails)
    {
        TestDetails = testDetails;
    }

    public void AddProperty(string key, string value)
    {
        var properties = Properties ??= new Dictionary<string, string>();
        properties.Add(key, value);
    }
}