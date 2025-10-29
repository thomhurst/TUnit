using System.Threading.Tasks;

namespace UnifiedTests;

[TestClass]
public class MatrixTests
{
    [DataDrivenTest]
    [TestData(1)]
    [TestData(2)]
    [TestData(3)]
    [TestData(4)]
    [TestData(5)]
    [TestData(6)]
    [TestData(7)]
    [TestData(8)]
    [TestData(9)]
    [TestData(10)]
    [TestData(11)]
    [TestData(12)]
    [TestData(13)]
    [TestData(14)]
    [TestData(15)]
    [TestData(16)]
    [TestData(17)]
    [TestData(18)]
    [TestData(19)]
    [TestData(20)]
    [TestData(21)]
    [TestData(22)]
    [TestData(23)]
    [TestData(24)]
    [TestData(25)]
    public async Task Matrix_5x5_CombinationTest(int multiplier)
    {
        var results = new List<int>();
        for (var i = 1; i <= 5; i++)
        {
            results.Add(ComputeValue(multiplier, i));
        }

        var sum = results.Sum();
        var product = results.Aggregate(1, (acc, val) => acc * val);
    }

    [DataDrivenTest]
    [TestData(1, "small", true)]
    [TestData(1, "small", false)]
    [TestData(1, "medium", true)]
    [TestData(1, "medium", false)]
    [TestData(1, "large", true)]
    [TestData(1, "large", false)]
    [TestData(2, "small", true)]
    [TestData(2, "small", false)]
    [TestData(2, "medium", true)]
    [TestData(2, "medium", false)]
    [TestData(2, "large", true)]
    [TestData(2, "large", false)]
    [TestData(3, "small", true)]
    [TestData(3, "small", false)]
    [TestData(3, "medium", true)]
    [TestData(3, "medium", false)]
    [TestData(3, "large", true)]
    [TestData(3, "large", false)]
    [TestData(4, "small", true)]
    [TestData(4, "small", false)]
    [TestData(4, "medium", true)]
    [TestData(4, "medium", false)]
    [TestData(4, "large", true)]
    [TestData(4, "large", false)]
    [TestData(5, "small", true)]
    [TestData(5, "small", false)]
    [TestData(5, "medium", true)]
    [TestData(5, "medium", false)]
    [TestData(5, "large", true)]
    [TestData(5, "large", false)]
    public async Task Matrix_MultiParam_CombinationTest(int size, string category, bool enabled)
    {
        var result = ProcessConfiguration(size, category, enabled);
        var multiplier = GetMultiplier(category);
        var expected = enabled ? size * multiplier : 0;
    }

    [DataDrivenTest]
    [TestData(10, 20)]
    [TestData(10, 30)]
    [TestData(10, 40)]
    [TestData(10, 50)]
    [TestData(20, 20)]
    [TestData(20, 30)]
    [TestData(20, 40)]
    [TestData(20, 50)]
    [TestData(30, 20)]
    [TestData(30, 30)]
    [TestData(30, 40)]
    [TestData(30, 50)]
    [TestData(40, 20)]
    [TestData(40, 30)]
    [TestData(40, 40)]
    [TestData(40, 50)]
    [TestData(50, 20)]
    [TestData(50, 30)]
    [TestData(50, 40)]
    [TestData(50, 50)]
    public async Task Matrix_AsyncCombinationTest(int delayMs, int computeValue)
    {
        await Task.Delay(50);
        var result = await ComputeAsync(computeValue);
        var adjusted = result + delayMs;
    }

    private int ComputeValue(int multiplier, int value)
    {
        return multiplier * value;
    }

    private int ProcessConfiguration(int size, string category, bool enabled)
    {
        if (!enabled) return 0;
        var multiplier = GetMultiplier(category);
        return size * multiplier;
    }

    private int GetMultiplier(string category)
    {
        return category switch
        {
            "small" => 1,
            "medium" => 2,
            "large" => 3,
            _ => 0
        };
    }

    private async Task<int> ComputeAsync(int value)
    {
        await Task.Yield();
        return value * 2;
    }
}
