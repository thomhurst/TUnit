namespace TUnit.TestProject.Bugs._7222;

// A method-level [GenerateGenericTest] that lists no type arguments, or the wrong number of them, fails as a
// test of its own in both modes, with the same message (#7222). Source-generated mode used to drop it silently.
//
// Not marked [EngineTest]: the failures are intentional. TUnit.Engine.Tests (Issue7222Tests) checks each
// method's outcome by name in both modes.
public class GenerateGenericTestArityTests
{
    [Test]
    [GenerateGenericTest(typeof(int), typeof(string))]
    public void TooManyTypeArguments<T>()
    {
    }

    [Test]
    [GenerateGenericTest]
    public void NoTypeArguments<T>()
    {
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    public void TooFewTypeArguments<T1, T2>()
    {
    }

    // The valid one runs; the wrongly sized one between them fails on its own.
    [Test]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(int), typeof(string))]
    [GenerateGenericTest(typeof(string))]
    public async Task MixedArity<T>()
        => await Assert.That(typeof(T) == typeof(int) || typeof(T) == typeof(string)).IsTrue();

    // An array type argument counts like any other type.
    [Test]
    [GenerateGenericTest(typeof(int[]))]
    public async Task ArrayTypeArgument<T>()
        => await Assert.That(typeof(T)).IsEqualTo(typeof(int[]));
}

// Reflection mode resolves the method on each concrete class, so the wrongly sized one fails on each.
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(long))]
public class GenericClassGenerateGenericTestArityTests<TClass>
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest(typeof(string), typeof(int))]
    public async Task MixedArity<TMethod>()
        => await Assert.That(typeof(TMethod)).IsEqualTo(typeof(string));
}
