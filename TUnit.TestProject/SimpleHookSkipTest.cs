using TUnit.Core;

namespace TUnit.TestProject;

public class SimpleHookSkipTest
{
    public static bool SkippedHookExecuted = false;
    public static bool NormalHookExecuted = false;

    [Before(Test)]
    [Skip("This hook should be skipped")]
    public void SkippedHook()
    {
        SkippedHookExecuted = true;
    }

    [Before(Test)]
    public void NormalHook()
    {
        NormalHookExecuted = true;
    }

    [Test]
    public void TestMethod()
    {
        // Reset flags
        SkippedHookExecuted = false;
        NormalHookExecuted = false;
        
        // Test should run normally
    }
}