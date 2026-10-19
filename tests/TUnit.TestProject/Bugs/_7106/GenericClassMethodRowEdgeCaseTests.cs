using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// Edge cases for method-level [Arguments] rows that infer a generic class's type arguments (#7106).

// A test method with an overload must resolve to its own counterpart on the constructed type
// rather than failing with an ambiguous name lookup.
[EngineTest(ExpectedResult.Pass)]
public class OverloadedGenericClassMethodRows<T>
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Run(T value)
    {
        await Assert.That(value).IsTypeOf<int>();
    }

    public Task Run(T value, int other) => Task.CompletedTask;
}

// An array argument for a T[] parameter infers T from the array's element type, in both modes.
[EngineTest(ExpectedResult.Pass)]
public class GenericClassArrayMethodRows<T>
{
    [Test]
    [Arguments(new[] { 1, 2 }, 0)]
    [Arguments(new[] { "a" }, 0)]
    public async Task Run(T[] values, int other)
    {
        await Assert.That(values.Length).IsGreaterThan(0);
        await Assert.That(values[0]).IsTypeOf<T>();
    }
}

// An array argument for an IEnumerable<T> parameter infers T through the interface the array implements.
[EngineTest(ExpectedResult.Pass)]
public class GenericClassEnumerableMethodRow<T>
{
    [Test]
    [Arguments(new[] { "a", "b" }, 0)]
    public async Task Run(IEnumerable<T> values, int other)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(values.Count()).IsEqualTo(2);
    }
}

// The row's string[] infers T = string, which the [GenerateGenericTest] does not list. In both modes it
// runs on its own instantiation and, like any row on the attribute's instantiation, on T = object.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(object))]
public class GenerateGenericTestWithArrayMethodRow<T>
{
    [Test]
    [Arguments(new[] { "a" }, 0)]
    public async Task Run(T[] values, int other)
    {
        await Assert.That(typeof(T) == typeof(string) || typeof(T) == typeof(object)).IsTrue();
        await Assert.That(values).IsEquivalentTo(new object[] { "a" });
    }
}
