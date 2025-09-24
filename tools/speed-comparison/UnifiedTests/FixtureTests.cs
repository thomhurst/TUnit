using System.Threading.Tasks;

namespace UnifiedTests;

public interface ITestDatabase
{
    void Add(string key, string value);
    string? Get(string key);
    int Count { get; }
    void Clear();
}

public class TestDatabase : ITestDatabase, IDisposable
{
    private readonly Dictionary<string, string> _data = new();

    public TestDatabase()
    {
        // Simulate expensive initialization
        for (var i = 0; i < 100; i++)
        {
            _data[$"init_{i}"] = $"value_{i}";
        }
    }

    public void Add(string key, string value) => _data[key] = value;
    public string? Get(string key) => _data.TryGetValue(key, out var value) ? value : null;
    public int Count => _data.Count;
    public void Clear() => _data.Clear();

    public void Dispose()
    {
        _data.Clear();
    }
}

#if MSTEST
[TestClass]
public class FixtureTests : IDisposable
#elif NUNIT
[TestFixture]
public class FixtureTests : IDisposable
#elif XUNIT
public class FixtureTests : IDisposable, IClassFixture<TestDatabase>
#else
public class FixtureTests : IDisposable
#endif
{
    private readonly ITestDatabase _database;
    private readonly List<string> _testKeys;

#if XUNIT
    public FixtureTests(TestDatabase database)
    {
        _database = database;
        _testKeys = new List<string>();
        SetupTest();
    }
#else
    public FixtureTests()
    {
        _database = new TestDatabase();
        _testKeys = new List<string>();
    }
#endif

#if TUNIT
    [Before(Test)]
    public void SetupTest()
#elif MSTEST
    [TestInitialize]
    public void SetupTest()
#elif NUNIT
    [SetUp]
    public void SetupTest()
#elif XUNIT
    private void SetupTest()
#endif
    {
        _testKeys.Clear();
        for (var i = 0; i < 10; i++)
        {
#if XUNIT
            var key = $"test_{Guid.NewGuid()}_{i}";
#else
            var key = $"test_{i}";
#endif
            _testKeys.Add(key);
            _database.Add(key, $"test_value_{i}");
        }
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
#elif XUNIT
    private void CleanupTest()
#endif
    {
        foreach (var key in _testKeys)
        {
            _database.Add(key, "cleaned");
        }
        _testKeys.Clear();
    }

#if TUNIT
    [Test]
    public async Task TestDatabaseOperations()
#elif XUNIT
    [Fact]
    public void TestDatabaseOperations()
#elif NUNIT
    [Test]
    public void TestDatabaseOperations()
#elif MSTEST
    [TestMethod]
    public void TestDatabaseOperations()
#endif
    {
#if TUNIT
        await Assert.That(_database.Count).IsGreaterThanOrEqualTo(110);
        var value = _database.Get("test_5");
        await Assert.That(value).IsEqualTo("test_value_5");
#elif XUNIT
        Assert.True(_database.Count >= 110);
        var value = _database.Get(_testKeys[5]);
        Assert.Equal("test_value_5", value);
#elif NUNIT
        Assert.That(_database.Count, Is.GreaterThanOrEqualTo(110));
        var value = _database.Get("test_5");
        Assert.That(value, Is.EqualTo("test_value_5"));
#elif MSTEST
        Assert.IsTrue(_database.Count >= 110);
        var value = _database.Get("test_5");
        Assert.AreEqual("test_value_5", value);
#endif

        _database.Add("custom_key", "custom_value");
#if TUNIT
        await Assert.That(_database.Get("custom_key")).IsEqualTo("custom_value");
#elif XUNIT
        Assert.Equal("custom_value", _database.Get("custom_key"));
        CleanupTest();
#elif NUNIT
        Assert.That(_database.Get("custom_key"), Is.EqualTo("custom_value"));
#elif MSTEST
        Assert.AreEqual("custom_value", _database.Get("custom_key"));
#endif
    }

    public void Dispose()
    {
        (_database as IDisposable)?.Dispose();
    }
}