using System.Text.Json;
using System.Threading.Tasks;

namespace UnifiedTests;

/// <summary>
/// Tests measuring the overhead of per-test isolated fixture creation.
/// Each test gets its own fresh fixture instance, simulating scenarios
/// where tests require isolated state and cannot share resources.
/// </summary>
#if MSTEST
[TestClass]
public class PerTestFixtureTests : IDisposable
#elif NUNIT
[TestFixture]
public class PerTestFixtureTests : IDisposable
#elif XUNIT || XUNIT3
public class PerTestFixtureTests : IDisposable
#else
public class PerTestFixtureTests : IDisposable
#endif
{
    private Dictionary<string, int> _testCache;
    private List<string> _processedItems;
    private HashSet<int> _usedIds;
    private string _testId;

    public PerTestFixtureTests()
    {
        // Per-test initialization - each test gets a fresh fixture
        // Simulates scenarios requiring isolation (e.g., database transactions, file handles)

        _testId = Guid.NewGuid().ToString()[..8];
        _testCache = [];
        _processedItems = [];
        _usedIds = [];

        // Initialize test-specific data
        for (var i = 0; i < 30; i++)
        {
            _testCache[$"item_{i}"] = i * 10;
            _usedIds.Add(i);
        }

        // Process some initial data
        for (var i = 0; i < 20; i++)
        {
            var item = $"TestData_{_testId}_{i}_{DateTime.UtcNow.Ticks % 1000}";
            _processedItems.Add(item);
        }
    }

    public void Dispose()
    {
        // Cleanup per-test resources
        _testCache?.Clear();
        _processedItems?.Clear();
        _usedIds?.Clear();
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test1()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test1()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test1()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test1()
#endif
    {
        var value = _testCache["item_5"];

#if TUNIT
        await Assert.That(value).IsEqualTo(50);
        await Assert.That(_testCache).HasCount(30);
#elif XUNIT || XUNIT3
        Assert.Equal(50, value);
        Assert.Equal(30, _testCache.Count);
#elif NUNIT
        Assert.That(value, Is.EqualTo(50));
        Assert.That(_testCache.Count, Is.EqualTo(30));
#elif MSTEST
        Assert.AreEqual(50, value);
        Assert.AreEqual(30, _testCache.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test2()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test2()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test2()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test2()
#endif
    {
        var count = _processedItems.Count;

#if TUNIT
        await Assert.That(count).IsEqualTo(20);
        await Assert.That(_processedItems[0]).Contains(_testId);
#elif XUNIT || XUNIT3
        Assert.Equal(20, count);
        Assert.Contains(_testId, _processedItems[0]);
#elif NUNIT
        Assert.That(count, Is.EqualTo(20));
        Assert.That(_processedItems[0], Does.Contain(_testId));
#elif MSTEST
        Assert.AreEqual(20, count);
        Assert.IsTrue(_processedItems[0].Contains(_testId));
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test3()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test3()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test3()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test3()
#endif
    {
        var hasId = _usedIds.Contains(15);

#if TUNIT
        await Assert.That(hasId).IsTrue();
        await Assert.That(_usedIds).HasCount(30);
#elif XUNIT || XUNIT3
        Assert.True(hasId);
        Assert.Equal(30, _usedIds.Count);
#elif NUNIT
        Assert.That(hasId, Is.True);
        Assert.That(_usedIds.Count, Is.EqualTo(30));
#elif MSTEST
        Assert.IsTrue(hasId);
        Assert.AreEqual(30, _usedIds.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test4()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test4()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test4()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test4()
#endif
    {
        var testIdLength = _testId.Length;

#if TUNIT
        await Assert.That(testIdLength).IsEqualTo(8);
        await Assert.That(_testId).IsNotEmpty();
#elif XUNIT || XUNIT3
        Assert.Equal(8, testIdLength);
        Assert.NotEmpty(_testId);
#elif NUNIT
        Assert.That(testIdLength, Is.EqualTo(8));
        Assert.That(_testId, Is.Not.Empty);
#elif MSTEST
        Assert.AreEqual(8, testIdLength);
        Assert.IsTrue(_testId.Length > 0);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test5()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test5()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test5()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test5()
#endif
    {
        var sum = _testCache.Values.Sum();

#if TUNIT
        await Assert.That(sum).IsEqualTo(4350);
#elif XUNIT || XUNIT3
        Assert.Equal(4350, sum);
#elif NUNIT
        Assert.That(sum, Is.EqualTo(4350));
#elif MSTEST
        Assert.AreEqual(4350, sum);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test6()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test6()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test6()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test6()
#endif
    {
        var lastItem = _processedItems[^1];

#if TUNIT
        await Assert.That(lastItem).Contains("TestData_");
        await Assert.That(lastItem).Contains(_testId);
#elif XUNIT || XUNIT3
        Assert.Contains("TestData_", lastItem);
        Assert.Contains(_testId, lastItem);
#elif NUNIT
        Assert.That(lastItem, Does.Contain("TestData_"));
        Assert.That(lastItem, Does.Contain(_testId));
#elif MSTEST
        Assert.IsTrue(lastItem.Contains("TestData_"));
        Assert.IsTrue(lastItem.Contains(_testId));
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test7()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test7()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test7()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test7()
#endif
    {
        var maxId = _usedIds.Max();

#if TUNIT
        await Assert.That(maxId).IsEqualTo(29);
#elif XUNIT || XUNIT3
        Assert.Equal(29, maxId);
#elif NUNIT
        Assert.That(maxId, Is.EqualTo(29));
#elif MSTEST
        Assert.AreEqual(29, maxId);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test8()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test8()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test8()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test8()
#endif
    {
        var hasKey = _testCache.ContainsKey("item_20");

#if TUNIT
        await Assert.That(hasKey).IsTrue();
        await Assert.That(_testCache["item_20"]).IsEqualTo(200);
#elif XUNIT || XUNIT3
        Assert.True(hasKey);
        Assert.Equal(200, _testCache["item_20"]);
#elif NUNIT
        Assert.That(hasKey, Is.True);
        Assert.That(_testCache["item_20"], Is.EqualTo(200));
#elif MSTEST
        Assert.IsTrue(hasKey);
        Assert.AreEqual(200, _testCache["item_20"]);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test9()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test9()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test9()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test9()
#endif
    {
        var allContainTestId = _processedItems.All(item => item.Contains(_testId));

#if TUNIT
        await Assert.That(allContainTestId).IsTrue();
#elif XUNIT || XUNIT3
        Assert.True(allContainTestId);
#elif NUNIT
        Assert.That(allContainTestId, Is.True);
#elif MSTEST
        Assert.IsTrue(allContainTestId);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test10()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test10()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test10()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test10()
#endif
    {
        var minId = _usedIds.Min();

#if TUNIT
        await Assert.That(minId).IsEqualTo(0);
#elif XUNIT || XUNIT3
        Assert.Equal(0, minId);
#elif NUNIT
        Assert.That(minId, Is.EqualTo(0));
#elif MSTEST
        Assert.AreEqual(0, minId);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test11()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test11()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test11()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test11()
#endif
    {
        var firstValue = _testCache["item_0"];

#if TUNIT
        await Assert.That(firstValue).IsEqualTo(0);
#elif XUNIT || XUNIT3
        Assert.Equal(0, firstValue);
#elif NUNIT
        Assert.That(firstValue, Is.EqualTo(0));
#elif MSTEST
        Assert.AreEqual(0, firstValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test12()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test12()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test12()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test12()
#endif
    {
        var avg = _testCache.Values.Average();

#if TUNIT
        await Assert.That(avg).IsEqualTo(145.0);
#elif XUNIT || XUNIT3
        Assert.Equal(145.0, avg);
#elif NUNIT
        Assert.That(avg, Is.EqualTo(145.0));
#elif MSTEST
        Assert.AreEqual(145.0, avg);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test13()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test13()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test13()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test13()
#endif
    {
        var firstItem = _processedItems[0];

#if TUNIT
        await Assert.That(firstItem).StartsWith("TestData_");
#elif XUNIT || XUNIT3
        Assert.StartsWith("TestData_", firstItem);
#elif NUNIT
        Assert.That(firstItem, Does.StartWith("TestData_"));
#elif MSTEST
        Assert.IsTrue(firstItem.StartsWith("TestData_"));
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test14()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test14()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test14()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test14()
#endif
    {
        var lastValue = _testCache["item_29"];

#if TUNIT
        await Assert.That(lastValue).IsEqualTo(290);
#elif XUNIT || XUNIT3
        Assert.Equal(290, lastValue);
#elif NUNIT
        Assert.That(lastValue, Is.EqualTo(290));
#elif MSTEST
        Assert.AreEqual(290, lastValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task PerTestFixture_Test15()
#elif XUNIT || XUNIT3
    [Fact]
    public void PerTestFixture_Test15()
#elif NUNIT
    [Test]
    public void PerTestFixture_Test15()
#elif MSTEST
    [TestMethod]
    public void PerTestFixture_Test15()
#endif
    {
        var keys = _testCache.Keys.ToList();

#if TUNIT
        await Assert.That(keys).HasCount(30);
        await Assert.That(keys.All(k => k.StartsWith("item_"))).IsTrue();
#elif XUNIT || XUNIT3
        Assert.Equal(30, keys.Count);
        Assert.True(keys.All(k => k.StartsWith("item_")));
#elif NUNIT
        Assert.That(keys.Count, Is.EqualTo(30));
        Assert.That(keys.All(k => k.StartsWith("item_")), Is.True);
#elif MSTEST
        Assert.AreEqual(30, keys.Count);
        Assert.IsTrue(keys.All(k => k.StartsWith("item_")));
#endif
    }
}
