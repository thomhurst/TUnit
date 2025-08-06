using System.Threading.Tasks;

namespace TUnitTimer;

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
        for (int i = 0; i < 100; i++)
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

public class FixtureTests : IDisposable
{
    private readonly ITestDatabase _database;
    private readonly List<string> _testKeys;

    public FixtureTests()
    {
        _database = new TestDatabase();
        _testKeys = new List<string>();
    }

    [Before(Test)]
    public void SetupTest()
    {
        _testKeys.Clear();
        for (int i = 0; i < 10; i++)
        {
            var key = $"test_{i}";
            _testKeys.Add(key);
            _database.Add(key, $"test_value_{i}");
        }
    }

    [After(Test)]
    public void CleanupTest()
    {
        foreach (var key in _testKeys)
        {
            _database.Add(key, "cleaned");
        }
        _testKeys.Clear();
    }

    [Test]
    public async Task TestDatabaseOperations()
    {
        await Assert.That(_database.Count).IsGreaterThanOrEqualTo(110);

        var value = _database.Get("test_5");
        await Assert.That(value).IsEqualTo("test_value_5");

        _database.Add("custom_key", "custom_value");
        await Assert.That(_database.Get("custom_key")).IsEqualTo("custom_value");
    }

    [Test]
    public async Task TestFixtureIsolation()
    {
        var initialCount = _database.Count;

        for (int i = 0; i < 5; i++)
        {
            _database.Add($"isolation_{i}", $"value_{i}");
        }

        await Assert.That(_database.Count).IsEqualTo(initialCount + 5);
        await Assert.That(_database.Get("isolation_2")).IsEqualTo("value_2");
    }

    [Test]
    [Repeat(3)]
    public async Task TestRepeatedWithFixture()
    {
        await Assert.That(_testKeys).HasCount(10);
        await Assert.That(_database.Get("test_0")).IsNotNull();
        await Assert.That(_database.Get("test_9")).IsNotNull();

        var sum = _testKeys.Count + _database.Count;
        await Assert.That(sum).IsGreaterThan(100);
    }

    public void Dispose()
    {
        (_database as IDisposable)?.Dispose();
    }
}

public class SharedFixtureTests
{
    private static readonly TestDatabase SharedDatabase = new();
    private readonly string _instanceId = Guid.NewGuid().ToString();

    [Test]
    public async Task TestWithSharedResource1()
    {
        var key = $"shared_{_instanceId}_1";
        SharedDatabase.Add(key, "value1");

        await Assert.That(SharedDatabase.Get(key)).IsEqualTo("value1");
        await Assert.That(SharedDatabase.Count).IsGreaterThan(100);
    }

    [Test]
    public async Task TestWithSharedResource2()
    {
        var key = $"shared_{_instanceId}_2";
        SharedDatabase.Add(key, "value2");

        await Assert.That(SharedDatabase.Get(key)).IsEqualTo("value2");
        await Assert.That(SharedDatabase.Count).IsGreaterThan(100);
    }

    [Test]
    public async Task TestWithSharedResource3()
    {
        var count = SharedDatabase.Count;
        var key = $"shared_{_instanceId}_3";
        SharedDatabase.Add(key, "value3");

        await Assert.That(SharedDatabase.Count).IsEqualTo(count + 1);
        await Assert.That(SharedDatabase.Get(key)).IsNotNull();
    }
}
