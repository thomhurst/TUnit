namespace MSTestTimer;

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

[TestClass]
public class FixtureTests : IDisposable
{
    private readonly ITestDatabase _database;
    private readonly List<string> _testKeys;
    
    public FixtureTests()
    {
        _database = new TestDatabase();
        _testKeys = new List<string>();
    }
    
    [TestInitialize]
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
    
    [TestCleanup]
    public void CleanupTest()
    {
        foreach (var key in _testKeys)
        {
            _database.Add(key, "cleaned");
        }
        _testKeys.Clear();
    }
    
    [TestMethod]
    public void TestDatabaseOperations()
    {
        Assert.IsTrue(_database.Count >= 110);
        
        var value = _database.Get("test_5");
        Assert.AreEqual("test_value_5", value);
        
        _database.Add("custom_key", "custom_value");
        Assert.AreEqual("custom_value", _database.Get("custom_key"));
    }
    
    [TestMethod]
    public void TestFixtureIsolation()
    {
        var initialCount = _database.Count;
        
        for (int i = 0; i < 5; i++)
        {
            _database.Add($"isolation_{i}", $"value_{i}");
        }
        
        Assert.AreEqual(initialCount + 5, _database.Count);
        Assert.AreEqual("value_2", _database.Get("isolation_2"));
    }
    
    [TestMethod]
    public void TestRepeatedWithFixture()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i > 0) SetupTest(); // Re-setup for each iteration
            
            Assert.AreEqual(10, _testKeys.Count);
            Assert.IsNotNull(_database.Get("test_0"));
            Assert.IsNotNull(_database.Get("test_9"));
            
            var sum = _testKeys.Count + _database.Count;
            Assert.IsTrue(sum > 100);
            
            if (i < 2) CleanupTest(); // Clean between iterations
        }
    }
    
    public void Dispose()
    {
        (_database as IDisposable)?.Dispose();
    }
}

[TestClass]
public class SharedFixtureTests
{
    private static TestDatabase? _sharedDatabase;
    private readonly string _instanceId = Guid.NewGuid().ToString();
    
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _sharedDatabase = new TestDatabase();
    }
    
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _sharedDatabase?.Dispose();
    }
    
    [TestMethod]
    public void TestWithSharedResource1()
    {
        Assert.IsNotNull(_sharedDatabase);
        var key = $"shared_{_instanceId}_1";
        _sharedDatabase.Add(key, "value1");
        
        Assert.AreEqual("value1", _sharedDatabase.Get(key));
        Assert.IsTrue(_sharedDatabase.Count > 100);
    }
    
    [TestMethod]
    public void TestWithSharedResource2()
    {
        Assert.IsNotNull(_sharedDatabase);
        var key = $"shared_{_instanceId}_2";
        _sharedDatabase.Add(key, "value2");
        
        Assert.AreEqual("value2", _sharedDatabase.Get(key));
        Assert.IsTrue(_sharedDatabase.Count > 100);
    }
    
    [TestMethod]
    public void TestWithSharedResource3()
    {
        Assert.IsNotNull(_sharedDatabase);
        var count = _sharedDatabase.Count;
        var key = $"shared_{_instanceId}_3";
        _sharedDatabase.Add(key, "value3");
        
        Assert.AreEqual(count + 1, _sharedDatabase.Count);
        Assert.IsNotNull(_sharedDatabase.Get(key));
    }
}