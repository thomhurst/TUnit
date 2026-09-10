using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ParamsArgumentsTests
{
    [Test]
    [Arguments(2, 2)]
    [Arguments(20, 3, Operation.Kind.A)]
    [Arguments(20, 6, Operation.Kind.Deposit, Operation.Kind.B)]
    public void GetOperations(int dayDelta, int expectedNumberOfOperation, params Operation.Kind[] kinds)
    {
        // Test implementation
    }

    [Test]
    [Arguments("Foo", typeof(string))]
    public async Task SingleTypeInParamsArray(string name, params Type[] types)
    {
        await Assert.That(name).IsEqualTo("Foo");
        await Assert.That(types).IsNotNull();
        await Assert.That(types.Length).IsEqualTo(1);
        await Assert.That(types[0]).IsEqualTo(typeof(string));
    }

    [Test]
    [Arguments("Bar", typeof(int), typeof(string))]
    public async Task MultipleTypesInParamsArray(string name, params Type[] types)
    {
        await Assert.That(name).IsEqualTo("Bar");
        await Assert.That(types).IsNotNull();
        await Assert.That(types.Length).IsEqualTo(2);
        await Assert.That(types[0]).IsEqualTo(typeof(int));
        await Assert.That(types[1]).IsEqualTo(typeof(string));
    }

    [Test]
    [Arguments("Baz")]
    public async Task EmptyParamsArray(string name, params Type[] types)
    {
        await Assert.That(name).IsEqualTo("Baz");
        await Assert.That(types).IsNotNull();
        await Assert.That(types.Length).IsEqualTo(0);
    }

    [Test]
    [Arguments]
    [Arguments("a")]
    [Arguments("a", "b")]
    [Arguments("a", "b", "c")]
    public async Task ParamsOnlyWithEmptyArguments(params string[] args)
    {
        // When [Arguments] has no values, params should be an empty array, not null
        await Assert.That(args).IsNotNull();
    }

    [Test]
    [Arguments(1, "single")]
    public async Task SingleStringInParamsArray(int id, params string[] values)
    {
        await Assert.That(id).IsEqualTo(1);
        await Assert.That(values).IsNotNull();
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo("single");
    }

    [Test]
    [Arguments(2, "first", "second", "third")]
    public async Task MultipleStringsInParamsArray(int id, params string[] values)
    {
        await Assert.That(id).IsEqualTo(2);
        await Assert.That(values).IsNotNull();
        await Assert.That(values.Length).IsEqualTo(3);
        await Assert.That(values[0]).IsEqualTo("first");
        await Assert.That(values[1]).IsEqualTo("second");
        await Assert.That(values[2]).IsEqualTo("third");
    }
}

public class Operation
{
    public enum Kind
    {
        A,
        B,
        Deposit
    }
}
