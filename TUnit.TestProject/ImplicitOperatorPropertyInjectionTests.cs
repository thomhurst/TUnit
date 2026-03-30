using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ImplicitOperatorPropertyInjectionTests
{
    [ClassDataSource<ImplicitDbFixture>]
    public required ImplicitDbContext Db { get; init; }

    [Test]
    public async Task Injection_Works_With_Implicit_Operator()
    {
        await Assert.That(Db).IsNotNull();
        await Assert.That(Db.ConnStr).IsEqualTo("test-conn");
    }
}

public class ImplicitDbContext(string connStr)
{
    public string ConnStr { get; } = connStr;
}

public class ImplicitDbFixture : IAsyncDisposable
{
    public static implicit operator ImplicitDbContext(ImplicitDbFixture f) => new("test-conn");
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
