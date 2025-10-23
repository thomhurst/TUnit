using System.Text.Json;
using System.Threading.Tasks;

namespace UnifiedTests;

/// <summary>
/// Tests measuring the efficiency of shared class-level fixtures.
/// The fixture is initialized once and reused across all tests,
/// allowing measurement of fixture sharing and reuse overhead.
/// </summary>

// Shared fixture class for xUnit
#if XUNIT || XUNIT3
public class SharedTestFixture : IDisposable
{
    public Dictionary<int, string> SharedCache { get; }
    public List<ProcessedRecord> ProcessedRecords { get; }
    public string ConfigurationJson { get; }

    public SharedTestFixture()
    {
        // Expensive one-time initialization
        SharedCache = [];
        ProcessedRecords = [];

        // Build shared cache
        for (var i = 0; i < 100; i++)
        {
            SharedCache[i] = $"CachedValue_{i}_{Guid.NewGuid().ToString()[..8]}";
        }

        // Process records
        for (var i = 0; i < 50; i++)
        {
            ProcessedRecords.Add(new ProcessedRecord
            {
                Id = i,
                Name = $"Record_{i}",
                Value = i * 100,
                Timestamp = DateTime.UtcNow
            });
        }

        // Serialize configuration
        ConfigurationJson = JsonSerializer.Serialize(new
        {
            Version = "1.0",
            Environment = "Test",
            Settings = SharedCache.Take(10).ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
        });
    }

    public void Dispose()
    {
        SharedCache?.Clear();
        ProcessedRecords?.Clear();
    }
}

public class ProcessedRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
}
#endif

