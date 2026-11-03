using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7499;

[Arguments("first")]
[Arguments("second")]
[Arguments(42)]
[EngineTest(ExpectedResult.Pass)]
public class GenericPropertyMetadataTests<T>(T value)
{
    public T Value { get; } = value;
    public string Label => "label";
    public T? Optional { get; }

    [Test]
    public async Task MetadataGettersReadClosedInstance()
    {
        var valueGetter = PropertyMetadataFactory.CreateGenericGetter(typeof(GenericPropertyMetadataTests<>), nameof(Value));
        var labelGetter = PropertyMetadataFactory.CreateGenericGetter(typeof(GenericPropertyMetadataTests<>), nameof(Label));
        var optionalGetter = PropertyMetadataFactory.CreateGenericGetter(typeof(GenericPropertyMetadataTests<>), nameof(Optional));
        await Assert.That(valueGetter(this)).IsEqualTo(Value);
        await Assert.That(labelGetter(this)).IsEqualTo("label");
        await Assert.That(optionalGetter(this)).IsEqualTo(Optional);
    }
}
