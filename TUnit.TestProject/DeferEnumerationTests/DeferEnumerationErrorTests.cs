namespace TUnit.TestProject.DeferEnumerationTests;

// Designed to fail: a deferred data source that throws. The failure must surface as a single failed
// placeholder result at runtime, NOT crash the whole discovery phase. Filter explicitly to run.
public class DeferEnumerationErrorTests
{
    public static IEnumerable<int> Throws() => throw new InvalidOperationException("Boom from data source");

    [Test]
    [MethodDataSource(nameof(Throws), DeferEnumeration = true)]
    public async Task DeferredThrowing(int value)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(0);
    }
}
