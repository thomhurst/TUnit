namespace TUnit.Example.Asp.Net.TestProject;

public abstract class TestsBase
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }
}
