namespace NUnitTimer;

[TestFixture]
public class SetupTeardownTests : IDisposable
{
    private List<string> _testData;
    private Dictionary<int, string> _cache;
    private int _setupCounter;

    public SetupTeardownTests()
    {
        // Constructor
        _testData = new List<string>();
        _cache = new Dictionary<int, string>();
        _setupCounter = 0;
    }

    [SetUp]
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

    [TearDown]
    public void AfterEachTest()
    {
        _testData.Clear();
        _cache.Clear();
    }

    [Test]
    public void TestWithSetupData()
    {
        Assert.That(_testData.Count, Is.EqualTo(5));
        Assert.That(_testData[0], Is.EqualTo("Apple"));
        Assert.That(_cache.Count, Is.EqualTo(10));
        Assert.That(_cache[5], Is.EqualTo("Value_5"));
        
        _testData.Add("Fig");
        Assert.That(_testData.Count, Is.EqualTo(6));
    }

    [Test]
    public void TestDataIsolation()
    {
        Assert.That(_testData.Count, Is.EqualTo(5));
        Assert.That(_cache.Count, Is.EqualTo(10));
        
        _testData.RemoveAt(0);
        _cache.Remove(0);
        
        Assert.That(_testData.Count, Is.EqualTo(4));
        Assert.That(_cache.Count, Is.EqualTo(9));
    }

    [Test]
    public void TestWithDataManipulation()
    {
        var sorted = _testData.OrderBy(x => x).ToList();
        var reversed = _testData.AsEnumerable().Reverse().ToList();
        
        Assert.That(sorted[0], Is.EqualTo("Apple"));
        Assert.That(sorted[4], Is.EqualTo("Elderberry"));
        Assert.That(reversed[0], Is.EqualTo("Elderberry"));
        Assert.That(reversed[4], Is.EqualTo("Apple"));
    }

    [Test]
    [Repeat(5)]
    public void RepeatedTestWithSetup()
    {
        Assert.That(_testData.Count, Is.EqualTo(5));
        Assert.That(_cache.Count, Is.EqualTo(10));
        
        var sum = _cache.Keys.Sum();
        Assert.That(sum, Is.EqualTo(45));
    }

    public void Dispose()
    {
        _testData?.Clear();
        _cache?.Clear();
    }
}

[TestFixture]
public class ClassSetupTeardownTests
{
    private static List<int> _sharedData = new();
    private static bool _isInitialized;

    [OneTimeSetUp]
    public static void ClassSetup()
    {
        _isInitialized = true;
        _sharedData = Enumerable.Range(1, 100).ToList();
    }

    [OneTimeTearDown]
    public static void ClassTeardown()
    {
        _isInitialized = false;
        _sharedData.Clear();
    }

    [Test]
    public void TestUsingClassData1()
    {
        Assert.That(_isInitialized, Is.True);
        Assert.That(_sharedData.Count, Is.EqualTo(100));
        Assert.That(_sharedData.Sum(), Is.EqualTo(5050));
    }

    [Test]
    public void TestUsingClassData2()
    {
        Assert.That(_isInitialized, Is.True);
        var evens = _sharedData.Where(x => x % 2 == 0).ToList();
        Assert.That(evens.Count, Is.EqualTo(50));
        Assert.That(evens.Sum(), Is.EqualTo(2550));
    }

    [Test]
    public void TestUsingClassData3()
    {
        Assert.That(_isInitialized, Is.True);
        var primes = _sharedData.Where(IsPrime).ToList();
        Assert.That(primes.Count, Is.EqualTo(25));
        Assert.That(primes.First(), Is.EqualTo(2));
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