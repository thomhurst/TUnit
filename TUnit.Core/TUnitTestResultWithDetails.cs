namespace TUnit.Core;

internal record TUnitTestResultWithDetails : TUnitTestResult
{
    public required TestDetails TestDetails { get; init; }
}