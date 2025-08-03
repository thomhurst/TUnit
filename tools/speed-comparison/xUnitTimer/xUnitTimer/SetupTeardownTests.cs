namespace xUnitTimer;

public class SetupTeardownTests : IDisposable
{
    private readonly List<string> _testData;
    private readonly Dictionary<int, string> _cache;
    private int _setupCounter;

    public SetupTeardownTests()
    {
        // Constructor acts as setup
        _testData = new List<string>();
        _cache = new Dictionary<int, string>();
        _setupCounter = 0;
        
        // Setup test data
        BeforeEachTest();
    }

    private void BeforeEachTest()
    {
        _setupCounter++;
        _testData.Clear();
        _testData.AddRange(new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" });
        
        _cache.Clear();
        for (int i = 0; i < 10; i++)
        {
            _cache[i] = $"Value_{i}";
        }
    }

    private void AfterEachTest()
    {
        _testData.Clear();
        _cache.Clear();
    }

    [Fact]
    public void TestWithSetupData()
    {
        Assert.Equal(5, _testData.Count);
        Assert.Equal("Apple", _testData[0]);
        Assert.Equal(10, _cache.Count);
        Assert.Equal("Value_5", _cache[5]);
        
        _testData.Add("Fig");
        Assert.Equal(6, _testData.Count);
        
        AfterEachTest();
    }

    [Fact]
    public void TestDataIsolation()
    {
        BeforeEachTest(); // Reset for this test
        
        Assert.Equal(5, _testData.Count);
        Assert.Equal(10, _cache.Count);
        
        _testData.RemoveAt(0);
        _cache.Remove(0);
        
        Assert.Equal(4, _testData.Count);
        Assert.Equal(9, _cache.Count);
        
        AfterEachTest();
    }

    [Fact]
    public void TestWithDataManipulation()
    {
        BeforeEachTest(); // Reset for this test
        
        var sorted = _testData.OrderBy(x => x).ToList();
        var reversed = _testData.AsEnumerable().Reverse().ToList();
        
        Assert.Equal("Apple", sorted[0]);
        Assert.Equal("Elderberry", sorted[4]);
        Assert.Equal("Elderberry", reversed[0]);
        Assert.Equal("Apple", reversed[4]);
        
        AfterEachTest();
    }

    public void Dispose()
    {
        _testData?.Clear();
        _cache?.Clear();
    }
}

public class ClassFixtureTests : IClassFixture<SharedTestFixture>
{
    private readonly SharedTestFixture _fixture;
    private readonly string _instanceId = Guid.NewGuid().ToString();

    public ClassFixtureTests(SharedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void TestUsingClassData1()
    {
        Assert.True(_fixture.IsInitialized);
        Assert.Equal(100, _fixture.SharedData.Count);
        Assert.Equal(5050, _fixture.SharedData.Sum());
    }

    [Fact]
    public void TestUsingClassData2()
    {
        Assert.True(_fixture.IsInitialized);
        var evens = _fixture.SharedData.Where(x => x % 2 == 0).ToList();
        Assert.Equal(50, evens.Count);
        Assert.Equal(2550, evens.Sum());
    }

    [Fact]
    public void TestUsingClassData3()
    {
        Assert.True(_fixture.IsInitialized);
        var primes = _fixture.SharedData.Where(IsPrime).ToList();
        Assert.Equal(25, primes.Count);
        Assert.Equal(2, primes.First());
    }

    private static bool IsPrime(int n)
    {
        if (n <= 1) return false;
        if (n <= 3) return true;
        if (n % 2 == 0 || n % 3 == 0) return false;
        
        for (int i = 5; i * i <= n; i += 6)
        {
            if (n % i == 0 || n % (i + 2) == 0)
                return false;
        }
        return true;
    }
}

public class SharedTestFixture : IDisposable
{
    public List<int> SharedData { get; }
    public bool IsInitialized { get; private set; }

    public SharedTestFixture()
    {
        IsInitialized = true;
        SharedData = Enumerable.Range(1, 100).ToList();
    }

    public void Dispose()
    {
        IsInitialized = false;
        SharedData.Clear();
    }
}