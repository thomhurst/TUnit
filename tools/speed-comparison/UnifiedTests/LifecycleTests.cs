using System.Threading.Tasks;

namespace UnifiedTests;

public interface IExpensiveResource
{
    void Initialize();
    string GetData(int index);
    void Cleanup();
    int ResourceId { get; }
}

public class ExpensiveResource : IExpensiveResource
{
    private readonly Dictionary<int, string> _data = [];
    private static int _instanceCounter = 0;

    public int ResourceId { get; }

    public ExpensiveResource()
    {
        ResourceId = Interlocked.Increment(ref _instanceCounter);
    }

    public void Initialize()
    {
        // Simulate expensive initialization
        for (var i = 0; i < 100; i++)
        {
            _data[i] = $"Resource_{ResourceId}_Data_{i}_{Guid.NewGuid()}";
        }
    }

    public string GetData(int index)
    {
        return _data.TryGetValue(index, out var value) ? value : "";
    }

    public void Cleanup()
    {
        _data.Clear();
    }
}

#if MSTEST
[TestClass]
public class LifecycleTests : IDisposable
#elif NUNIT
[TestFixture]
public class LifecycleTests : IDisposable
#elif XUNIT || XUNIT3
public class LifecycleTests : IDisposable
#else
public class LifecycleTests : IDisposable
#endif
{
    private IExpensiveResource _resource;
    private List<string> _testLogs;
    private Dictionary<string, int> _testCounters;

    public LifecycleTests()
    {
        // Constructor - expensive initialization
        _resource = new ExpensiveResource();
        _resource.Initialize();
        _testLogs = [];
        _testCounters = new Dictionary<string, int>();
    }

#if TUNIT
    [Before(Test)]
    public void SetupTest()
#elif MSTEST
    [TestInitialize]
    public void SetupTest()
#elif NUNIT
    [SetUp]
    public void SetupTest()
#elif XUNIT || XUNIT3
    private void SetupTest()
#endif
    {
        _testLogs.Clear();
        _testLogs.Add($"Setup_{DateTime.UtcNow.Ticks}");

        // Initialize test-specific counters
        if (!_testCounters.ContainsKey("setup_count"))
        {
            _testCounters["setup_count"] = 0;
        }
        _testCounters["setup_count"]++;
    }

#if TUNIT
    [After(Test)]
    public void CleanupTest()
#elif MSTEST
    [TestCleanup]
    public void CleanupTest()
#elif NUNIT
    [TearDown]
    public void CleanupTest()
#elif XUNIT || XUNIT3
    private void CleanupTest()
#endif
    {
        _testLogs.Add($"Cleanup_{DateTime.UtcNow.Ticks}");
        if (!_testCounters.ContainsKey("cleanup_count"))
        {
            _testCounters["cleanup_count"] = 0;
        }
        _testCounters["cleanup_count"]++;
    }

#if TUNIT
    [Test]
    public async Task Lifecycle_Test1()
#elif XUNIT || XUNIT3
    [Fact]
    public void Lifecycle_Test1()
#elif NUNIT
    [Test]
    public void Lifecycle_Test1()
#elif MSTEST
    [TestMethod]
    public void Lifecycle_Test1()
#endif
    {
#if XUNIT || XUNIT3
        SetupTest();
#endif

        var data = _resource.GetData(1);

#if TUNIT
        await Assert.That(data).IsNotEmpty();
        await Assert.That(data).Contains(_resource.ResourceId.ToString());
        await Assert.That(_testLogs).HasCount().GreaterThanOrEqualTo(1);
#elif XUNIT || XUNIT3
        Assert.NotEmpty(data);
        Assert.Contains(_resource.ResourceId.ToString(), data);
        Assert.True(_testLogs.Count >= 1);
        CleanupTest();
#elif NUNIT
        Assert.That(data, Is.Not.Empty);
        Assert.That(data, Does.Contain(_resource.ResourceId.ToString()));
        Assert.That(_testLogs.Count, Is.GreaterThanOrEqualTo(1));
#elif MSTEST
        Assert.IsTrue(!string.IsNullOrEmpty(data));
        Assert.IsTrue(data.Contains(_resource.ResourceId.ToString()));
        Assert.IsTrue(_testLogs.Count >= 1);
#endif
    }

#if TUNIT
    [Test]
    public async Task Lifecycle_Test2()
#elif XUNIT || XUNIT3
    [Fact]
    public void Lifecycle_Test2()
#elif NUNIT
    [Test]
    public void Lifecycle_Test2()
#elif MSTEST
    [TestMethod]
    public void Lifecycle_Test2()
#endif
    {
#if XUNIT || XUNIT3
        SetupTest();
#endif

        var data = _resource.GetData(2);

#if TUNIT
        await Assert.That(data).IsNotEmpty();
        await Assert.That(_testLogs).HasCount().GreaterThanOrEqualTo(1);
#elif XUNIT || XUNIT3
        Assert.NotEmpty(data);
        Assert.True(_testLogs.Count >= 1);
        CleanupTest();
#elif NUNIT
        Assert.That(data, Is.Not.Empty);
        Assert.That(_testLogs.Count, Is.GreaterThanOrEqualTo(1));
#elif MSTEST
        Assert.IsTrue(!string.IsNullOrEmpty(data));
        Assert.IsTrue(_testLogs.Count >= 1);
#endif
    }

#if TUNIT
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    public async Task Lifecycle_ParameterizedTest(int index)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void Lifecycle_ParameterizedTest(int index)
#elif NUNIT
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Lifecycle_ParameterizedTest(int index)
#elif MSTEST
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    public void Lifecycle_ParameterizedTest(int index)
#endif
    {
#if XUNIT || XUNIT3
        SetupTest();
#endif

        // Each parameterized test goes through setup/cleanup
        var data = _resource.GetData(index);
        var processed = ProcessWithLifecycle(data, index);

#if TUNIT
        await Assert.That(processed).IsNotEmpty();
        await Assert.That(processed).Contains($"Index_{index}");
        await Assert.That(_testLogs).HasCount().GreaterThanOrEqualTo(1);
#elif XUNIT || XUNIT3
        Assert.NotEmpty(processed);
        Assert.Contains($"Index_{index}", processed);
        Assert.True(_testLogs.Count >= 1);
        CleanupTest();
#elif NUNIT
        Assert.That(processed, Is.Not.Empty);
        Assert.That(processed, Does.Contain($"Index_{index}"));
        Assert.That(_testLogs.Count, Is.GreaterThanOrEqualTo(1));
#elif MSTEST
        Assert.IsTrue(!string.IsNullOrEmpty(processed));
        Assert.IsTrue(processed.Contains($"Index_{index}"));
        Assert.IsTrue(_testLogs.Count >= 1);
#endif
    }

#if TUNIT
    [Test]
    public async Task Lifecycle_AsyncTest()
#elif XUNIT || XUNIT3
    [Fact]
    public async Task Lifecycle_AsyncTest()
#elif NUNIT
    [Test]
    public async Task Lifecycle_AsyncTest()
#elif MSTEST
    [TestMethod]
    public async Task Lifecycle_AsyncTest()
#endif
    {
#if XUNIT || XUNIT3
        SetupTest();
#endif

        // Simulate async initialization
        await Task.Yield();
        var data = await GetResourceDataAsync(5);

#if TUNIT
        await Assert.That(data).IsNotEmpty();
        await Assert.That(data).Contains("Async");
#elif XUNIT || XUNIT3
        Assert.NotEmpty(data);
        Assert.Contains("Async", data);
        CleanupTest();
#elif NUNIT
        Assert.That(data, Is.Not.Empty);
        Assert.That(data, Does.Contain("Async"));
#elif MSTEST
        Assert.IsTrue(!string.IsNullOrEmpty(data));
        Assert.IsTrue(data.Contains("Async"));
#endif
    }

#if TUNIT
    [Test]
    [Arguments(1, "config1")]
    [Arguments(2, "config2")]
    [Arguments(3, "config3")]
    [Arguments(4, "config4")]
    [Arguments(5, "config5")]
    [Arguments(6, "config6")]
    [Arguments(7, "config7")]
    [Arguments(8, "config8")]
    [Arguments(9, "config9")]
    [Arguments(10, "config10")]
    public async Task Lifecycle_ComplexParameterizedTest(int id, string config)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(1, "config1")]
    [InlineData(2, "config2")]
    [InlineData(3, "config3")]
    [InlineData(4, "config4")]
    [InlineData(5, "config5")]
    [InlineData(6, "config6")]
    [InlineData(7, "config7")]
    [InlineData(8, "config8")]
    [InlineData(9, "config9")]
    [InlineData(10, "config10")]
    public void Lifecycle_ComplexParameterizedTest(int id, string config)
