namespace TUnit.Core;

public class FailedInitializationTest
{
    public required string TestId { get; init; }
    public required string TestName { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public required string DisplayName { get; set; }
    public required Exception Exception { get; init; } 
}