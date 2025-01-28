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
                .Contains(x => x > 1)
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
                .And.HasCount().EqualTo(0);
        }
        catch
        {
            // Don't care for assertion failures
            // Just want to surface any compiler errors if we knock out type inference
        }
    }
}