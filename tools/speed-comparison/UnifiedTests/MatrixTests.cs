using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class MatrixTests
{
    // 5x5x5 = 125 test combinations - shows how TUnit handles combinatorial expansion
#if TUNIT
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    [Arguments(10)]
    [Arguments(11)]
    [Arguments(12)]
    [Arguments(13)]
    [Arguments(14)]
    [Arguments(15)]
    [Arguments(16)]
    [Arguments(17)]
    [Arguments(18)]
    [Arguments(19)]
    [Arguments(20)]
    [Arguments(21)]
    [Arguments(22)]
    [Arguments(23)]
    [Arguments(24)]
    [Arguments(25)]
    public async Task Matrix_5x5_CombinationTest(int multiplier)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(25)]
    public void Matrix_5x5_CombinationTest(int multiplier)
#elif NUNIT
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(18)]
    [TestCase(19)]
    [TestCase(20)]
    [TestCase(21)]
    [TestCase(22)]
    [TestCase(23)]
    [TestCase(24)]
    [TestCase(25)]
    public void Matrix_5x5_CombinationTest(int multiplier)
#elif MSTEST
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(10)]
    [DataRow(11)]
    [DataRow(12)]
    [DataRow(13)]
    [DataRow(14)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(18)]
    [DataRow(19)]
    [DataRow(20)]
    [DataRow(21)]
    [DataRow(22)]
    [DataRow(23)]
    [DataRow(24)]
    [DataRow(25)]
    public void Matrix_5x5_CombinationTest(int multiplier)
#endif
    {
        // Simulate matrix operations across 5 different input ranges
        var results = new List<int>();
        for (var i = 1; i <= 5; i++)
        {
            results.Add(ComputeValue(multiplier, i));
        }

        var sum = results.Sum();
        var product = results.Aggregate(1, (acc, val) => acc * val);

#if TUNIT
        await Assert.That(results).HasCount(5);
        await Assert.That(sum).IsGreaterThan(0);
        await Assert.That(product).IsGreaterThan(0);
#elif XUNIT || XUNIT3
        Assert.Equal(5, results.Count);
        Assert.True(sum > 0);
        Assert.True(product > 0);
#elif NUNIT
        Assert.That(results.Count, Is.EqualTo(5));
        Assert.That(sum, Is.GreaterThan(0));
        Assert.That(product, Is.GreaterThan(0));
#elif MSTEST
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(sum > 0);
        Assert.IsTrue(product > 0);
#endif
    }

    // Multiple parameters creating complex combinations
#if TUNIT
    [Test]
    [Arguments(1, "small", true)]
    [Arguments(1, "small", false)]
    [Arguments(1, "medium", true)]
    [Arguments(1, "medium", false)]
    [Arguments(1, "large", true)]
    [Arguments(1, "large", false)]
    [Arguments(2, "small", true)]
    [Arguments(2, "small", false)]
    [Arguments(2, "medium", true)]
    [Arguments(2, "medium", false)]
    [Arguments(2, "large", true)]
    [Arguments(2, "large", false)]
    [Arguments(3, "small", true)]
    [Arguments(3, "small", false)]
    [Arguments(3, "medium", true)]
    [Arguments(3, "medium", false)]
    [Arguments(3, "large", true)]
    [Arguments(3, "large", false)]
    [Arguments(4, "small", true)]
    [Arguments(4, "small", false)]
    [Arguments(4, "medium", true)]
    [Arguments(4, "medium", false)]
    [Arguments(4, "large", true)]
    [Arguments(4, "large", false)]
    [Arguments(5, "small", true)]
    [Arguments(5, "small", false)]
    [Arguments(5, "medium", true)]
    [Arguments(5, "medium", false)]
    [Arguments(5, "large", true)]
    [Arguments(5, "large", false)]
    public async Task Matrix_MultiParam_CombinationTest(int size, string category, bool enabled)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(1, "small", true)]
    [InlineData(1, "small", false)]
    [InlineData(1, "medium", true)]
    [InlineData(1, "medium", false)]
    [InlineData(1, "large", true)]
    [InlineData(1, "large", false)]
    [InlineData(2, "small", true)]
    [InlineData(2, "small", false)]
    [InlineData(2, "medium", true)]
    [InlineData(2, "medium", false)]
    [InlineData(2, "large", true)]
    [InlineData(2, "large", false)]
    [InlineData(3, "small", true)]
    [InlineData(3, "small", false)]
    [InlineData(3, "medium", true)]
    [InlineData(3, "medium", false)]
    [InlineData(3, "large", true)]
    [InlineData(3, "large", false)]
    [InlineData(4, "small", true)]
    [InlineData(4, "small", false)]
    [InlineData(4, "medium", true)]
    [InlineData(4, "medium", false)]
    [InlineData(4, "large", true)]
    [InlineData(4, "large", false)]
    [InlineData(5, "small", true)]
    [InlineData(5, "small", false)]
    [InlineData(5, "medium", true)]
    [InlineData(5, "medium", false)]
    [InlineData(5, "large", true)]
    [InlineData(5, "large", false)]
    public void Matrix_MultiParam_CombinationTest(int size, string category, bool enabled)
