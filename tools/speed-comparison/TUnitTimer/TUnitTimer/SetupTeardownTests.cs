namespace TUnitTimer;

public class SetupTeardownTests : IDisposable
{
    private List<string> _testData;
    private Dictionary<int, string> _cache;
    private int _setupCounter;

    public SetupTeardownTests()
    {
        // Constructor acts as setup
        _testData = new List<string>();
        _cache = new Dictionary<int, string>();
        _setupCounter = 0;
    }

    [Before(Test)]
    public void BeforeEachTest()
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

    [After(Test)]
    public void AfterEachTest()
    {
        _testData.Clear();
        _cache.Clear();
    }

    [Test]
    public void TestWithSetupData()
    {
        Assert.That(_testData).HasCount(5);
        Assert.That(_testData[0]).IsEqualTo("Apple");
        Assert.That(_cache).HasCount(10);
        Assert.That(_cache[5]).IsEqualTo("Value_5");
        
        _testData.Add("Fig");
        Assert.That(_testData).HasCount(6);
    }

    [Test]
    public void TestDataIsolation()
    {
        Assert.That(_testData).HasCount(5);
        Assert.That(_cache).HasCount(10);
        
        _testData.RemoveAt(0);
        _cache.Remove(0);
        
        Assert.That(_testData).HasCount(4);
        Assert.That(_cache).HasCount(9);
    }

    [Test]
    public void TestWithDataManipulation()
    {
        var sorted = _testData.OrderBy(x => x).ToList();
        var reversed = _testData.AsEnumerable().Reverse().ToList();
        
        Assert.That(sorted[0]).IsEqualTo("Apple");
        Assert.That(sorted[4]).IsEqualTo("Elderberry");
        Assert.That(reversed[0]).IsEqualTo("Elderberry");
        Assert.That(reversed[4]).IsEqualTo("Apple");
    }

    [Test]
    [Repeat(5)]
    public void RepeatedTestWithSetup()
    {
        Assert.That(_testData).HasCount(5);
        Assert.That(_cache).HasCount(10);
        
        var sum = _cache.Keys.Sum();
        Assert.That(sum).IsEqualTo(45);
    }

    public void Dispose()
    {
        _testData?.Clear();
        _cache?.Clear();
    }
}

[Before(Class)]
public class ClassSetupTeardownTests
{
    private static List<int> _sharedData = new();
    private static bool _isInitialized;

    [Before(Class)]
    public static void ClassSetup()
    {
        _isInitialized = true;
        _sharedData = Enumerable.Range(1, 100).ToList();
    }

    [After(Class)]
    public static void ClassTeardown()
    {
        _isInitialized = false;
        _sharedData.Clear();
    }

    [Test]
    public void TestUsingClassData1()
    {
        Assert.That(_isInitialized).IsTrue();
        Assert.That(_sharedData).HasCount(100);
        Assert.That(_sharedData.Sum()).IsEqualTo(5050);
    }

    [Test]
    public void TestUsingClassData2()
    {
        Assert.That(_isInitialized).IsTrue();
        var evens = _sharedData.Where(x => x % 2 == 0).ToList();
        Assert.That(evens).HasCount(50);
        Assert.That(evens.Sum()).IsEqualTo(2550);
    }

    [Test]
    public void TestUsingClassData3()
    {
        Assert.That(_isInitialized).IsTrue();
        var primes = _sharedData.Where(IsPrime).ToList();
        Assert.That(primes).HasCount(25);
        Assert.That(primes.First()).IsEqualTo(2);
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