using System.Collections.Immutable;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[Property("ClassProperty", "ClassPropertyValue")]
[ClassProperty]
public class CustomPropertyTests
{
    [Test]
    [Property("MethodProperty", "MethodPropertyValue")]
    [MethodProperty]
    public async Task Test()
    {
        await Assert.That(GetDictionary()).ContainsKey("ClassProperty");
        await Assert.That(GetDictionary()).ContainsValue("ClassPropertyValue");
        
        await Assert.That(GetDictionary()).ContainsKey("ClassProperty2");
        await Assert.That(GetDictionary()).ContainsValue("ClassPropertyValue2");
        
        await Assert.That(GetDictionary()).ContainsKey("MethodProperty");
        await Assert.That(GetDictionary()).ContainsValue("MethodPropertyValue");
        
        await Assert.That(GetDictionary()).ContainsKey("MethodProperty2");
        await Assert.That(GetDictionary()).ContainsValue("MethodPropertyValue2");
    }

    private static ImmutableDictionary<string, string> GetDictionary()
    {
        return TestContext.Current?.TestDetails.CustomProperties.ToImmutableDictionary(x => x.Key, x => x.Value)
            ?? ImmutableDictionary<string, string>.Empty;
    }

    public class ClassPropertyAttribute() : PropertyAttribute("ClassProperty2", "ClassPropertyValue2");
    
    public class MethodPropertyAttribute() : PropertyAttribute("MethodProperty2", "MethodPropertyValue2");
}