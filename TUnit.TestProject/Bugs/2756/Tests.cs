namespace TUnit.TestProject.Bugs._2756;

public class Tests
{
    private static DateTimeOffset? _start;

    [Test]
    [MethodDataSource(nameof(Ints))]
    public async Task Test1(int i)
    {
        await Task.Delay(100);
        await Assert.That(i).IsGreaterThan(0);
    }

    public static IEnumerable<int> Ints() => Enumerable.Range(1, 10000);

    [After(Class)]
    public static void Before()
    {
        _start = DateTimeOffset.UtcNow;
    }

    [After(Class)]
    public static void After()
    {
        Console.WriteLine(@$"Test duration: {(DateTimeOffset.UtcNow - _start!.Value).TotalMilliseconds} ms");
    }
}
