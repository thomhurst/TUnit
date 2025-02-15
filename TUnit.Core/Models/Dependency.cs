namespace TUnit.Core;

internal record Dependency(DiscoveredTest Test, bool ProceedOnFailure)
{
    public TestDetails TestDetails => Test.TestDetails;
    
    public virtual bool Equals(Dependency? other)
    {
        return other?.TestDetails.TestId == TestDetails.TestId;
    }

    public override int GetHashCode()
    {
        return TestDetails.TestId.GetHashCode();
    }
}