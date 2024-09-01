using System.Diagnostics;

namespace MSTestTimer;

[TestClass]
public class Tests
{
    [TestMethod]
    [DynamicData(nameof(Repeat), DynamicDataSourceType.Method)]
    public async Task TestMethod1(int _)
    {
        await Task.Delay(50);
    }

    public static IEnumerable<object[]> Repeat()
    {
        return Enumerable.Range(0, 10).Select(i => (object[])[i]);
    }
}