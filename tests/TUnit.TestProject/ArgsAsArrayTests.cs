using TUnit.Assertions.Enums;

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

    // Issue #6678: a data source can hand a trailing array parameter a single array whose runtime
    // type differs from the parameter's — here an object[] from a MatrixAttribute subclass for a
    // ConfigKind[] parameter. It must be converted element-wise, not forced into one element.
    [Test]
    [MatrixDataSource]
    public async Task TrailingArray_ObjectArrayFromMatrix(
        [MatrixArray([ConfigKind.Ai, ConfigKind.Sample], [ConfigKind.Sample, ConfigKind.NoWindow, ConfigKind.Mcp])]
        ConfigKind[] configs)
    {
        ConfigKind[] expected = configs.Length == 2
            ? [ConfigKind.Ai, ConfigKind.Sample]
            : [ConfigKind.Sample, ConfigKind.NoWindow, ConfigKind.Mcp];

        await Assert.That(configs).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }

    [Test]
    [MatrixDataSource]
    public async Task TrailingParamsArray_ObjectArrayFromMatrix(
        [MatrixArray([ConfigKind.Ai, ConfigKind.Mcp])] params ConfigKind[] configs)
    {
        await Assert.That(configs).IsEquivalentTo([ConfigKind.Ai, ConfigKind.Mcp], CollectionOrdering.Matching);
    }

    // A rank-1 array with a non-zero lower bound (`object[*]`) must convert too — the loop has to
    // start at GetLowerBound(0), not 0.
    [Test]
    [MatrixDataSource]
    public async Task TrailingArray_NonZeroLowerBoundArrayFromMatrix(
        [NonZeroLowerBoundMatrix] ConfigKind[] configs)
    {
        await Assert.That(configs).IsEquivalentTo([ConfigKind.NoWindow, ConfigKind.Mcp], CollectionOrdering.Matching);
    }

    // C# params expansion is preserved: an int[] is itself an `object`, so it lands as one element
    // of a `params object[]` rather than being spread element-wise. (An int[] is not an object[],
    // so ArgumentsAttribute's own params binding delivers it as a single argument.)
    [Test]
    [Arguments(new int[] { 1, 2 })]
    public async Task ParamsObjectArray_ArrayArgumentStaysSingleElement(params object[] values)
    {
        await Assert.That(values).HasCount(1);
        await Assert.That(values[0]).IsTypeOf<int[]>();
    }
}

public enum ConfigKind
{
    Ai,
    Sample,
    NoWindow,
    Mcp
}

// Mirrors the community helper from discussion #4596: each constructor argument is one matrix
// value, so the decorated parameter receives a whole (object[]) array per test case.
public sealed class MatrixArrayAttribute(object[] first, object[]? second = null, object[]? third = null) : MatrixAttribute
{
    public override object?[] GetObjects(DataGeneratorMetadata dataGeneratorMetadata)
        => new object?[] { first, second, third }.Where(array => array is not null).ToArray();
}

// Supplies a single rank-1 object array whose lower bound is 1 rather than 0.
public sealed class NonZeroLowerBoundMatrixAttribute : MatrixAttribute
{
    public override object?[] GetObjects(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var array = Array.CreateInstance(typeof(object), [2], [1]);
        array.SetValue(ConfigKind.NoWindow, 1);
        array.SetValue(ConfigKind.Mcp, 2);
        return [array];
    }
}
