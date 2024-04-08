using TUnit.Core;

namespace TUnit.TestProject;

public static class GlobalSetUpCleanUp
{
    [AssemblySetUp]
    public static void BlahSetUp()
    {
    }
    
    [AssemblySetUp]
    public static void BlahSetUp2()
    {
    }

    [AssemblyCleanUp]
    public static Task BlahCleanUp()
    {
        return Task.CompletedTask;
    }
}