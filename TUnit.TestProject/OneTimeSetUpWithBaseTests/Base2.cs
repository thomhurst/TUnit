using TUnit.Core;

namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class Base2
{
    [OnlyOnceSetUp]
    public static Task Base2OneTimeSetup()
    {
        return Task.CompletedTask;
    }
}