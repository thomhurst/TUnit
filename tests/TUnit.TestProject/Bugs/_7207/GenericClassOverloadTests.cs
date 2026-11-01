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
