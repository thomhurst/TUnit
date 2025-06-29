namespace TUnit.TestProject;

public class ArgsAsArrayTests
{
    [Test]
    [Arguments("arg1", "arg2", "arg3")]
    public void Params(params string[] arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }

    [Test]
    [Arguments("arg1", "arg2", "arg3")]
    public void NonParams(string[] arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }

    [Test]
    [Arguments("arg1", "arg2", "arg3")]
    public void ParamsEnumerable(params IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }

    [Test]
    [Arguments("arg1", "arg2", "arg3")]
    public void Enumerable(IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }

    [Test]
    [Arguments(1, "arg1", "arg2", "arg3")]
    public void Following_Non_Params(int i, IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }
}
