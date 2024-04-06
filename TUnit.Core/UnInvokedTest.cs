namespace TUnit.Core;

public record UnInvokedTest
{
    public required string Id { get; init; }
    public required TestContext TestContext { get; init; }
    
    // List per Type - So new one for base classes
    public required List<OneTimeSetUpModel> OneTimeSetUps { get; init; }
    public required List<Func<Task>> BeforeEachTestSetUps { get; init; }
    
    public required object TestClass { get; init; }
    public required Func<Task> TestBody { get; init; }
    
    public required List<Func<Task>> AfterEachTestCleanUps { get; init; }
}