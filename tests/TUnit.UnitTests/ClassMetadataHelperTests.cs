using TUnit.Core.PropertyInjection;

namespace TUnit.UnitTests;

public class ClassMetadataHelperTests
{
    [Test]
    public async Task PropertiesAreDeclaredPublicAndReferBackToTheirClass()
    {
        var metadata = ClassMetadataHelper.GetOrCreateClassMetadata(typeof(Derived));
        await Assert.That(metadata.Properties.Length).IsEqualTo(2);
        var instance = new Derived();
        var value = metadata.Properties.Single(p => p.Name == nameof(Derived.Value));
        await Assert.That(value.Getter(instance)).IsEqualTo("derived");
        await Assert.That(value.ReflectionInfo.DeclaringType).IsEqualTo(typeof(Derived));
        await Assert.That(ReferenceEquals(value.ClassMetadata, metadata)).IsTrue();
        await Assert.That(ReferenceEquals(value.ContainingTypeMetadata, metadata)).IsTrue();
        var count = metadata.Properties.Single(p => p.Name == nameof(Derived.Count));
        await Assert.That(count.IsStatic).IsTrue();
        await Assert.That(count.Getter(null)).IsEqualTo(42);
        await Assert.That(ReferenceEquals(metadata, ClassMetadataHelper.GetOrCreateClassMetadata(typeof(Derived)))).IsTrue();
    }

    public class Base
    {
        public string Inherited => "inherited";
    }

    public class Derived : Base
    {
        public string Value => "derived";
        public static int Count => 42;
        private string Hidden => "hidden";
    }
}
