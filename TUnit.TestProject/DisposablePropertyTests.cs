using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DisposablePropertyTests
{
#pragma warning disable TUnit0023
    public Stream? Stream { get; private set; }
#pragma warning restore TUnit0023

    [Before(Test)]
    public void Setup()
    {
        Stream = new MemoryStream();
    }

    [After(Test)]
    public void Blah()
    {
        Stream?.Dispose();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}
