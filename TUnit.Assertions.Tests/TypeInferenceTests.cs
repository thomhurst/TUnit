namespace TUnit.Assertions.Tests;

public class TypeInferenceTests
{
    [Test]
    public async Task Enumerables()
    {
        IEnumerable<int> enumerable = [];

        try
        {
            await Assert.That(enumerable)
                .IsEmpty()
                .And
                .IsNotEmpty()
                .And
                .ContainsOnly(x => x > 1)
                .And
                .DoesNotContain(x => x > 1)
                .And
                .DoesNotContain(x => x > 1)
                .And
                .HasDistinctItems()
                .And
                .HasSingleItem()
                .And.HasCount().EqualTo(0)
                .And.HasCount(0);
        }
        catch
        {
            // Don't care for assertion failures
            // Just want to surface any compiler errors if we knock out type inference
        }
    }

    [Test]
    public async Task PredicateAssertionsReturnItem()
    {
        // Contains with predicate returns the found item, not the collection
        // This allows further assertions on the item itself
        IEnumerable<int> enumerable = [1, 2, 3];

        try
        {
            await Assert.That(enumerable)
                .Contains(x => x > 1)  // Returns Assertion<int> with the found item
                .And
                .IsGreaterThan(0);  // Can assert on the found item
        }
        catch
        {
            // Don't care for assertion failures
            // Just want to surface any compiler errors if we knock out type inference
        }
    }
}
