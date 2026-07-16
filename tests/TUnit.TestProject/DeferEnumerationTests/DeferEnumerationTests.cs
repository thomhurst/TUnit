namespace TUnit.TestProject.DeferEnumerationTests;

// Tests for DeferEnumeration: the data source is not enumerated during discovery (one placeholder node
// is shown) and is expanded into individual cases at runtime. Filter with /*/*DeferEnumeration*/*.
public class DeferEnumerationTests
{
    public static IEnumerable<int> TenValues() => Enumerable.Range(0, 10);

    [Test]
    [MethodDataSource(nameof(TenValues), DeferEnumeration = true)]
    public async Task Deferred(int value)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [Repeat(2)]
    [MethodDataSource(nameof(TenValues), DeferEnumeration = true)]
    public async Task DeferredWithRepeat(int value)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(0);
    }
}
