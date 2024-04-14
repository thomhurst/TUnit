using TUnit.Core;

namespace TUnit.TestProject.BeforeAllTestsInClassWithBaseTests;

public class Base1 : Base2
{
    [BeforeAllTestsInClass]
    public static Task Base1OneTimeSetup()
    {
        return Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public Task Base1SetUp()
    {
        return Task.CompletedTask;
    }
}