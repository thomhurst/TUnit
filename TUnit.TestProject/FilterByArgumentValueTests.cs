namespace TUnit.TestProject;

public class FilterByArgumentValueTests
{
    [Test]
    [Arguments("alpha")]
    [Arguments("beta")]
    [Arguments("gamma")]
    public void Test(string value)
    {
    }
}
