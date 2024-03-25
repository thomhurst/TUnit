using TUnit.Core;

namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class Base1 : Base2
{
    [OnlyOnceSetUp]
    public static Task Base1OneTimeSetup()
    {
        return Task.CompletedTask;
    }
}