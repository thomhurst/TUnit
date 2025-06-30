using TUnit.Core;

namespace TUnit.TestProject;

[Property("TestProp", "TestValue")]
public class FilterDebugTest
{
    [Test]
    public void SimpleTestWithProperty()
    {
        Console.WriteLine("Test with property executed!");
    }
}

public class NoPropertyTest
{
    [Test]
    public void SimpleTestNoProperty()
    {
        Console.WriteLine("Test without property executed!");
    }
}
