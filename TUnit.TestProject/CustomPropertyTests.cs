﻿using System.Collections.Immutable;

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
        await Assert.That(GetDictionary()["ClassProperty"]).Contains("ClassPropertyValue");

        await Assert.That(GetDictionary()).ContainsKey("ClassProperty2");
        await Assert.That(GetDictionary()["ClassProperty2"]).Contains("ClassPropertyValue2");

        await Assert.That(GetDictionary()).ContainsKey("MethodProperty");
        await Assert.That(GetDictionary()["MethodProperty"]).Contains("MethodPropertyValue");

        await Assert.That(GetDictionary()).ContainsKey("MethodProperty2");
        await Assert.That(GetDictionary()["MethodProperty2"]).Contains("MethodPropertyValue2");
    }

    private static ImmutableDictionary<string, IReadOnlyList<string>> GetDictionary()
    {
        return TestContext.Current?.TestDetails.CustomProperties.ToImmutableDictionary(x => x.Key, x => x.Value)
            ?? ImmutableDictionary<string, IReadOnlyList<string>>.Empty;
    }

    public class ClassPropertyAttribute() : PropertyAttribute("ClassProperty2", "ClassPropertyValue2");

    public class MethodPropertyAttribute() : PropertyAttribute("MethodProperty2", "MethodPropertyValue2");
}
