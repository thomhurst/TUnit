namespace TUnit.Core;

internal record Dependency(DiscoveredTest Test, bool ProceedOnFailure)
{
    public TestDetails TestDetails => Test.TestDetails;
    
    public virtual bool Equals(Dependency? other)
    {
        return other?.TestDetails.IsSameTest(TestDetails) is true;
    }

    public override int GetHashCode()
    {
        return TestDetails.TestName.GetHashCode();
    }
}