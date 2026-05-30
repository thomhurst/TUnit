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
    public void ParamsEnumerable(params IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }

    [Test]
    [Arguments(1, "arg1", "arg2", "arg3")]
    public void Following_Non_Params(int i, params IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            Console.WriteLine(argument);
        }
    }

    // Issue #6120: a plain (non-params) array parameter collects loose argument values,
    // so `[Arguments(["a", "b"])]` (a collection expression, identical to `[Arguments("a", "b")]`)
    // maps onto a single `string[]` parameter.
    [Test]
    [Arguments(["Chloe"])]
    [Arguments(["Skipper", "Lucy"])]
    public async Task NonParamsStringArray(string[] names)
    {
        await Assert.That(names).IsNotEmpty();
        await Assert.That(names.All(n => !string.IsNullOrEmpty(n))).IsTrue();
    }

    [Test]
    [Arguments("Chloe")]
    public async Task NonParamsStringArray_SingleValue(string[] names)
    {
        await Assert.That(names).IsEquivalentTo(["Chloe"]);
    }

    [Test]
    [Arguments(["Skipper", "Lucy"])]
    public async Task NonParamsStringArray_MultipleValues(string[] names)
    {
        await Assert.That(names).IsEquivalentTo(["Skipper", "Lucy"]);
    }

    [Test]
    [Arguments(1, 2, 3)]
    public async Task NonParamsIntArray(int[] numbers)
    {
        await Assert.That(numbers).IsEquivalentTo([1, 2, 3]);
    }

    // The strongly-typed generic form is the unambiguous way to pass an array as a single value.
    [Test]
    [Arguments<string[]>(["Skipper", "Lucy"])]
    public async Task GenericStringArray(string[] names)
    {
        await Assert.That(names).IsEquivalentTo(["Skipper", "Lucy"]);
    }

    // Issue #6120: more loose values than the source generator emits static switch cases for
    // (cap is parameterCount + 5) must still bind, matching the unbounded reflection path.
    [Test]
    [Arguments("a", "b", "c", "d", "e", "f", "g", "h")]
    public async Task NonParamsStringArray_BeyondStaticCaseCap(string[] names)
    {
        await Assert.That(names).IsEquivalentTo(["a", "b", "c", "d", "e", "f", "g", "h"]);
    }
}