#elif NUNIT
    [TestCase(1, "config1")]
    [TestCase(2, "config2")]
    [TestCase(3, "config3")]
    [TestCase(4, "config4")]
    [TestCase(5, "config5")]
    [TestCase(6, "config6")]
    [TestCase(7, "config7")]
    [TestCase(8, "config8")]
    [TestCase(9, "config9")]
    [TestCase(10, "config10")]
    public void Lifecycle_ComplexParameterizedTest(int id, string config)
#elif MSTEST
    [TestMethod]
    [DataRow(1, "config1")]
    [DataRow(2, "config2")]
    [DataRow(3, "config3")]
    [DataRow(4, "config4")]
    [DataRow(5, "config5")]
    [DataRow(6, "config6")]
    [DataRow(7, "config7")]
    [DataRow(8, "config8")]
    [DataRow(9, "config9")]
    [DataRow(10, "config10")]
    public void Lifecycle_ComplexParameterizedTest(int id, string config)
#endif
    {
#if XUNIT || XUNIT3
        SetupTest();
#endif

        // Complex lifecycle with multiple setup/cleanup phases
        var resourceData = _resource.GetData(id % 10);
        var processed = ApplyConfiguration(resourceData, config);
        var validated = ValidateResult(processed, id);

#if TUNIT
        await Assert.That(validated).IsTrue();
        await Assert.That(processed).Contains(config);
        await Assert.That(_testCounters["setup_count"]).IsGreaterThan(0);
#elif XUNIT || XUNIT3
        Assert.True(validated);
        Assert.Contains(config, processed);
        Assert.True(_testCounters["setup_count"] > 0);
        CleanupTest();
#elif NUNIT
        Assert.That(validated, Is.True);
        Assert.That(processed, Does.Contain(config));
        Assert.That(_testCounters["setup_count"], Is.GreaterThan(0));
#elif MSTEST
        Assert.IsTrue(validated);
        Assert.IsTrue(processed.Contains(config));
        Assert.IsTrue(_testCounters["setup_count"] > 0);
#endif
    }

    public void Dispose()
    {
        _resource?.Cleanup();
        _testLogs?.Clear();
        _testCounters?.Clear();
    }

    private string ProcessWithLifecycle(string data, int index)
    {
        _testLogs.Add($"Process_{index}");
        return $"{data}_Index_{index}";
    }

    private async Task<string> GetResourceDataAsync(int index)
    {
        await Task.Yield();
        var data = _resource.GetData(index);
        return $"Async_{data}";
    }

    private string ApplyConfiguration(string data, string config)
    {
        return $"{data}_{config}_{DateTime.UtcNow.Ticks % 1000}";
    }

    private bool ValidateResult(string result, int id)
    {
        return !string.IsNullOrEmpty(result) && result.Length > id;
    }
}
