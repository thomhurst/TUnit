using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7146;

// A row of a generic test may leave out trailing optional parameters, which then take their
// declared defaults, as on a non-generic class and in reflection mode (#7146).
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class GenericClassOptionalParameterTests<T>(T value)
{
    [Test]
    [Arguments(null)]
    public async Task Null_Row_Omits_Optional(T? arg, int other = 7)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
        // T is int, so the unconstrained T? is int and the null row value becomes default(int).
        await Assert.That(arg).IsEqualTo(default(T));
        await Assert.That(other).IsEqualTo(7);
    }

    [Test]
    [Arguments("a")]
    public async Task Row_Omits_Optional(string first, int other = 7)
    {
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(other).IsEqualTo(7);
    }

    [Test]
    [Arguments("a", 3)]
    public async Task Row_Supplies_Optional(string first, int other = 7)
    {
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(other).IsEqualTo(3);
    }

    [Test]
    [Arguments("a")]
    public async Task Omits_Generic_And_Several_Optionals(string first, T? generic = default, string text = "declared", CancellationToken token = default)
    {
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(generic).IsEqualTo(default(T));
        await Assert.That(text).IsEqualTo("declared");
        await Assert.That(token.CanBeCanceled).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericMethodOptionalParameterTests
{
    [Test]
    [Arguments(5)]
    public async Task Inferred_Method_Omits_Optional<T>(T arg, int other = 7)
    {
        await Assert.That(arg).IsEqualTo((T)(object)5);
        await Assert.That(other).IsEqualTo(7);
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(5)]
    public async Task Generated_Method_Omits_Optional<T>(int arg, T? other = default, string text = "declared")
    {
        await Assert.That(arg).IsEqualTo(5);
        await Assert.That(other).IsEqualTo(default(T));
        await Assert.That(text).IsEqualTo("declared");
    }
}

// A row that omits an optional must still call the test method, not a shorter sibling overload
// that the omitted-argument call would also match.
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class GenericClassOptionalOverloadTests<T>(T value)
{
    [Test]
    [Arguments("a")]
    public async Task Omits_Optional_Beside_Shorter_Overload(string first, int other = 7)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(other).IsEqualTo(7);
    }

    internal Task Omits_Optional_Beside_Shorter_Overload(string first)
        => throw new InvalidOperationException("Bound to the wrong overload");

    // An overload that differs only in the type of an optional parameter. The row supplies every
    // value, but a generated call that left the optional out would be ambiguous between the two,
    // and the project would not compile.
    [Test]
    [Arguments("a", 3)]
    public async Task Overloaded_By_Optional(string first, int other = 7)
    {
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(other).IsEqualTo(3);
    }

    internal Task Overloaded_By_Optional(string first, string other = "x")
        => throw new InvalidOperationException("Bound to the wrong overload");
}

[EngineTest(ExpectedResult.Pass)]
public class GenericMethodOptionalOverloadTests
{
    [Test]
    [Arguments(5)]
    public async Task Inferred_Method_Beside_Shorter_Overload<T>(T arg, int other = 7)
    {
        await Assert.That(arg).IsEqualTo((T)(object)5);
        await Assert.That(other).IsEqualTo(7);
    }

    internal Task Inferred_Method_Beside_Shorter_Overload<T>(T arg)
        => throw new InvalidOperationException("Bound to the wrong overload");
}
