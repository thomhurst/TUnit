namespace TUnit.Core;

/// <summary>
/// Context for after test execution phase
/// </summary>
public class AfterTestContext : TestContext
{
    public AfterTestContext(string testName, string displayName) : base(testName, displayName)
    {
    }
}