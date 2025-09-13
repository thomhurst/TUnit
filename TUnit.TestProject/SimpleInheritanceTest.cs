using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Simple reproduction of the inheritance issue
public class SimpleContainer : IDisposable
{
    public string Id { get; } = Guid.NewGuid().ToString()[..8];
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
        Console.WriteLine($"Container {Id} disposed");
    }
}

// Working scenario - attribute directly on concrete class
public class DirectTestServer
{
    [ClassDataSource<SimpleContainer>(Shared = SharedType.PerTestSession)]
    public required SimpleContainer Container { get; set; } = new();
}

// Broken scenario - attribute on abstract base class
public abstract class BaseTestServer
{
    [ClassDataSource<SimpleContainer>(Shared = SharedType.PerTestSession)]
    public required SimpleContainer Container { get; set; } = new();
}

public class InheritedTestServer : BaseTestServer
{
    // Inherits the ClassDataSource property
}

[EngineTest(ExpectedResult.Pass)]
public class DirectServerTests
{
    [ClassDataSource<DirectTestServer>(Shared = SharedType.PerClass)]
    public required DirectTestServer Server { get; set; } = default!;

    [Test]
    public async Task DirectTest1()
    {
        Console.WriteLine($"DirectTest1 - Container ID: {Server.Container.Id}");
        await Assert.That(Server.Container.IsDisposed).IsFalse();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class InheritedServerTests
{
    [ClassDataSource<InheritedTestServer>(Shared = SharedType.PerClass)]
    public required InheritedTestServer Server { get; set; } = default!;

    [Test]
    public async Task InheritedTest1()
    {
        Console.WriteLine($"InheritedTest1 - Container ID: {Server.Container.Id}");
        await Assert.That(Server.Container.IsDisposed).IsFalse();
    }
}