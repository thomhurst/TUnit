extern alias JsonNet;
using JObject = JsonNet::Newtonsoft.Json.Linq.JObject;

namespace TUnit.TestProject.ExternAlias;

/// <summary>
/// Base class using an extern alias to reference Newtonsoft.Json.
/// This tests that the source generator preserves extern alias qualifiers.
/// </summary>
public abstract class JsonTestBase
{
    /// <summary>
    /// Property with ClassDataSource using a type from an extern aliased package.
    /// The source generator must preserve the extern alias qualifier when generating code.
    /// </summary>
    [ClassDataSource<JObject>(Shared = SharedType.PerTestSession)]
    public required JObject JsonData { get; init; }

    /// <summary>
    /// Method data source using extern aliased types
    /// </summary>
    [MethodDataSource(nameof(JsonDataSource.GetJsonObjects))]
    public static void ConfigureJson(JObject json)
    {
        // Configuration method
    }
}
