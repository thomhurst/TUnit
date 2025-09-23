using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class RepeatTests
{
    private static int _counter = 0;

    // Note: xUnit doesn't have built-in repeat functionality
    // MSTest doesn't have built-in repeat, but we can simulate with DataRow
    // NUnit has Repeat attribute
    // TUnit has Repeat attribute

#if TUNIT
    [Test]
    [Repeat(100)]
    public async Task RepeatedCalculationTest()
#elif NUNIT
    [Test]
    [Repeat(100)]
    public void RepeatedCalculationTest()
#elif XUNIT
    // xUnit doesn't have Repeat, so we use Theory with range data
    [Theory]
    [MemberData(nameof(RepeatData), 100)]
    public void RepeatedCalculationTest(int _)
#elif MSTEST
    // MSTest doesn't have Repeat, simulate with multiple DataRows (limited example)
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void RepeatedCalculationTest(int _)
#endif
    {
        var localCounter = Interlocked.Increment(ref _counter);
        var result = PerformCalculation(localCounter);

#if TUNIT
        await Assert.That(result).IsGreaterThan(0);
        await Assert.That(result).IsEqualTo(localCounter * (localCounter + 1) / 2);
#elif XUNIT
        Assert.True(result > 0);
        Assert.Equal(localCounter * (localCounter + 1) / 2, result);
#elif NUNIT
        Assert.That(result, Is.GreaterThan(0));
        Assert.That(result, Is.EqualTo(localCounter * (localCounter + 1) / 2));
#elif MSTEST
        Assert.IsTrue(result > 0);
        Assert.AreEqual(localCounter * (localCounter + 1) / 2, result);
#endif
    }

#if TUNIT
    [Test]
    [Repeat(50)]
    public async Task RepeatedAsyncTest()
#elif NUNIT
    [Test]
    [Repeat(50)]
    public async Task RepeatedAsyncTest()
#elif XUNIT
    [Theory]
    [MemberData(nameof(RepeatData), 50)]
    public async Task RepeatedAsyncTest(int _)
#elif MSTEST
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task RepeatedAsyncTest(int _)
#endif
    {
        var taskId = Guid.NewGuid();
        var result = await ProcessDataAsync(taskId);

#if TUNIT
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Length).IsEqualTo(36); // GUID length
        await Assert.That(result).IsEqualTo(taskId.ToString());
#elif XUNIT
        Assert.NotNull(result);
        Assert.Equal(36, result.Length);
        Assert.Equal(taskId.ToString(), result);
#elif NUNIT
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(36));
        Assert.That(result, Is.EqualTo(taskId.ToString()));
#elif MSTEST
        Assert.IsNotNull(result);
        Assert.AreEqual(36, result.Length);
        Assert.AreEqual(taskId.ToString(), result);
#endif
    }

#if TUNIT
    [Test]
    [Repeat(25)]
    public async Task RepeatedStringOperationTest()
#elif NUNIT
    [Test]
    [Repeat(25)]
    public void RepeatedStringOperationTest()
#elif XUNIT
    [Theory]
    [MemberData(nameof(RepeatData), 25)]
    public void RepeatedStringOperationTest(int _)
#elif MSTEST
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void RepeatedStringOperationTest(int _)
#endif
    {
        var iteration = Interlocked.Increment(ref _counter);
        var text = $"Iteration_{iteration}";
        var processed = ProcessString(text);

#if TUNIT
        await Assert.That(processed).Contains("PROCESSED");
        await Assert.That(processed).Contains(iteration.ToString());
        await Assert.That(processed.Length).IsGreaterThan(text.Length);
#elif XUNIT
        Assert.Contains("PROCESSED", processed);
        Assert.Contains(iteration.ToString(), processed);
        Assert.True(processed.Length > text.Length);
#elif NUNIT
        Assert.That(processed, Does.Contain("PROCESSED"));
        Assert.That(processed, Does.Contain(iteration.ToString()));
        Assert.That(processed.Length, Is.GreaterThan(text.Length));
#elif MSTEST
        Assert.IsTrue(processed.Contains("PROCESSED"));
        Assert.IsTrue(processed.Contains(iteration.ToString()));
        Assert.IsTrue(processed.Length > text.Length);
#endif
    }

    private int PerformCalculation(int input)
    {
        var result = 0;
        for (var i = 1; i <= input; i++)
        {
            result += i;
        }
        return result;
    }

    private async Task<string> ProcessDataAsync(Guid id)
    {
        await Task.Yield();
        return id.ToString();
    }

    private string ProcessString(string input)
    {
        return $"PROCESSED_{input.ToUpper()}_{input.Length}";
    }

#if XUNIT
    public static IEnumerable<object[]> RepeatData(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new object[] { i };
        }
    }
#endif
}