#elif NUNIT
    [TestCase(1, "small", true)]
    [TestCase(1, "small", false)]
    [TestCase(1, "medium", true)]
    [TestCase(1, "medium", false)]
    [TestCase(1, "large", true)]
    [TestCase(1, "large", false)]
    [TestCase(2, "small", true)]
    [TestCase(2, "small", false)]
    [TestCase(2, "medium", true)]
    [TestCase(2, "medium", false)]
    [TestCase(2, "large", true)]
    [TestCase(2, "large", false)]
    [TestCase(3, "small", true)]
    [TestCase(3, "small", false)]
    [TestCase(3, "medium", true)]
    [TestCase(3, "medium", false)]
    [TestCase(3, "large", true)]
    [TestCase(3, "large", false)]
    [TestCase(4, "small", true)]
    [TestCase(4, "small", false)]
    [TestCase(4, "medium", true)]
    [TestCase(4, "medium", false)]
    [TestCase(4, "large", true)]
    [TestCase(4, "large", false)]
    [TestCase(5, "small", true)]
    [TestCase(5, "small", false)]
    [TestCase(5, "medium", true)]
    [TestCase(5, "medium", false)]
    [TestCase(5, "large", true)]
    [TestCase(5, "large", false)]
    public void Matrix_MultiParam_CombinationTest(int size, string category, bool enabled)
#elif MSTEST
    [TestMethod]
    [DataRow(1, "small", true)]
    [DataRow(1, "small", false)]
    [DataRow(1, "medium", true)]
    [DataRow(1, "medium", false)]
    [DataRow(1, "large", true)]
    [DataRow(1, "large", false)]
    [DataRow(2, "small", true)]
    [DataRow(2, "small", false)]
    [DataRow(2, "medium", true)]
    [DataRow(2, "medium", false)]
    [DataRow(2, "large", true)]
    [DataRow(2, "large", false)]
    [DataRow(3, "small", true)]
    [DataRow(3, "small", false)]
    [DataRow(3, "medium", true)]
    [DataRow(3, "medium", false)]
    [DataRow(3, "large", true)]
    [DataRow(3, "large", false)]
    [DataRow(4, "small", true)]
    [DataRow(4, "small", false)]
    [DataRow(4, "medium", true)]
    [DataRow(4, "medium", false)]
    [DataRow(4, "large", true)]
    [DataRow(4, "large", false)]
    [DataRow(5, "small", true)]
    [DataRow(5, "small", false)]
    [DataRow(5, "medium", true)]
    [DataRow(5, "medium", false)]
    [DataRow(5, "large", true)]
    [DataRow(5, "large", false)]
    public void Matrix_MultiParam_CombinationTest(int size, string category, bool enabled)
#endif
    {
        // Process combinations of parameters
        var result = ProcessConfiguration(size, category, enabled);
        var multiplier = GetMultiplier(category);
        var expected = enabled ? size * multiplier : 0;

#if TUNIT
        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(category).IsIn("small", "medium", "large");
        await Assert.That(size).IsBetween(1, 5).Inclusive();
#elif XUNIT || XUNIT3
        Assert.Equal(expected, result);
        Assert.Contains(category, new[] { "small", "medium", "large" });
        Assert.InRange(size, 1, 5);
#elif NUNIT
        Assert.That(result, Is.EqualTo(expected));
        Assert.That(category, Is.AnyOf("small", "medium", "large"));
        Assert.That(size, Is.InRange(1, 5));
#elif MSTEST
        Assert.AreEqual(expected, result);
        Assert.IsTrue(new[] { "small", "medium", "large" }.Contains(category));
        Assert.IsTrue(size >= 1 && size <= 5);
#endif
    }

    // Async data combinations
