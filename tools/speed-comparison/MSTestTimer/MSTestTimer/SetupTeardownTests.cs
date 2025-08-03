namespace MSTestTimer;

[TestClass]
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

    [TestInitialize]
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

    [TestCleanup]
    public void AfterEachTest()
    {
        _testData.Clear();
        _cache.Clear();
    }

    [TestMethod]
    public void TestWithSetupData()
    {
        Assert.AreEqual(5, _testData.Count);
        Assert.AreEqual("Apple", _testData[0]);
        Assert.AreEqual(10, _cache.Count);
        Assert.AreEqual("Value_5", _cache[5]);
        
        _testData.Add("Fig");
        Assert.AreEqual(6, _testData.Count);
    }

    [TestMethod]
    public void TestDataIsolation()
    {
        Assert.AreEqual(5, _testData.Count);
        Assert.AreEqual(10, _cache.Count);
        
        _testData.RemoveAt(0);
        _cache.Remove(0);
        
        Assert.AreEqual(4, _testData.Count);
        Assert.AreEqual(9, _cache.Count);
    }

    [TestMethod]
    public void TestWithDataManipulation()
    {
        var sorted = _testData.OrderBy(x => x).ToList();
        var reversed = _testData.AsEnumerable().Reverse().ToList();
        
        Assert.AreEqual("Apple", sorted[0]);
        Assert.AreEqual("Elderberry", sorted[4]);
        Assert.AreEqual("Elderberry", reversed[0]);
        Assert.AreEqual("Apple", reversed[4]);
    }

    public void Dispose()
    {
        _testData?.Clear();
        _cache?.Clear();
    }
}

[TestClass]
public class ClassSetupTeardownTests
{
    private static List<int> _sharedData = new();
    private static bool _isInitialized;

    [ClassInitialize]
    public static void ClassSetup(TestContext context)
    {
        _isInitialized = true;
        _sharedData = Enumerable.Range(1, 100).ToList();
    }

    [ClassCleanup]
    public static void ClassTeardown()
    {
        _isInitialized = false;
        _sharedData.Clear();
    }

    [TestMethod]
    public void TestUsingClassData1()
    {
        Assert.IsTrue(_isInitialized);
        Assert.AreEqual(100, _sharedData.Count);
        Assert.AreEqual(5050, _sharedData.Sum());
    }

    [TestMethod]
    public void TestUsingClassData2()
    {
        Assert.IsTrue(_isInitialized);
        var evens = _sharedData.Where(x => x % 2 == 0).ToList();
        Assert.AreEqual(50, evens.Count);
        Assert.AreEqual(2550, evens.Sum());
    }

    [TestMethod]
    public void TestUsingClassData3()
    {
        Assert.IsTrue(_isInitialized);
        var primes = _sharedData.Where(IsPrime).ToList();
        Assert.AreEqual(25, primes.Count);
        Assert.AreEqual(2, primes.First());
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