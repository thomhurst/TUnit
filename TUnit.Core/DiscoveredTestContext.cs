namespace TUnit.Core;

public class DiscoveredTestContext(TestDetails TestDetails)
{
    internal Dictionary<string, string>? Properties;
    internal Dictionary<string, object?>? ObjectBag;
    
    public void AddProperty(string key, string value) =>
        (Properties ??= new Dictionary<string, string>()).Add(key, value);

    public void AddToObjectBag(string key, object? value) =>
        (ObjectBag ??= new Dictionary<string, object?>()).Add(key, value);
}