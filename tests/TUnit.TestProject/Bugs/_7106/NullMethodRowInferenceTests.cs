using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// A null element of a method row's params array infers object for the class type argument, as Roslyn
// types it, in both modes. [Arguments(null)] passes a null params array instead, which infers no type.

[EngineTest(ExpectedResult.Pass)]
public class TypedNullMethodRowInfersObject<T>
{
    [Test]
    [Arguments((string?)null)]
    public async Task Run(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(value).IsNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class NullElementMethodRowInfersObject<T>
{
    [Test]
    [Arguments(null, 1)]
    public async Task Run(T value, int other)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(value).IsNull();
        await Assert.That(other).IsEqualTo(1);
    }
}

// When several parameters use T, the first one fixes it, as in the source generator: the null first
// value infers object, and the later 1 does not replace it with int.
[EngineTest(ExpectedResult.Pass)]
public class NullFirstOfSharedTypeParameterMethodRow<T>
{
    [Test]
    [Arguments(null, 1)]
    public async Task Run(T first, T second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(first).IsNull();
        await Assert.That((object?)second).IsEqualTo(1);
    }
}

// The same row lands on the instantiation [GenerateGenericTest(typeof(object))] lists, so the class
// runs once as C<object> in both modes, with no extra C<int> in reflection mode.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(object))]
public class NullFirstOfSharedTypeParameterWithGenerateGenericTest<T>
{
    [Test]
    [Arguments(null, 1)]
    public async Task Run(T first, T second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(first).IsNull();
        await Assert.That((object?)second).IsEqualTo(1);
    }
}

// The null row infers no type, so it neither runs nor is reported as breaking the struct constraint.
[EngineTest(ExpectedResult.Pass)]
public class NullArrayMethodRowOnConstrainedClass<T> where T : struct
{
    [Test]
    [Arguments(1)]
    [Arguments(null)]
    public async Task Run(T value)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
    }
}

// Each class row reaches its own test: the values seen across the tests are exactly the rows' values.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[Arguments(3)]
public class GenericClassSameTypeRowValues<T>(T value)
{
    [Test]
    public void Run()
    {
        Console.WriteLine($"Issue7106 class row value: {value}");
    }
}
