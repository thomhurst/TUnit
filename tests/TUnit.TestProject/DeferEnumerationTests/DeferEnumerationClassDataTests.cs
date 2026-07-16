namespace TUnit.TestProject.DeferEnumerationTests;

// Exercises DeferEnumeration on a CLASS-level (constructor) data source, so the deferral flows through
// the metadata.ClassDataSources path. 5 class instances x 1 method = 5 cases (+ 1 placeholder container).
[MethodDataSource(nameof(ClassValues), DeferEnumeration = true)]
public class DeferEnumerationClassDataTests(int value)
{
    public static IEnumerable<int> ClassValues() => Enumerable.Range(0, 5);

    [Test]
    public async Task ClassDeferred()
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(0);
    }
}
