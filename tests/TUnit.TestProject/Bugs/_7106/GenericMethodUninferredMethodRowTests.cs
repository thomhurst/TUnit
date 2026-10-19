using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// A generic method on a generic class whose class row fixes the class's type and whose
// [GenerateGenericTest] supplies the method's type: a method row with a null value is not dropped. A null
// [Arguments] value is an element of the params object?[] argument, so it infers object (#7092), and the
// row runs as Run<object>(null, 2) beside Run<string>("a", 1), in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[GenerateGenericTest(typeof(int))]
public class GenericMethodUninferredMethodRow<T>(T classValue)
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("a", 1)]
    [Arguments(null, 2)]
    public async Task Run<U>(U? value, int other)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(U)).IsEqualTo(value is null ? typeof(object) : typeof(string));
        await Assert.That(classValue).IsEqualTo((T)(object)1);
        await Assert.That(value is null && other == 2 || value is "a" && other == 1).IsTrue();
    }
}


// A single null value passes a null params array, which ArgumentsAttribute stores as one null value:
// it infers object as well, so the row runs as Run<object>(null) rather than being dropped.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[GenerateGenericTest(typeof(int))]
public class GenericMethodSingleNullMethodRow<T>(T classValue)
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("a")]
    [Arguments(null)]
    public async Task Run<U>(U? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(typeof(U)).IsEqualTo(value is null ? typeof(object) : typeof(string));
        await Assert.That(classValue).IsEqualTo((T)(object)1);
        await Assert.That(value is null or "a").IsTrue();
    }
}

// The same on a non-generic class.
[EngineTest(ExpectedResult.Pass)]
public class NonGenericClassGenericMethodNullMethodRow
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("a", 1)]
    [Arguments(null, 2)]
    public async Task Run<U>(U? value, int other)
    {
        await Assert.That(typeof(U)).IsEqualTo(value is null ? typeof(object) : typeof(string));
        await Assert.That(value is null && other == 2 || value is "a" && other == 1).IsTrue();
    }
}
