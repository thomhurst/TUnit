namespace TUnit.Core.Models;

public class ClassHookContext
{
    public required Type ClassType { get; init; }
    
    public List<TestContext> Tests { get; } = [];

    public int TestCount => Tests.Count;
}