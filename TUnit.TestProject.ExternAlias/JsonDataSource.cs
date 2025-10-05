extern alias JsonNet;

using JObject = JsonNet::Newtonsoft.Json.Linq.JObject;

namespace TUnit.TestProject.ExternAlias;

/// <summary>
/// Data source that provides JObject instances.
/// This uses extern alias to test the source generator handles it correctly.
/// </summary>
public class JsonDataSource
{
    public static JObject CreateJsonObject()
    {
        return new JObject();
    }

    public static IEnumerable<JObject> GetJsonObjects()
    {
        yield return new JObject { ["test"] = "value1" };
        yield return new JObject { ["test"] = "value2" };
    }
}
