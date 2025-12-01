namespace TUnit.TestProject.Bugs._3958;

/// <summary>
/// Simulates PulsarIntegrationTestBase that:
/// 1. Extends IntegrationTestBase (which has its own ClassDataSource property)
/// 2. Adds another ClassDataSource property for the wrapper class
/// This creates the multi-level inheritance with nested property injection.
/// </summary>
public abstract class DerivedIntegrationTestBase : IntegrationTestBase
{
    [ClassDataSource<WrapperClass>(Shared = SharedType.None)]
    public required WrapperClass BrokerHandlerWrapper { get; init; }
}
