namespace TUnit.Core;

internal record Dependency(DiscoveredTest Test, bool ProceedOnFailure)
{
    public TestDetails TestDetails => Test.TestDetails;
}