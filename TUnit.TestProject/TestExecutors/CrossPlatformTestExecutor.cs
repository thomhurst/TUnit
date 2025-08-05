using TUnit.Core;

namespace TUnit.TestProject.TestExecutors;

public class CrossPlatformTestExecutor : DedicatedThreadExecutor
{
    public static readonly AsyncLocal<bool> IsRunningInTestExecutor = new();
    
    protected override void ConfigureThread(Thread thread)
    {
        // Set a custom thread name that works on all platforms
        thread.Name = "CrossPlatformTestExecutor";
        
        // Set an AsyncLocal value to track execution context
        IsRunningInTestExecutor.Value = true;
    }
}