namespace TUnit.TestProject.Bugs._7105;

// Regression fixtures for https://github.com/thomhurst/TUnit/issues/7105.
// Generic tests must get the same tree-node path and the same display name in
// source-generated and reflection mode. TUnit.Engine.Tests/Issue7105Tests asserts on them.

public class GenericMethodNames
{
    // T is inferred per row: reflection resolves it while building the test,
    // source-gen emits one instantiation per [Arguments] row.
    [Test]
    [Arguments(5)]
    [Arguments("s")]
    public async Task Method<T>(T value) => await Assert.That(value is not null).IsTrue();

    // Closed by [GenerateGenericTest]: reflection uses MakeGenericMethod.
    [Test]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(GenericNamePayload))]
    [GenerateGenericTest(typeof(List<int>))]
    [GenerateGenericTest(typeof(int?))]
    public async Task Generated<T>() => await Assert.That(typeof(T)).IsNotNull();
}

[Arguments(5)]
public class GenericClassNames<T>(T value)
{
    [Test]
    public async Task Plain() => await Assert.That(value is not null).IsTrue();
}

public class GenericNamePayload;
