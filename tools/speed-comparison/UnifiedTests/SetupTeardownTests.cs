using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
public class SetupTeardownTests : IDisposable
#elif NUNIT
[TestFixture]
public class SetupTeardownTests : IDisposable
#elif XUNIT
public class SetupTeardownTests : IDisposable
#else
public class SetupTeardownTests : IDisposable
#endif
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

#if TUNIT
    [Before(Test)]
    public void BeforeEachTest()
#elif MSTEST
    [TestInitialize]
    public void BeforeEachTest()
#elif NUNIT
    [SetUp]
    public void BeforeEachTest()
#elif XUNIT
    private void BeforeEachTest()
#endif
    {
        _setupCounter++;
        _testData.Clear();
        _testData.AddRange(new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" });

        _cache.Clear();
        for (var i = 0; i < 10; i++)
        {
            _cache[i] = $"Value_{i}";
        }
    }

#if TUNIT
    [After(Test)]
    public void AfterEachTest()
#elif MSTEST
    [TestCleanup]
    public void AfterEachTest()
#elif NUNIT
    [TearDown]
    public void AfterEachTest()
#elif XUNIT
    private void AfterEachTest()
#endif
    {
        _testData.Clear();
        _cache.Clear();
    }

#if TUNIT
    [Test]
    public async Task TestWithSetupData()
#elif XUNIT
    [Fact]
    public void TestWithSetupData()
#elif NUNIT
    [Test]
    public void TestWithSetupData()
#elif MSTEST
    [TestMethod]
    public void TestWithSetupData()
#endif
    {
#if XUNIT
        BeforeEachTest();
#endif

#if TUNIT
        await Assert.That(_testData).HasCount(5);
        await Assert.That(_testData[0]).IsEqualTo("Apple");
        await Assert.That(_cache).HasCount(10);
        await Assert.That(_cache[5]).IsEqualTo("Value_5");
#elif XUNIT
        Assert.Equal(5, _testData.Count);
        Assert.Equal("Apple", _testData[0]);
        Assert.Equal(10, _cache.Count);
        Assert.Equal("Value_5", _cache[5]);
#elif NUNIT
        Assert.That(_testData.Count, Is.EqualTo(5));
        Assert.That(_testData[0], Is.EqualTo("Apple"));
        Assert.That(_cache.Count, Is.EqualTo(10));
        Assert.That(_cache[5], Is.EqualTo("Value_5"));
#elif MSTEST
        Assert.AreEqual(5, _testData.Count);
        Assert.AreEqual("Apple", _testData[0]);
        Assert.AreEqual(10, _cache.Count);
        Assert.AreEqual("Value_5", _cache[5]);
#endif

        _testData.Add("Fig");
#if TUNIT
        await Assert.That(_testData).HasCount(6);
#elif XUNIT
        Assert.Equal(6, _testData.Count);
        AfterEachTest();
#elif NUNIT
        Assert.That(_testData.Count, Is.EqualTo(6));
#elif MSTEST
        Assert.AreEqual(6, _testData.Count);
#endif
    }

#if TUNIT
    [Test]
    public async Task TestDataIsolation()
#elif XUNIT
    [Fact]
    public void TestDataIsolation()
#elif NUNIT
    [Test]
    public void TestDataIsolation()
#elif MSTEST
    [TestMethod]
    public void TestDataIsolation()
#endif
    {
#if XUNIT
        BeforeEachTest();
#endif

#if TUNIT
        await Assert.That(_testData).HasCount(5);
        await Assert.That(_cache).HasCount(10);
#elif XUNIT
        Assert.Equal(5, _testData.Count);
        Assert.Equal(10, _cache.Count);
#elif NUNIT
        Assert.That(_testData.Count, Is.EqualTo(5));
        Assert.That(_cache.Count, Is.EqualTo(10));
#elif MSTEST
        Assert.AreEqual(5, _testData.Count);
        Assert.AreEqual(10, _cache.Count);
#endif

        _testData.RemoveAt(0);
        _cache.Remove(0);

#if TUNIT
        await Assert.That(_testData).HasCount(4);
        await Assert.That(_cache).HasCount(9);
#elif XUNIT
        Assert.Equal(4, _testData.Count);
        Assert.Equal(9, _cache.Count);
        AfterEachTest();
#elif NUNIT
        Assert.That(_testData.Count, Is.EqualTo(4));
        Assert.That(_cache.Count, Is.EqualTo(9));
#elif MSTEST
        Assert.AreEqual(4, _testData.Count);
        Assert.AreEqual(9, _cache.Count);
#endif
    }

    public void Dispose()
    {
        _testData?.Clear();
        _cache?.Clear();
    }
}