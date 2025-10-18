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

        // Custom collection types require cast to IEnumerable<T> for collection assertions
        // This is the documented workaround for C# type inference limitations
        await Assert.That((IEnumerable<string>)collection).Contains(x => x == "A");
    }
}
