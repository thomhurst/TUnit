using System.Threading.Tasks;

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
    public async Task TestWithSetupData()
    {
        await Assert.That(_testData).HasCount(5);
        await Assert.That(_testData[0]).IsEqualTo("Apple");
        await Assert.That(_cache).HasCount(10);
        await Assert.That(_cache[5]).IsEqualTo("Value_5");

        _testData.Add("Fig");
        await Assert.That(_testData).HasCount(6);
    }

    [Test]
    public async Task TestDataIsolation()
    {
        await Assert.That(_testData).HasCount(5);
        await Assert.That(_cache).HasCount(10);

        _testData.RemoveAt(0);
        _cache.Remove(0);

        await Assert.That(_testData).HasCount(4);
        await Assert.That(_cache).HasCount(9);
    }

    [Test]
    public async Task TestWithDataManipulation()
    {
        var sorted = _testData.OrderBy(x => x).ToList();
        var reversed = _testData.AsEnumerable().Reverse().ToList();

        await Assert.That(sorted[0]).IsEqualTo("Apple");
        await Assert.That(sorted[4]).IsEqualTo("Elderberry");
        await Assert.That(reversed[0]).IsEqualTo("Elderberry");
        await Assert.That(reversed[4]).IsEqualTo("Apple");
    }

    [Test]
    [Repeat(5)]
    public async Task RepeatedTestWithSetup()
    {
        await Assert.That(_testData).HasCount(5);
        await Assert.That(_cache).HasCount(10);

        var sum = _cache.Keys.Sum();
        await Assert.That(sum).IsEqualTo(45);
    }

    public void Dispose()
    {
        _testData?.Clear();
        _cache?.Clear();
    }
}

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
    public async Task TestUsingClassData1()
    {
        await Assert.That(_isInitialized).IsTrue();
        await Assert.That(_sharedData).HasCount(100);
        await Assert.That(_sharedData.Sum()).IsEqualTo(5050);
    }

    [Test]
    public async Task TestUsingClassData2()
    {
        await Assert.That(_isInitialized).IsTrue();
        var evens = _sharedData.Where(x => x % 2 == 0).ToList();
        await Assert.That(evens).HasCount(50);
        await Assert.That(evens.Sum()).IsEqualTo(2550);
    }

    [Test]
    public async Task TestUsingClassData3()
    {
        await Assert.That(_isInitialized).IsTrue();
        var primes = _sharedData.Where(IsPrime).ToList();
        await Assert.That(primes).HasCount(25);
        await Assert.That(primes.First()).IsEqualTo(2);
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
