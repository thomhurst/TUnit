namespace TUnit.Assertions.Tests;

public class CustomCollectionTests
{
    public class CustomCollection(string title) : List<string>
    {
        public string Title { get; } = title;
    }

    [Test]
    public async Task Test()
    {
        var collection = new CustomCollection("alphabet") { "A", "B", "C" };

        // Custom collection types now work directly without casts
        await Assert.That(collection).Contains(x => x == "A");
    }
}
