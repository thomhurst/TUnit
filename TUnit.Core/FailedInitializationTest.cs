namespace TUnit.Core;

public class FailedInitializationTest
{
    public required string TestId { get; init; }
    public required Type TestClass { get; init; }
    public required Type[] ParameterTypeFullNames { get; init; }
    public required Type ReturnType { get; init; }
    public required string TestName { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public required Exception Exception { get; init; } 
}