namespace TUnit.TestProject;

public static class GlobalSetUpCleanUp
{
    [Before(Assembly)]
    public static void BlahSetUp()
    {
        // Dummy method
    }

    [Before(Assembly)]
    public static void BlahSetUp2()
    {
        // Dummy method
    }

    [After(Assembly)]
    public static Task BlahCleanUp()
    {
        return Task.CompletedTask;
    }
}
