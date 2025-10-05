extern alias JsonNet;
using JObject = JsonNet::Newtonsoft.Json.Linq.JObject;

namespace TUnit.TestProject.ExternAlias;

/// <summary>
/// Concrete test class that uses the base class with extern alias.
/// This tests that the property injection source generator correctly
/// handles extern aliases in both the base class and derived class.
/// </summary>
public class JsonTests : JsonTestBase
{
    [Test]
    public async Task Test_JsonData_IsNotNull()
    {
        await Assert.That(JsonData).IsNotNull();
    }

    [Test]
    public async Task Test_JsonData_IsCorrectType()
    {
        var type = JsonData.GetType();
        await Assert.That(type.FullName).Contains("JObject");
    }

    [Test]
    public async Task Test_JsonData_CanAddProperty()
    {
        JsonData["testProp"] = "testValue";
        await Assert.That(JsonData["testProp"]!.ToString()).IsEqualTo("testValue");
    }

    [Test]
    [MethodDataSource(nameof(JsonDataSource.GetJsonObjects))]
    public async Task Test_WithMethodDataSource(JObject obj)
    {
        await Assert.That(obj).IsNotNull();
        await Assert.That(obj["test"]).IsNotNull();
    }
}
