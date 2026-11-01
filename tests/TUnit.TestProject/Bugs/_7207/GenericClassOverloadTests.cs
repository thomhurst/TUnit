namespace TUnit.TestProject.Bugs._7207;

[Arguments(1)]
public class GenericClassOverloadTests<T>(T value)
{
    [Test]
    [Arguments("a", 3)]
    public async Task Overloaded(string first, int other = 7)
    {
        await Assert.That(value).IsEqualTo((T) (object) 1);
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(other).IsEqualTo(3);
    }

    [Test]
    [Arguments("a", "b")]
    public async Task Overloaded(string first, string other = "x")
    {
        await Assert.That(value).IsEqualTo((T) (object) 1);
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(other).IsEqualTo("b");
    }
}

public class NonGenericClassOverloadTests
{
    [Test]
    [Arguments("a", 3)]
    public async Task Overloaded(string first, int other)
    {
        await Assert.That(other).IsEqualTo(3);
    }

    [Test]
    [Arguments("a", "b")]
    public async Task Overloaded(string first, string other)
    {
        await Assert.That(other).IsEqualTo("b");
    }
}

// Source generation emits both T[] and object as a placeholder object parameter, so the parameter
// metadata of these overloads matches and only the declaration site tells them apart.
[Arguments(1)]
public class GenericArrayOverloadTests<T>(T value)
{
    [Test]
    [Arguments(new[] { 1, 2 })]
    public async Task Overloaded(T[] values)
    {
        await Assert.That(value).IsEqualTo((T) (object) 1);
        await Assert.That(values.Length).IsEqualTo(2);
    }

    [Test]
    [Arguments("b")]
    public async Task Overloaded(object other)
    {
        await Assert.That(value).IsEqualTo((T) (object) 1);
        await Assert.That(other).IsEqualTo("b");
    }
}

// A filter that selects only Dependent must pull in every overload that [DependsOn] names.
public class DependsOnOverloadTests
{
    [Test]
    [Arguments("a", 3)]
    public async Task Overloaded(string first, int other)
    {
        await Assert.That(other).IsEqualTo(3);
    }

    [Test]
    [Arguments("a", "b")]
    public async Task Overloaded(string first, string other)
    {
        await Assert.That(other).IsEqualTo("b");
    }

    [Test]
    [DependsOn(nameof(Overloaded))]
    public async Task Dependent()
    {
        await Assert.That(TestContext.Current!.Dependencies.DependsOn).Count().IsEqualTo(2);
    }
}
