namespace TUnit.TestProject.Bugs._3958;

/// <summary>
/// Simulates the base IntegrationTestBase class that has a ClassDataSource property
/// for a factory-like class (e.g., WebApplicationFactory).
/// </summary>
public abstract class IntegrationTestBase
{
    [ClassDataSource<FactoryClass>(Shared = SharedType.None)]
    public required FactoryClass Factory { get; init; }
}
