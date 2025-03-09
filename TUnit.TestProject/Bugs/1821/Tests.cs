using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1821;

[ClassDataSource<MyData>]
public class Tests(MyData data)
{
    [Test]
    [MethodDataSource(nameof(DataSource))]
    public async Task MethodDataSource(string value)
    {
        await Assert.That(value).IsEqualTo("Hello World!");
    }
    
    [Test]
    [MatrixDataSource]
    public async Task MatrixDataSource([MatrixMethod<Tests>(nameof(DataSource))] string value)
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