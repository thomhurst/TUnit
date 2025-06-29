using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DisposableFieldTests
{
    private Stream? _stream;

    [Before(Test)]
    public void Setup()
    {
        _stream = new MemoryStream();
    }

    [After(Test)]
    public void Blah()
    {
        _stream?.Dispose();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}
