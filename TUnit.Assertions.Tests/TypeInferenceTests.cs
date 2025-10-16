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
        // Contains with predicate can be awaited to get the found item
        // Or chained with .And to continue collection assertions
        IEnumerable<int> enumerable = [1, 2, 3];

        try
        {
            // Test 1: Await to get the found item
            var item = await Assert.That(enumerable).Contains(x => x > 1);
            await Assert.That(item).IsGreaterThan(0);

            // Test 2: Chain with .And for collection assertions
            await Assert.That(enumerable)
                .Contains(x => x > 1)
                .And
                .Contains(x => x > 2);  // Can chain multiple Contains
        }
        catch
        {
            // Don't care for assertion failures
            // Just want to surface any compiler errors if we knock out type inference
        }
    }
}
