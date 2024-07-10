using System.Collections.Immutable;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

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
        await Assert.That(GetDictionary()).Does.ContainKey("ClassProperty");
        await Assert.That(GetDictionary()).Does.ContainValue("ClassPropertyValue");
        
        await Assert.That(GetDictionary()).Does.ContainKey("ClassProperty2");
        await Assert.That(GetDictionary()).Does.ContainValue("ClassPropertyValue2");
        
        await Assert.That(GetDictionary()).Does.ContainKey("MethodProperty");
        await Assert.That(GetDictionary()).Does.ContainValue("MethodPropertyValue");
        
        await Assert.That(GetDictionary()).Does.ContainKey("MethodProperty2");
        await Assert.That(GetDictionary()).Does.ContainValue("MethodPropertyValue2");
    }

    private static ImmutableDictionary<string, string> GetDictionary()
    {
        return TestContext.Current?.TestDetails.CustomProperties.ToImmutableDictionary(x => x.Key, x => x.Value)
            ?? ImmutableDictionary<string, string>.Empty;
    }

    private class ClassPropertyAttribute : PropertyAttribute
    {
        public ClassPropertyAttribute() : base("ClassProperty2", "ClassPropertyValue2")
        {
        }
    }
    
    private class MethodPropertyAttribute : PropertyAttribute
    {
        public MethodPropertyAttribute() : base("MethodProperty2", "MethodPropertyValue2")
        {
        }
    }
}