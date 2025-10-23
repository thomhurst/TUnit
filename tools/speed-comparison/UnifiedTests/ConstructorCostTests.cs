using System.Text.Json;
using System.Threading.Tasks;

namespace UnifiedTests;

/// <summary>
/// Tests measuring the overhead of expensive constructor initialization.
/// All tests in this class share the same constructor, allowing measurement
/// of per-class initialization cost amortized across multiple tests.
/// </summary>
#if MSTEST
[TestClass]
public class ConstructorCostTests
#elif NUNIT
[TestFixture]
public class ConstructorCostTests
#elif XUNIT || XUNIT3
public class ConstructorCostTests
#else
public class ConstructorCostTests
#endif
{
    private readonly Dictionary<string, object> _configCache;
    private readonly List<string> _processedData;
    private readonly HashSet<int> _indexLookup;
    private readonly string _serializedConfig;
    private readonly int _totalRecords;

    public ConstructorCostTests()
    {
        // Simulate expensive constructor initialization
        // Realistic work: JSON parsing, collection building, computation

        // Build configuration cache (simulates loading config)
        _configCache = [];
        for (var i = 0; i < 50; i++)
        {
            var config = new
            {
                Id = i,
                Name = $"Config_{i}",
                Enabled = i % 2 == 0,
                Priority = i * 10,
                Tags = new[] { $"tag_{i}", $"category_{i % 5}" },
                Timestamp = DateTime.UtcNow.Ticks
            };
            _configCache[$"config_{i}"] = config;
        }

        // Process and cache data (simulates data transformation)
        _processedData = [];
        for (var i = 0; i < 100; i++)
        {
            var data = $"Record_{i}_Data_{Guid.NewGuid().ToString()[..8]}";
            var processed = data.ToUpperInvariant() + $"_Processed_{i * 2}";
            _processedData.Add(processed);
        }

        // Build index lookup (simulates index creation)
        _indexLookup = [];
        for (var i = 0; i < 100; i++)
        {
            _indexLookup.Add(i * 3);
        }

        // Serialize configuration (simulates config serialization)
        _serializedConfig = JsonSerializer.Serialize(_configCache);

        _totalRecords = _processedData.Count;
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_AccessConfigCache()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_AccessConfigCache()
#elif NUNIT
    [Test]
    public void Constructor_Test_AccessConfigCache()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_AccessConfigCache()
#endif
    {
        var config = _configCache["config_10"];

#if TUNIT
        await Assert.That(config).IsNotNull();
        await Assert.That(_configCache).HasCount(50);
#elif XUNIT || XUNIT3
        Assert.NotNull(config);
        Assert.Equal(50, _configCache.Count);
#elif NUNIT
        Assert.That(config, Is.Not.Null);
        Assert.That(_configCache.Count, Is.EqualTo(50));
#elif MSTEST
        Assert.IsNotNull(config);
        Assert.AreEqual(50, _configCache.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_AccessProcessedData()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_AccessProcessedData()
#elif NUNIT
    [Test]
    public void Constructor_Test_AccessProcessedData()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_AccessProcessedData()
#endif
    {
        var firstRecord = _processedData[0];

#if TUNIT
        await Assert.That(firstRecord).Contains("RECORD_0");
        await Assert.That(firstRecord).Contains("Processed_0");
#elif XUNIT || XUNIT3
        Assert.Contains("RECORD_0", firstRecord);
        Assert.Contains("Processed_0", firstRecord);
#elif NUNIT
        Assert.That(firstRecord, Does.Contain("RECORD_0"));
        Assert.That(firstRecord, Does.Contain("Processed_0"));
#elif MSTEST
        Assert.IsTrue(firstRecord.Contains("RECORD_0"));
        Assert.IsTrue(firstRecord.Contains("Processed_0"));
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_IndexLookup()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_IndexLookup()
#elif NUNIT
    [Test]
    public void Constructor_Test_IndexLookup()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_IndexLookup()
#endif
    {
        var hasValue = _indexLookup.Contains(15);

#if TUNIT
        await Assert.That(hasValue).IsTrue();
        await Assert.That(_indexLookup).HasCount(100);
#elif XUNIT || XUNIT3
        Assert.True(hasValue);
        Assert.Equal(100, _indexLookup.Count);
#elif NUNIT
        Assert.That(hasValue, Is.True);
        Assert.That(_indexLookup.Count, Is.EqualTo(100));
#elif MSTEST
        Assert.IsTrue(hasValue);
        Assert.AreEqual(100, _indexLookup.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_SerializedConfig()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_SerializedConfig()
#elif NUNIT
    [Test]
    public void Constructor_Test_SerializedConfig()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_SerializedConfig()
#endif
    {
        var isValid = _serializedConfig.Length > 100;

#if TUNIT
        await Assert.That(_serializedConfig).IsNotEmpty();
        await Assert.That(isValid).IsTrue();
#elif XUNIT || XUNIT3
        Assert.NotEmpty(_serializedConfig);
        Assert.True(isValid);
#elif NUNIT
        Assert.That(_serializedConfig, Is.Not.Empty);
        Assert.That(isValid, Is.True);
#elif MSTEST
        Assert.IsTrue(_serializedConfig.Length > 0);
        Assert.IsTrue(isValid);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_TotalRecords()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_TotalRecords()
#elif NUNIT
    [Test]
    public void Constructor_Test_TotalRecords()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_TotalRecords()
#endif
    {
#if TUNIT
        await Assert.That(_totalRecords).IsEqualTo(100);
        await Assert.That(_processedData).HasCount(_totalRecords);
#elif XUNIT || XUNIT3
        Assert.Equal(100, _totalRecords);
        Assert.Equal(_totalRecords, _processedData.Count);
#elif NUNIT
        Assert.That(_totalRecords, Is.EqualTo(100));
        Assert.That(_processedData.Count, Is.EqualTo(_totalRecords));
#elif MSTEST
        Assert.AreEqual(100, _totalRecords);
        Assert.AreEqual(_totalRecords, _processedData.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_DataRange()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_DataRange()
#elif NUNIT
    [Test]
    public void Constructor_Test_DataRange()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_DataRange()
#endif
    {
        var midRecord = _processedData[50];

#if TUNIT
        await Assert.That(midRecord).Contains("RECORD_50");
        await Assert.That(midRecord).IsNotEmpty();
#elif XUNIT || XUNIT3
        Assert.Contains("RECORD_50", midRecord);
        Assert.NotEmpty(midRecord);
#elif NUNIT
        Assert.That(midRecord, Does.Contain("RECORD_50"));
        Assert.That(midRecord, Is.Not.Empty);
#elif MSTEST
        Assert.IsTrue(midRecord.Contains("RECORD_50"));
        Assert.IsTrue(midRecord.Length > 0);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_ConfigLookup()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_ConfigLookup()
#elif NUNIT
    [Test]
    public void Constructor_Test_ConfigLookup()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_ConfigLookup()
#endif
    {
        var hasKey = _configCache.ContainsKey("config_25");

#if TUNIT
        await Assert.That(hasKey).IsTrue();
        await Assert.That(_configCache["config_25"]).IsNotNull();
#elif XUNIT || XUNIT3
        Assert.True(hasKey);
        Assert.NotNull(_configCache["config_25"]);
#elif NUNIT
        Assert.That(hasKey, Is.True);
        Assert.That(_configCache["config_25"], Is.Not.Null);
#elif MSTEST
        Assert.IsTrue(hasKey);
        Assert.IsNotNull(_configCache["config_25"]);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_IndexContains()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_IndexContains()
#elif NUNIT
    [Test]
    public void Constructor_Test_IndexContains()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_IndexContains()
#endif
    {
        var contains30 = _indexLookup.Contains(30);
        var contains99 = _indexLookup.Contains(99);

#if TUNIT
        await Assert.That(contains30).IsTrue();
        await Assert.That(contains99).IsTrue();
#elif XUNIT || XUNIT3
        Assert.True(contains30);
        Assert.True(contains99);
#elif NUNIT
        Assert.That(contains30, Is.True);
        Assert.That(contains99, Is.True);
#elif MSTEST
        Assert.IsTrue(contains30);
        Assert.IsTrue(contains99);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_DataEndRange()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_DataEndRange()
#elif NUNIT
    [Test]
    public void Constructor_Test_DataEndRange()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_DataEndRange()
#endif
    {
        var lastRecord = _processedData[^1];

#if TUNIT
        await Assert.That(lastRecord).Contains("RECORD_99");
        await Assert.That(lastRecord).Contains("Processed_198");
#elif XUNIT || XUNIT3
        Assert.Contains("RECORD_99", lastRecord);
        Assert.Contains("Processed_198", lastRecord);
#elif NUNIT
        Assert.That(lastRecord, Does.Contain("RECORD_99"));
        Assert.That(lastRecord, Does.Contain("Processed_198"));
#elif MSTEST
        Assert.IsTrue(lastRecord.Contains("RECORD_99"));
        Assert.IsTrue(lastRecord.Contains("Processed_198"));
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_ConfigRange()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_ConfigRange()
#elif NUNIT
    [Test]
    public void Constructor_Test_ConfigRange()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_ConfigRange()
#endif
    {
        var firstConfig = _configCache["config_0"];
        var lastConfig = _configCache["config_49"];

#if TUNIT
        await Assert.That(firstConfig).IsNotNull();
        await Assert.That(lastConfig).IsNotNull();
#elif XUNIT || XUNIT3
        Assert.NotNull(firstConfig);
        Assert.NotNull(lastConfig);
#elif NUNIT
        Assert.That(firstConfig, Is.Not.Null);
        Assert.That(lastConfig, Is.Not.Null);
#elif MSTEST
        Assert.IsNotNull(firstConfig);
        Assert.IsNotNull(lastConfig);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_IndexMax()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_IndexMax()
#elif NUNIT
    [Test]
    public void Constructor_Test_IndexMax()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_IndexMax()
#endif
    {
        var maxValue = _indexLookup.Max();

#if TUNIT
        await Assert.That(maxValue).IsEqualTo(297);
#elif XUNIT || XUNIT3
        Assert.Equal(297, maxValue);
#elif NUNIT
        Assert.That(maxValue, Is.EqualTo(297));
#elif MSTEST
        Assert.AreEqual(297, maxValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_SerializedLength()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_SerializedLength()
#elif NUNIT
    [Test]
    public void Constructor_Test_SerializedLength()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_SerializedLength()
#endif
    {
        var length = _serializedConfig.Length;
        var hasContent = length > 1000;

#if TUNIT
        await Assert.That(hasContent).IsTrue();
#elif XUNIT || XUNIT3
        Assert.True(hasContent);
#elif NUNIT
        Assert.That(hasContent, Is.True);
#elif MSTEST
        Assert.IsTrue(hasContent);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_DataConsistency()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_DataConsistency()
#elif NUNIT
    [Test]
    public void Constructor_Test_DataConsistency()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_DataConsistency()
#endif
    {
        var allProcessed = _processedData.All(d => d.Contains("Processed_"));

#if TUNIT
        await Assert.That(allProcessed).IsTrue();
        await Assert.That(_processedData).IsNotEmpty();
#elif XUNIT || XUNIT3
        Assert.True(allProcessed);
        Assert.NotEmpty(_processedData);
#elif NUNIT
        Assert.That(allProcessed, Is.True);
        Assert.That(_processedData, Is.Not.Empty);
#elif MSTEST
        Assert.IsTrue(allProcessed);
        Assert.IsTrue(_processedData.Count > 0);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_IndexMin()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_IndexMin()
#elif NUNIT
    [Test]
    public void Constructor_Test_IndexMin()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_IndexMin()
#endif
    {
        var minValue = _indexLookup.Min();

#if TUNIT
        await Assert.That(minValue).IsEqualTo(0);
#elif XUNIT || XUNIT3
        Assert.Equal(0, minValue);
#elif NUNIT
        Assert.That(minValue, Is.EqualTo(0));
#elif MSTEST
        Assert.AreEqual(0, minValue);
#endif
    }

#if TUNIT
    [Test]
    public async Task Constructor_Test_ConfigCount()
#elif XUNIT || XUNIT3
    [Fact]
    public void Constructor_Test_ConfigCount()
#elif NUNIT
    [Test]
    public void Constructor_Test_ConfigCount()
#elif MSTEST
    [TestMethod]
    public void Constructor_Test_ConfigCount()
#endif
    {
        var count = _configCache.Count;
        var keysValid = _configCache.Keys.All(k => k.StartsWith("config_"));

#if TUNIT
        await Assert.That(count).IsEqualTo(50);
        await Assert.That(keysValid).IsTrue();
#elif XUNIT || XUNIT3
        Assert.Equal(50, count);
        Assert.True(keysValid);
#elif NUNIT
        Assert.That(count, Is.EqualTo(50));
        Assert.That(keysValid, Is.True);
#elif MSTEST
        Assert.AreEqual(50, count);
        Assert.IsTrue(keysValid);
#endif
    }
}
