namespace TUnit.Core;

public class DiscoveredTestContext(TestDetails TestDetails)
{
    internal Dictionary<string, string>? Properties;
    
    public void AddProperty(string key, string value)
    {
        var properties = Properties ??= new Dictionary<string, string>();
        properties.Add(key, value);
    }
}