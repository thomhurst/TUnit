namespace TUnit.TestProject.DeferEnumerationTests;

public class DeferredDependencyTests
{
    private static int _completed;
    private static int _dependentCompleted;

    public static IEnumerable<int> Rows() => Enumerable.Range(1, 3);
    public static IEnumerable<int> DependentRows() => Enumerable.Range(1, 2);

    [Test]
    [MethodDataSource(nameof(Rows), DeferEnumeration = true)]
    public async Task Deferred(int row)
    {
        await Task.Yield();
        Interlocked.Increment(ref _completed);
    }

    [Test]
    [DependsOn(nameof(Deferred))]
    [MethodDataSource(nameof(DependentRows), DeferEnumeration = true)]
    public async Task Depends_On_Deferred(int row)
    {
        await Assert.That(_completed).IsEqualTo(3);
        Interlocked.Increment(ref _dependentCompleted);
    }

    [Test]
    [DependsOn(nameof(Depends_On_Deferred))]
    public async Task Waits_For_All_Rows()
    {
        await Assert.That(_completed).IsEqualTo(3);
        await Assert.That(_dependentCompleted).IsEqualTo(2);
    }
}
