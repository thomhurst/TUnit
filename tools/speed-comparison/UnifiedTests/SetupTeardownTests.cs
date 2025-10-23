using System.Threading.Tasks;

namespace UnifiedTests;

/// <summary>
/// Tests measuring the overhead of setup and teardown lifecycle hooks.
/// Each test goes through setup before execution and teardown after,
/// allowing measurement of per-test lifecycle hook overhead.
/// </summary>
#if MSTEST
[TestClass]
public class SetupTeardownTests : IDisposable
#elif NUNIT
[TestFixture]
public class SetupTeardownTests : IDisposable
#elif XUNIT || XUNIT3
public class SetupTeardownTests : IDisposable
#else
public class SetupTeardownTests : IDisposable
#endif
{
    private Dictionary<string, string> _testState;
    private List<int> _workingData;
    private HashSet<string> _processedKeys;
    private int _setupCounter;

    public SetupTeardownTests()
    {
        // Lightweight constructor - just initialize empty collections
        _testState = [];
        _workingData = [];
        _processedKeys = [];
        _setupCounter = 0;
    }

#if !XUNIT && !XUNIT3
#if TUNIT
    [Before(Test)]
#elif MSTEST
    [TestInitialize]
#elif NUNIT
    [SetUp]
#endif
    public void Setup()
    {
        // Reset and prepare state for each test
        _testState.Clear();
        _workingData.Clear();
        _processedKeys.Clear();

        // Simulate realistic setup work
        for (var i = 0; i < 20; i++)
        {
            _testState[$"key_{i}"] = $"value_{i}_{Guid.NewGuid().ToString()[..8]}";
            _workingData.Add(i * 10);
        }

        _setupCounter++;
    }
#else
    // xUnit doesn't have per-test setup - constructor is called per test
    // This creates a fair comparison of lifecycle overhead
    private void Setup()
    {
        // Reset and prepare state for each test
        _testState.Clear();
        _workingData.Clear();
        _processedKeys.Clear();

        // Simulate realistic setup work
        for (var i = 0; i < 20; i++)
        {
            _testState[$"key_{i}"] = $"value_{i}_{Guid.NewGuid().ToString()[..8]}";
            _workingData.Add(i * 10);
        }

        _setupCounter++;
    }
#endif

#if !XUNIT && !XUNIT3
#if TUNIT
    [After(Test)]
#elif MSTEST
    [TestCleanup]
#elif NUNIT
    [TearDown]
#endif
    public void Teardown()
    {
        // Actual cleanup work
        _testState.Clear();
        _workingData.Clear();
        _processedKeys.Clear();
    }
#endif

    public void Dispose()
    {
#if XUNIT || XUNIT3
        // For xUnit, Dispose is called after each test
        _testState?.Clear();
        _workingData?.Clear();
        _processedKeys?.Clear();
#else
        // For other frameworks, cleanup happens in class disposal
        _testState?.Clear();
        _workingData?.Clear();
        _processedKeys?.Clear();
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test1()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test1()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test1()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test1()
#endif
    {
#if XUNIT || XUNIT3
        // Simulate setup for xUnit to maintain fairness
        Setup();
#endif

        var value = _testState.GetValueOrDefault("key_5", "");

#if TUNIT
        await Assert.That(value).IsNotEmpty();
        await Assert.That(value).StartsWith("value_5");
#elif XUNIT || XUNIT3
        Assert.NotEmpty(value);
        Assert.StartsWith("value_5", value);
#elif NUNIT
        Assert.That(value, Is.Not.Empty);
        Assert.That(value, Does.StartWith("value_5"));
#elif MSTEST
        Assert.IsTrue(value.Length > 0);
        Assert.IsTrue(value.StartsWith("value_5"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test2()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test2()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test2()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test2()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var count = _workingData.Count;

#if TUNIT
        await Assert.That(count).IsEqualTo(20);
        await Assert.That(_workingData[0]).IsEqualTo(0);
#elif XUNIT || XUNIT3
        Assert.Equal(20, count);
        Assert.Equal(0, _workingData[0]);
#elif NUNIT
        Assert.That(count, Is.EqualTo(20));
        Assert.That(_workingData[0], Is.EqualTo(0));
#elif MSTEST
        Assert.AreEqual(20, count);
        Assert.AreEqual(0, _workingData[0]);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test3()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test3()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test3()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test3()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        _processedKeys.Add("test_key_1");
        _processedKeys.Add("test_key_2");

#if TUNIT
        await Assert.That(_processedKeys).HasCount(2);
        await Assert.That(_processedKeys.Contains("test_key_1")).IsTrue();
#elif XUNIT || XUNIT3
        Assert.Equal(2, _processedKeys.Count);
        Assert.True(_processedKeys.Contains("test_key_1"));
#elif NUNIT
        Assert.That(_processedKeys.Count, Is.EqualTo(2));
        Assert.That(_processedKeys.Contains("test_key_1"), Is.True);
#elif MSTEST
        Assert.AreEqual(2, _processedKeys.Count);
        Assert.IsTrue(_processedKeys.Contains("test_key_1"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test4()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test4()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test4()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test4()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var lastValue = _workingData[^1];

#if TUNIT
        await Assert.That(lastValue).IsEqualTo(190);
#elif XUNIT || XUNIT3
        Assert.Equal(190, lastValue);
#elif NUNIT
        Assert.That(lastValue, Is.EqualTo(190));
#elif MSTEST
        Assert.AreEqual(190, lastValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test5()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test5()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test5()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test5()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var hasKey = _testState.ContainsKey("key_10");

#if TUNIT
        await Assert.That(hasKey).IsTrue();
        await Assert.That(_testState).HasCount(20);
#elif XUNIT || XUNIT3
        Assert.True(hasKey);
        Assert.Equal(20, _testState.Count);
#elif NUNIT
        Assert.That(hasKey, Is.True);
        Assert.That(_testState.Count, Is.EqualTo(20));
#elif MSTEST
        Assert.IsTrue(hasKey);
        Assert.AreEqual(20, _testState.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test6()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test6()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test6()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test6()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var sum = _workingData.Sum();

#if TUNIT
        await Assert.That(sum).IsEqualTo(1900);
#elif XUNIT || XUNIT3
        Assert.Equal(1900, sum);
#elif NUNIT
        Assert.That(sum, Is.EqualTo(1900));
#elif MSTEST
        Assert.AreEqual(1900, sum);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test7()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test7()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test7()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test7()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var keys = _testState.Keys.ToList();

#if TUNIT
        await Assert.That(keys).HasCount(20);
        await Assert.That(keys[0]).StartsWith("key_");
#elif XUNIT || XUNIT3
        Assert.Equal(20, keys.Count);
        Assert.StartsWith("key_", keys[0]);
#elif NUNIT
        Assert.That(keys.Count, Is.EqualTo(20));
        Assert.That(keys[0], Does.StartWith("key_"));
#elif MSTEST
        Assert.AreEqual(20, keys.Count);
        Assert.IsTrue(keys[0].StartsWith("key_"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test8()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test8()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test8()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test8()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        _processedKeys.Add("processed_1");
        var hasProcessed = _processedKeys.Contains("processed_1");

#if TUNIT
        await Assert.That(hasProcessed).IsTrue();
#elif XUNIT || XUNIT3
        Assert.True(hasProcessed);
#elif NUNIT
        Assert.That(hasProcessed, Is.True);
#elif MSTEST
        Assert.IsTrue(hasProcessed);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test9()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test9()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test9()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test9()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var midValue = _workingData[10];

#if TUNIT
        await Assert.That(midValue).IsEqualTo(100);
#elif XUNIT || XUNIT3
        Assert.Equal(100, midValue);
#elif NUNIT
        Assert.That(midValue, Is.EqualTo(100));
#elif MSTEST
        Assert.AreEqual(100, midValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test10()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test10()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test10()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test10()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var allValues = _testState.Values.ToList();

#if TUNIT
        await Assert.That(allValues).HasCount(20);
        await Assert.That(allValues.All(v => v.StartsWith("value_"))).IsTrue();
#elif XUNIT || XUNIT3
        Assert.Equal(20, allValues.Count);
        Assert.True(allValues.All(v => v.StartsWith("value_")));
#elif NUNIT
        Assert.That(allValues.Count, Is.EqualTo(20));
        Assert.That(allValues.All(v => v.StartsWith("value_")), Is.True);
#elif MSTEST
        Assert.AreEqual(20, allValues.Count);
        Assert.IsTrue(allValues.All(v => v.StartsWith("value_")));
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test11()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test11()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test11()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test11()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var avg = _workingData.Average();

#if TUNIT
        await Assert.That(avg).IsEqualTo(95.0);
#elif XUNIT || XUNIT3
        Assert.Equal(95.0, avg);
#elif NUNIT
        Assert.That(avg, Is.EqualTo(95.0));
#elif MSTEST
        Assert.AreEqual(95.0, avg);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test12()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test12()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test12()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test12()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var firstKey = "key_0";
        var value = _testState[firstKey];

#if TUNIT
        await Assert.That(value).StartsWith("value_0");
#elif XUNIT || XUNIT3
        Assert.StartsWith("value_0", value);
#elif NUNIT
        Assert.That(value, Does.StartWith("value_0"));
#elif MSTEST
        Assert.IsTrue(value.StartsWith("value_0"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test13()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test13()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test13()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test13()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var max = _workingData.Max();

#if TUNIT
        await Assert.That(max).IsEqualTo(190);
#elif XUNIT || XUNIT3
        Assert.Equal(190, max);
#elif NUNIT
        Assert.That(max, Is.EqualTo(190));
#elif MSTEST
        Assert.AreEqual(190, max);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test14()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test14()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test14()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test14()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test14()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        _processedKeys.Add("temp_1");
        _processedKeys.Add("temp_2");
        _processedKeys.Add("temp_3");

#if TUNIT
        await Assert.That(_processedKeys).HasCount(3);
#elif XUNIT || XUNIT3
        Assert.Equal(3, _processedKeys.Count);
#elif NUNIT
        Assert.That(_processedKeys.Count, Is.EqualTo(3));
#elif MSTEST
        Assert.AreEqual(3, _processedKeys.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task SetupTeardown_Test15()
#elif XUNIT || XUNIT3
    [Fact]
    public void SetupTeardown_Test15()
#elif NUNIT
    [Test]
    public void SetupTeardown_Test15()
#elif MSTEST
    [TestMethod]
    public void SetupTeardown_Test15()
#endif
    {
#if XUNIT || XUNIT3
        Setup();
#endif

        var lastKey = "key_19";
        var hasLastKey = _testState.ContainsKey(lastKey);

#if TUNIT
        await Assert.That(hasLastKey).IsTrue();
#elif XUNIT || XUNIT3
        Assert.True(hasLastKey);
#elif NUNIT
        Assert.That(hasLastKey, Is.True);
#elif MSTEST
        Assert.IsTrue(hasLastKey);
#endif
    }
}