#if MSTEST
[TestClass]
public class SharedFixtureTests
#elif NUNIT
[TestFixture]
public class SharedFixtureTests
#elif XUNIT || XUNIT3
public class SharedFixtureTests : IClassFixture<SharedTestFixture>
#else
public class SharedFixtureTests
#endif
{
#if !XUNIT && !XUNIT3
    private static Dictionary<int, string> _sharedCache;
    private static List<ProcessedRecord> _processedRecords;
    private static string _configurationJson;

    public class ProcessedRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
#endif

#if XUNIT || XUNIT3
    private readonly SharedTestFixture _fixture;

    public SharedFixtureTests(SharedTestFixture fixture)
    {
        _fixture = fixture;
    }
#endif

#if !XUNIT && !XUNIT3
#if TUNIT
    [Before(Class)]
    public static void ClassSetup()
#elif MSTEST
    [ClassInitialize]
    public static void ClassSetup(TestContext context)
#elif NUNIT
    [OneTimeSetUp]
    public void ClassSetup()
#endif
    {
        // One-time expensive initialization shared across all tests
        _sharedCache = [];
        _processedRecords = [];

        // Build shared cache
        for (var i = 0; i < 100; i++)
        {
            _sharedCache[i] = $"CachedValue_{i}_{Guid.NewGuid().ToString()[..8]}";
        }

        // Process records
        for (var i = 0; i < 50; i++)
        {
            _processedRecords.Add(new ProcessedRecord
            {
                Id = i,
                Name = $"Record_{i}",
                Value = i * 100,
                Timestamp = DateTime.UtcNow
            });
        }

        // Serialize configuration
        _configurationJson = JsonSerializer.Serialize(new
        {
            Version = "1.0",
            Environment = "Test",
            Settings = _sharedCache.Take(10).ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
        });
    }

#if TUNIT
    [After(Class)]
    public static void ClassCleanup()
#elif MSTEST
    [ClassCleanup]
    public static void ClassCleanup()
#elif NUNIT
    [OneTimeTearDown]
    public void ClassCleanup()
#endif
    {
        _sharedCache?.Clear();
        _processedRecords?.Clear();
    }
#endif

#if TUNIT
    [Test]
    public async Task SharedFixture_Test1()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test1()
#elif NUNIT
    [Test]
    public void SharedFixture_Test1()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test1()
#endif
    {
#if XUNIT || XUNIT3
        var value = _fixture.SharedCache[10];
        Assert.NotEmpty(value);
        Assert.Contains("CachedValue_10", value);
#elif TUNIT
        var value = _sharedCache[10];
        await Assert.That(value).IsNotEmpty();
        await Assert.That(value).Contains("CachedValue_10");
#elif NUNIT
        var value = _sharedCache[10];
        Assert.That(value, Is.Not.Empty);
        Assert.That(value, Does.Contain("CachedValue_10"));
#elif MSTEST
        var value = _sharedCache[10];
        Assert.IsTrue(value.Length > 0);
        Assert.IsTrue(value.Contains("CachedValue_10"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test2()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test2()
#elif NUNIT
    [Test]
    public void SharedFixture_Test2()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test2()
#endif
    {
#if XUNIT || XUNIT3
        var count = _fixture.SharedCache.Count;
        Assert.Equal(100, count);
#elif TUNIT
        var count = _sharedCache.Count;
        await Assert.That(count).IsEqualTo(100);
#elif NUNIT
        var count = _sharedCache.Count;
        Assert.That(count, Is.EqualTo(100));
#elif MSTEST
        var count = _sharedCache.Count;
        Assert.AreEqual(100, count);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test3()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test3()
#elif NUNIT
    [Test]
    public void SharedFixture_Test3()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test3()
#endif
    {
#if XUNIT || XUNIT3
        var record = _fixture.ProcessedRecords[0];
        Assert.Equal(0, record.Id);
        Assert.Equal("Record_0", record.Name);
#elif TUNIT
        var record = _processedRecords[0];
        await Assert.That(record.Id).IsEqualTo(0);
        await Assert.That(record.Name).IsEqualTo("Record_0");
#elif NUNIT
        var record = _processedRecords[0];
        Assert.That(record.Id, Is.EqualTo(0));
        Assert.That(record.Name, Is.EqualTo("Record_0"));
#elif MSTEST
        var record = _processedRecords[0];
        Assert.AreEqual(0, record.Id);
        Assert.AreEqual("Record_0", record.Name);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test4()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test4()
#elif NUNIT
    [Test]
    public void SharedFixture_Test4()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test4()
#endif
    {
#if XUNIT || XUNIT3
        var recordCount = _fixture.ProcessedRecords.Count;
        Assert.Equal(50, recordCount);
#elif TUNIT
        var recordCount = _processedRecords.Count;
        await Assert.That(recordCount).IsEqualTo(50);
#elif NUNIT
        var recordCount = _processedRecords.Count;
        Assert.That(recordCount, Is.EqualTo(50));
#elif MSTEST
        var recordCount = _processedRecords.Count;
        Assert.AreEqual(50, recordCount);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test5()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test5()
#elif NUNIT
    [Test]
    public void SharedFixture_Test5()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test5()
#endif
    {
#if XUNIT || XUNIT3
        var json = _fixture.ConfigurationJson;
        Assert.NotEmpty(json);
        Assert.Contains("Version", json);
#elif TUNIT
        var json = _configurationJson;
        await Assert.That(json).IsNotEmpty();
        await Assert.That(json).Contains("Version");
#elif NUNIT
        var json = _configurationJson;
        Assert.That(json, Is.Not.Empty);
        Assert.That(json, Does.Contain("Version"));
#elif MSTEST
        var json = _configurationJson;
        Assert.IsTrue(json.Length > 0);
        Assert.IsTrue(json.Contains("Version"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test6()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test6()
#elif NUNIT
    [Test]
    public void SharedFixture_Test6()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test6()
#endif
    {
#if XUNIT || XUNIT3
        var lastRecord = _fixture.ProcessedRecords[^1];
        Assert.Equal(49, lastRecord.Id);
#elif TUNIT
        var lastRecord = _processedRecords[^1];
        await Assert.That(lastRecord.Id).IsEqualTo(49);
#elif NUNIT
        var lastRecord = _processedRecords[^1];
        Assert.That(lastRecord.Id, Is.EqualTo(49));
#elif MSTEST
        var lastRecord = _processedRecords[^1];
        Assert.AreEqual(49, lastRecord.Id);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test7()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test7()
#elif NUNIT
    [Test]
    public void SharedFixture_Test7()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test7()
#endif
    {
#if XUNIT || XUNIT3
        var midValue = _fixture.SharedCache[50];
        Assert.Contains("CachedValue_50", midValue);
#elif TUNIT
        var midValue = _sharedCache[50];
        await Assert.That(midValue).Contains("CachedValue_50");
#elif NUNIT
        var midValue = _sharedCache[50];
        Assert.That(midValue, Does.Contain("CachedValue_50"));
#elif MSTEST
        var midValue = _sharedCache[50];
        Assert.IsTrue(midValue.Contains("CachedValue_50"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test8()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test8()
#elif NUNIT
    [Test]
    public void SharedFixture_Test8()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test8()
#endif
    {
#if XUNIT || XUNIT3
        var totalValue = _fixture.ProcessedRecords.Sum(r => r.Value);
        Assert.Equal(122500, totalValue);
#elif TUNIT
        var totalValue = _processedRecords.Sum(r => r.Value);
        await Assert.That(totalValue).IsEqualTo(122500);
#elif NUNIT
        var totalValue = _processedRecords.Sum(r => r.Value);
        Assert.That(totalValue, Is.EqualTo(122500));
#elif MSTEST
        var totalValue = _processedRecords.Sum(r => r.Value);
        Assert.AreEqual(122500, totalValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test9()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test9()
#elif NUNIT
    [Test]
    public void SharedFixture_Test9()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test9()
#endif
    {
#if XUNIT || XUNIT3
        var hasKey = _fixture.SharedCache.ContainsKey(99);
        Assert.True(hasKey);
#elif TUNIT
        var hasKey = _sharedCache.ContainsKey(99);
        await Assert.That(hasKey).IsTrue();
#elif NUNIT
        var hasKey = _sharedCache.ContainsKey(99);
        Assert.That(hasKey, Is.True);
#elif MSTEST
        var hasKey = _sharedCache.ContainsKey(99);
        Assert.IsTrue(hasKey);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test10()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test10()
#elif NUNIT
    [Test]
    public void SharedFixture_Test10()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test10()
#endif
    {
#if XUNIT || XUNIT3
        var record = _fixture.ProcessedRecords[25];
        Assert.Equal(2500, record.Value);
#elif TUNIT
        var record = _processedRecords[25];
        await Assert.That(record.Value).IsEqualTo(2500);
#elif NUNIT
        var record = _processedRecords[25];
        Assert.That(record.Value, Is.EqualTo(2500));
#elif MSTEST
        var record = _processedRecords[25];
        Assert.AreEqual(2500, record.Value);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test11()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test11()
#elif NUNIT
    [Test]
    public void SharedFixture_Test11()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test11()
#endif
    {
#if XUNIT || XUNIT3
        var firstValue = _fixture.SharedCache[0];
        Assert.StartsWith("CachedValue_0", firstValue);
#elif TUNIT
        var firstValue = _sharedCache[0];
        await Assert.That(firstValue).StartsWith("CachedValue_0");
#elif NUNIT
        var firstValue = _sharedCache[0];
        Assert.That(firstValue, Does.StartWith("CachedValue_0"));
#elif MSTEST
        var firstValue = _sharedCache[0];
        Assert.IsTrue(firstValue.StartsWith("CachedValue_0"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test12()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test12()
#elif NUNIT
    [Test]
    public void SharedFixture_Test12()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test12()
#endif
    {
#if XUNIT || XUNIT3
        var names = _fixture.ProcessedRecords.Select(r => r.Name).ToList();
        Assert.Equal(50, names.Count);
#elif TUNIT
        var names = _processedRecords.Select(r => r.Name).ToList();
        await Assert.That(names).HasCount(50);
#elif NUNIT
        var names = _processedRecords.Select(r => r.Name).ToList();
        Assert.That(names.Count, Is.EqualTo(50));
#elif MSTEST
        var names = _processedRecords.Select(r => r.Name).ToList();
        Assert.AreEqual(50, names.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test13()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test13()
#elif NUNIT
    [Test]
    public void SharedFixture_Test13()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test13()
#endif
    {
#if XUNIT || XUNIT3
        var json = _fixture.ConfigurationJson;
        Assert.Contains("Environment", json);
#elif TUNIT
        var json = _configurationJson;
        await Assert.That(json).Contains("Environment");
#elif NUNIT
        var json = _configurationJson;
        Assert.That(json, Does.Contain("Environment"));
#elif MSTEST
        var json = _configurationJson;
        Assert.IsTrue(json.Contains("Environment"));
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test14()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test14()
#elif NUNIT
    [Test]
    public void SharedFixture_Test14()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test14()
#endif
    {
#if XUNIT || XUNIT3
        var lastValue = _fixture.SharedCache[99];
        Assert.NotEmpty(lastValue);
#elif TUNIT
        var lastValue = _sharedCache[99];
        await Assert.That(lastValue).IsNotEmpty();
#elif NUNIT
        var lastValue = _sharedCache[99];
        Assert.That(lastValue, Is.Not.Empty);
#elif MSTEST
        var lastValue = _sharedCache[99];
        Assert.IsTrue(lastValue.Length > 0);
#endif
    }

#if TUNIT
    [Test]
    public async Task SharedFixture_Test15()
#elif XUNIT || XUNIT3
    [Fact]
    public void SharedFixture_Test15()
#elif NUNIT
    [Test]
    public void SharedFixture_Test15()
#elif MSTEST
    [TestMethod]
    public void SharedFixture_Test15()
#endif
    {
#if XUNIT || XUNIT3
        var avgValue = _fixture.ProcessedRecords.Average(r => r.Value);
        Assert.Equal(2450.0, avgValue);
#elif TUNIT
        var avgValue = _processedRecords.Average(r => r.Value);
        await Assert.That(avgValue).IsEqualTo(2450.0);
#elif NUNIT
        var avgValue = _processedRecords.Average(r => r.Value);
        Assert.That(avgValue, Is.EqualTo(2450.0));
#elif MSTEST
        var avgValue = _processedRecords.Average(r => r.Value);
        Assert.AreEqual(2450.0, avgValue);
#endif
    }
}
