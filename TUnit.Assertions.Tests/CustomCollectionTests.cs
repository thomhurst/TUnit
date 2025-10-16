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

        // Cast to IEnumerable<string> to ensure collection assertion is used
        await Assert.That((IEnumerable<string>)collection).Contains(x => x == "A");
    }
}
