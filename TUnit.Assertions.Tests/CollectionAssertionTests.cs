namespace TUnit.Assertions.Tests;

public class CollectionAssertionTests
{
    [Test]
    public async Task IsEmpty()
    {
        var items = new List<int>();

        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task IsEmpty2()
    {
        var items = new List<int>();

        await Assert.That(() => items).IsEmpty();
    }

    [Test]
    public async Task Count()
    {
        var items = new List<int>();

        await Assert.That(items).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Count2()
    {
        var items = new List<int>();

        await Assert.That(() => items).Count().IsEqualTo(0);
    }
}
