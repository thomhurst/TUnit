using TUnit.Core;

namespace TUnit.TestProject;

public static class GlobalSetUpCleanUp
{
    [AssemblySetUp]
    public static void BlahSetUp()
    {
        // Dummy method
    }
    
    [AssemblySetUp]
    public static void BlahSetUp2()
    {
        // Dummy method
    }

    [AssemblyCleanUp]
    public static Task BlahCleanUp()
    {
        return Task.CompletedTask;
    }
}