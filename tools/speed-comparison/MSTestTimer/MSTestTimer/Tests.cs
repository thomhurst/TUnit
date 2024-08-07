using System.Diagnostics;

namespace MSTestTimer;

[TestClass]
public class Tests
{
    private static readonly Stopwatch Stopwatch = new();

    [ClassInitialize]
    public static void Setup(TestContext _)
    {
        Stopwatch.Start();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        Console.WriteLine(Stopwatch.Elapsed);
    }
    
    [TestMethod]
    [DynamicData(nameof(Repeat), DynamicDataSourceType.Method)]
    public async Task TestMethod1(int _)
    {
        await Task.Delay(50);
    }

    public static IEnumerable<object[]> Repeat()
    {
        foreach (var i in Enumerable.Range(0, 1001))
        {
            yield return [i];
        }
    }
}