#if TUNIT
    [Test]
    [Arguments(10, 20)]
    [Arguments(10, 30)]
    [Arguments(10, 40)]
    [Arguments(10, 50)]
    [Arguments(20, 20)]
    [Arguments(20, 30)]
    [Arguments(20, 40)]
    [Arguments(20, 50)]
    [Arguments(30, 20)]
    [Arguments(30, 30)]
    [Arguments(30, 40)]
    [Arguments(30, 50)]
    [Arguments(40, 20)]
    [Arguments(40, 30)]
    [Arguments(40, 40)]
    [Arguments(40, 50)]
    [Arguments(50, 20)]
    [Arguments(50, 30)]
    [Arguments(50, 40)]
    [Arguments(50, 50)]
    public async Task Matrix_AsyncCombinationTest(int delayMs, int computeValue)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(10, 20)]
    [InlineData(10, 30)]
    [InlineData(10, 40)]
    [InlineData(10, 50)]
    [InlineData(20, 20)]
    [InlineData(20, 30)]
    [InlineData(20, 40)]
    [InlineData(20, 50)]
    [InlineData(30, 20)]
    [InlineData(30, 30)]
    [InlineData(30, 40)]
    [InlineData(30, 50)]
    [InlineData(40, 20)]
    [InlineData(40, 30)]
    [InlineData(40, 40)]
    [InlineData(40, 50)]
    [InlineData(50, 20)]
    [InlineData(50, 30)]
    [InlineData(50, 40)]
    [InlineData(50, 50)]
    public async Task Matrix_AsyncCombinationTest(int delayMs, int computeValue)
#elif NUNIT
    [TestCase(10, 20)]
    [TestCase(10, 30)]
    [TestCase(10, 40)]
    [TestCase(10, 50)]
    [TestCase(20, 20)]
    [TestCase(20, 30)]
    [TestCase(20, 40)]
    [TestCase(20, 50)]
    [TestCase(30, 20)]
    [TestCase(30, 30)]
    [TestCase(30, 40)]
    [TestCase(30, 50)]
    [TestCase(40, 20)]
    [TestCase(40, 30)]
    [TestCase(40, 40)]
    [TestCase(40, 50)]
    [TestCase(50, 20)]
    [TestCase(50, 30)]
    [TestCase(50, 40)]
    [TestCase(50, 50)]
    public async Task Matrix_AsyncCombinationTest(int delayMs, int computeValue)
#elif MSTEST
    [TestMethod]
    [DataRow(10, 20)]
    [DataRow(10, 30)]
    [DataRow(10, 40)]
    [DataRow(10, 50)]
    [DataRow(20, 20)]
    [DataRow(20, 30)]
    [DataRow(20, 40)]
    [DataRow(20, 50)]
    [DataRow(30, 20)]
    [DataRow(30, 30)]
    [DataRow(30, 40)]
    [DataRow(30, 50)]
    [DataRow(40, 20)]
    [DataRow(40, 30)]
    [DataRow(40, 40)]
    [DataRow(40, 50)]
    [DataRow(50, 20)]
    [DataRow(50, 30)]
    [DataRow(50, 40)]
    [DataRow(50, 50)]
    public async Task Matrix_AsyncCombinationTest(int delayMs, int computeValue)
#endif
    {
        // Simulate async operations with different timings
        await Task.Delay(50);
        var result = await ComputeAsync(computeValue);
        var adjusted = result + delayMs;

#if TUNIT
        await Assert.That(result).IsEqualTo(computeValue * 2);
        await Assert.That(adjusted).IsGreaterThan(delayMs);
#elif XUNIT || XUNIT3
        Assert.Equal(computeValue * 2, result);
        Assert.True(adjusted > delayMs);
#elif NUNIT
        Assert.That(result, Is.EqualTo(computeValue * 2));
        Assert.That(adjusted, Is.GreaterThan(delayMs));
#elif MSTEST
        Assert.AreEqual(computeValue * 2, result);
        Assert.IsTrue(adjusted > delayMs);
#endif
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
