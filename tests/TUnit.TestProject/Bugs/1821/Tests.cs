using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1821;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<MyData>]
public class Tests(MyData data)
{
    [Test]
    [InstanceMethodDataSource(nameof(DataSource))]
    public async Task MethodDataSource(string value)
    {
        await Assert.That(value).IsEqualTo("Hello World!");
    }

    [Test]
    [MatrixDataSource]
    public async Task MatrixDataSource([MatrixInstanceMethod<Tests>(nameof(DataSource))] string value)
    {
        await Assert.That(value).IsEqualTo("Hello World!");
    }

    public IEnumerable<string> DataSource()
    {
        yield return data.MyMethod();
    }
}

public class MyData
{
    public string MyMethod()
    {
        return "Hello World!";
    }
}
