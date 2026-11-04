using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7522;

[EngineTest(ExpectedResult.Pass)]
public class PropertyMetadataTests
{
    public string Value => "value";
    public static int StaticValue => 42;
    public int? Optional => null;
    private string Hidden => "hidden";

    [Test]
    public async Task PublicPropertiesHaveUsableMetadata()
    {
        var metadata = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Class;
        await Assert.That(metadata.Properties.Length).IsEqualTo(3);
        foreach (var property in metadata.Properties)
        {
            await Assert.That(ReferenceEquals(property.ClassMetadata, metadata)).IsTrue();
            await Assert.That(ReferenceEquals(property.ContainingTypeMetadata, metadata)).IsTrue();
            await Assert.That(property.ReflectionInfo.Name).IsEqualTo(property.Name);
        }
        var value = metadata.Properties.Single(p => p.Name == nameof(Value));
        await Assert.That(value.Type).IsEqualTo(typeof(string));
        await Assert.That(value.Getter(this)).IsEqualTo("value");
        await Assert.That(value.IsStatic).IsFalse();
        var staticValue = metadata.Properties.Single(p => p.Name == nameof(StaticValue));
        await Assert.That(staticValue.IsStatic).IsTrue();
        await Assert.That(staticValue.Getter(null)).IsEqualTo(42);
        var optional = metadata.Properties.Single(p => p.Name == nameof(Optional));
        await Assert.That(optional.IsNullable).IsTrue();
        await Assert.That(optional.Getter(this)).IsNull();
    }
}
