using TUnit.Core;

namespace TUnit.TestProject;

public enum Status
{
    Draft,
    Pending,
    Published,
    Archived,
    Deleted
}

// This is what the user might expect to work - a custom exclusion attribute
public class MatrixExclusionStatusAttribute : MatrixExclusionAttribute
{
    public MatrixExclusionStatusAttribute(Status status) : base(status)
    {
    }
}

public class MatrixExclusionBugTest
{
    [Test]
    [MatrixDataSource]
    [MatrixExclusionStatus(Status.Draft)]
    public async Task Should_Filter_By_Status(Status status)
    {
        // Should generate 4 tests (all statuses except Draft)
        await Task.CompletedTask;
    }
    
    [Test]
    [MatrixDataSource]
    [MatrixExclusion(Status.Draft)]  // This should work with the base attribute
    public async Task Should_Filter_By_Status_Base(Status status)
    {
        // Should generate 4 tests (all statuses except Draft)  
        await Task.CompletedTask;
    }
}