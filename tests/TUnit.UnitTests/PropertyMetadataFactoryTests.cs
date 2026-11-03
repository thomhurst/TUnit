namespace TUnit.UnitTests;

public class PropertyMetadataFactoryTests
{
    [Test]
    public async Task GetterSupportsMultipleClosedTypesAndRepeatedReads()
    {
        var getter = PropertyMetadataFactory.CreateGenericGetter(typeof(Container<>), "Value");
        var text = new Container<string>("first");
        var number = new Container<int>(42);
        await Assert.That(getter(text)).IsEqualTo("first");
        await Assert.That(getter(number)).IsEqualTo(42);
        text.Value = "second";
        await Assert.That(getter(text)).IsEqualTo("second");
    }

    [Test]
    public async Task GetterResolvesDeclaringTypeRatherThanHiddenDerivedProperty()
    {
        var getter = PropertyMetadataFactory.CreateGenericGetter(typeof(Container<>), "Value");
        await Assert.That(getter(new Derived())).IsEqualTo(42);
    }

    [Test]
    public async Task GetterPreservesNullValues()
    {
        var getter = PropertyMetadataFactory.CreateGenericGetter(typeof(Container<>), "Value");
        await Assert.That(getter(new Container<string?>(null))).IsNull();
    }

    public class Container<T>(T value)
    {
        public T Value { get; set; } = value;
    }

    public class Derived() : Container<int>(42)
    {
        public new string Value => "hidden";
    }
}
