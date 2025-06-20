namespace TUnit.TestProject;

[Skip("Issue with AOT - https://github.com/microsoft/testfx/issues/4972")]
public class ConfigurationTests
{
    [Test]
    public async Task BasicConfigurationValue()
    {
        var value = TestContext.Configuration.Get("MyKey1");
        
        await Assert.That(value).IsEqualTo("MyValue1");
    }
    
    [Test]
    public async Task MissingConfigurationValue()
    {
        var value = TestContext.Configuration.Get("MyKey2");
        
        await Assert.That(value).IsNull();
    }
    
    [Test]
    public async Task NestedConfigurationValue()
    {
        var value = TestContext.Configuration.Get("Nested:MyKey2");
        
        await Assert.That(value).IsEqualTo("MyValue2");
    }
}