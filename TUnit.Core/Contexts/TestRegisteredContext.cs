using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test registration phase
/// </summary>
public class TestRegisteredContext : BeforeTestContext
{
    public TestRegisteredContext(string testName, string displayName) : base(testName, displayName)
    {
    }
    
    public DiscoveredTest DiscoveredTest { get; set; } = null!;
    
    public void SetTestExecutor(ITestExecutor executor)
    {
        if (InternalDiscoveredTest != null)
        {
            InternalDiscoveredTest.TestExecutor = executor;
        }
    }
}