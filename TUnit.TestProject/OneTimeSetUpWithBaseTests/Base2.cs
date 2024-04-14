using TUnit.Core;

namespace TUnit.TestProject.BeforeAllTestsInClassWithBaseTests;

public class Base2
{
    [BeforeAllTestsInClass]
    public static Task Base2OneTimeSetup()
    {
        return Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public Task Base2SetUp()
    {
        return Task.CompletedTask;
    }
}