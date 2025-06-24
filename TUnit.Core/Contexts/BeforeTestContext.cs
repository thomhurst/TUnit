namespace TUnit.Core;

/// <summary>
/// Context for before test execution phase
/// </summary>
public class BeforeTestContext : TestContext
{
    public BeforeTestContext(string testName, string displayName) : base(testName, displayName)
    {
    }
}