namespace xUnitTimerV3;

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

public class FixtureTests : IDisposable, IClassFixture<TestDatabase>
{
    private readonly ITestDatabase _database;
    private readonly List<string> _testKeys;
    
    public FixtureTests(TestDatabase database)
    {
        _database = database;
        _testKeys = new List<string>();
        SetupTest();
    }
    
    private void SetupTest()
    {
        _testKeys.Clear();
        for (int i = 0; i < 10; i++)
        {
            var key = $"test_{Guid.NewGuid()}_{i}";
            _testKeys.Add(key);
            _database.Add(key, $"test_value_{i}");
        }
    }
    
    private void CleanupTest()
    {
        foreach (var key in _testKeys)
        {
            _database.Add(key, "cleaned");
        }
        _testKeys.Clear();
    }
    
    [Fact]
    public void TestDatabaseOperations()
    {
        Assert.True(_database.Count >= 110);
        
        var value = _database.Get(_testKeys[5]);
        Assert.Equal("test_value_5", value);
        
        _database.Add("custom_key", "custom_value");
        Assert.Equal("custom_value", _database.Get("custom_key"));
        
        CleanupTest();
    }
    
    [Fact]
    public void TestFixtureIsolation()
    {
        SetupTest(); // Reset for this test
        var initialCount = _database.Count;
        
        for (int i = 0; i < 5; i++)
        {
            _database.Add($"isolation_{i}", $"value_{i}");
        }
        
        Assert.Equal(initialCount + 5, _database.Count);
        Assert.Equal("value_2", _database.Get("isolation_2"));
        
        CleanupTest();
    }
    
    [Fact]
    public void TestRepeatedWithFixture()
    {
        for (int i = 0; i < 3; i++)
        {
            SetupTest(); // Reset for each iteration
            
            Assert.Equal(10, _testKeys.Count);
            Assert.NotNull(_database.Get(_testKeys[0]));
            Assert.NotNull(_database.Get(_testKeys[9]));
            
            var sum = _testKeys.Count + _database.Count;
            Assert.True(sum > 100);
            
            CleanupTest();
        }
    }
    
    public void Dispose()
    {
        CleanupTest();
    }
}

public class SharedDatabaseFixture : IDisposable
{
    public TestDatabase Database { get; }
    
    public SharedDatabaseFixture()
    {
        Database = new TestDatabase();
    }
    
    public void Dispose()
    {
        Database.Dispose();
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<SharedDatabaseFixture>
{
}

[Collection("Database collection")]
public class SharedFixtureTests
{
    private readonly SharedDatabaseFixture _fixture;
    private readonly string _instanceId = Guid.NewGuid().ToString();
    
    public SharedFixtureTests(SharedDatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void TestWithSharedResource1()
    {
        var key = $"shared_{_instanceId}_1";
        _fixture.Database.Add(key, "value1");
        
        Assert.Equal("value1", _fixture.Database.Get(key));
        Assert.True(_fixture.Database.Count > 100);
    }
    
    [Fact]
    public void TestWithSharedResource2()
    {
        var key = $"shared_{_instanceId}_2";
        _fixture.Database.Add(key, "value2");
        
        Assert.Equal("value2", _fixture.Database.Get(key));
        Assert.True(_fixture.Database.Count > 100);
    }
    
    [Fact]
    public void TestWithSharedResource3()
    {
        var count = _fixture.Database.Count;
        var key = $"shared_{_instanceId}_3";
        _fixture.Database.Add(key, "value3");
        
        Assert.Equal(count + 1, _fixture.Database.Count);
        Assert.NotNull(_fixture.Database.Get(key));
    }
}