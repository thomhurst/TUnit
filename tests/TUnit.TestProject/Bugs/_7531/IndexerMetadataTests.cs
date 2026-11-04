using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7531;

[EngineTest(ExpectedResult.Pass)]
public class IndexerMetadataTests
{
    public string this[int index] => index.ToString();
    public string Label => "ordinary";

    [Test]
    public async Task Runs() => await Assert.That(Label).IsEqualTo("ordinary");
}

[Arguments("generic")]
[EngineTest(ExpectedResult.Pass)]
public class GenericIndexerMetadataTests<T>(T value)
{
    public T this[int index] => value;
    public T this[string index] => value;
    public T Value => value;

    [Test]
    public async Task Runs() => await Assert.That(this[0]).IsEqualTo(Value);
}

[EngineTest(ExpectedResult.Pass)]
public class OverloadedIndexerMetadataTests
{
    public string this[int index] => index.ToString();
    public string this[string index] => index;
    public string Label => "ordinary";

    [Test]
    public async Task Runs() => await Assert.That(Label).IsEqualTo("ordinary");
}
