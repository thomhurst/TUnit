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

    [Test]
    // This should work - single type params  
    [Arguments("Foo", typeof(Foo))]
    [Arguments("Bar", typeof(Bar))]
    // this works - multiple types
    [Arguments("FooBar", typeof(Foo), typeof(Bar))]
    public void ParamsTypesSingle(string reference, params Type[] typeName)
    {
        var result = string.Join("", typeName.Select(type => type.Name));
        Console.WriteLine($"Expected: {reference}, Got: {result}");
    }

    public record Foo();
    public record Bar();
}
