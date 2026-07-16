using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments("Hello")]
public class GenericHooks<T>(T arg)
{
    [Before(HookType.Test)]
    public void Before()
    {
        Console.WriteLine(arg);
    }

    [After(HookType.Test)]
    public void After()
    {
        Console.WriteLine(arg);
    }

    [Test]
    public void Test()
    {
        Console.WriteLine(arg);
    }
}
