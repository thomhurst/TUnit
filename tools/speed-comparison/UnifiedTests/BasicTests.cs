using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class BasicTests
{
#if TUNIT
    [Test]
    public async Task SimpleTest()
#elif XUNIT
    [Fact]
    public void SimpleTest()
#elif NUNIT
    [Test]
    public void SimpleTest()
#elif MSTEST
    [TestMethod]
    public void SimpleTest()
#endif
    {
        var result = CalculateSum(5, 10);
#if TUNIT
        await Assert.That(result).IsEqualTo(15);
#elif XUNIT
        Assert.Equal(15, result);
#elif NUNIT
        Assert.That(result, Is.EqualTo(15));
#elif MSTEST
        Assert.AreEqual(15, result);
#endif
    }

#if TUNIT
    [Test]
    public async Task MultipleAssertionsTest()
#elif XUNIT
    [Fact]
    public void MultipleAssertionsTest()
#elif NUNIT
    [Test]
    public void MultipleAssertionsTest()
#elif MSTEST
    [TestMethod]
    public void MultipleAssertionsTest()
#endif
    {
        var text = "Hello, World!";
        var numbers = new[] { 1, 2, 3, 4, 5 };

#if TUNIT
        await Assert.That(text).IsNotNull();
        await Assert.That(text.Length).IsEqualTo(13);
        await Assert.That(text).Contains("World");

        await Assert.That(numbers).HasCount(5);
        await Assert.That(numbers.Sum()).IsEqualTo(15);
        await Assert.That(numbers).Contains(3);
#elif XUNIT
        Assert.NotNull(text);
        Assert.Equal(13, text.Length);
        Assert.Contains("World", text);
        
        Assert.Equal(5, numbers.Length);
        Assert.Equal(15, numbers.Sum());
        Assert.Contains(3, numbers);
#elif NUNIT
        Assert.That(text, Is.Not.Null);
        Assert.That(text.Length, Is.EqualTo(13));
        Assert.That(text, Does.Contain("World"));
        
        Assert.That(numbers.Length, Is.EqualTo(5));
        Assert.That(numbers.Sum(), Is.EqualTo(15));
        Assert.That(numbers, Does.Contain(3));
#elif MSTEST
        Assert.IsNotNull(text);
        Assert.AreEqual(13, text.Length);
        Assert.IsTrue(text.Contains("World"));
        
        Assert.AreEqual(5, numbers.Length);
        Assert.AreEqual(15, numbers.Sum());
        CollectionAssert.Contains(numbers, 3);
#endif
    }

#if TUNIT
    [Test]
    public async Task CollectionOperationsTest()
#elif XUNIT
    [Fact]
    public void CollectionOperationsTest()
#elif NUNIT
    [Test]
    public void CollectionOperationsTest()
#elif MSTEST
    [TestMethod]
    public void CollectionOperationsTest()
#endif
    {
        var items = Enumerable.Range(1, 100).ToList();
        var filtered = items.Where(x => x % 2 == 0).ToList();
        var sum = filtered.Sum();

#if TUNIT
        await Assert.That(filtered).HasCount(50);
        await Assert.That(sum).IsEqualTo(2550);
        await Assert.That(filtered.First()).IsEqualTo(2);
        await Assert.That(filtered.Last()).IsEqualTo(100);
#elif XUNIT
        Assert.Equal(50, filtered.Count);
        Assert.Equal(2550, sum);
        Assert.Equal(2, filtered.First());
        Assert.Equal(100, filtered.Last());
#elif NUNIT
        Assert.That(filtered.Count, Is.EqualTo(50));
        Assert.That(sum, Is.EqualTo(2550));
        Assert.That(filtered.First(), Is.EqualTo(2));
        Assert.That(filtered.Last(), Is.EqualTo(100));
#elif MSTEST
        Assert.AreEqual(50, filtered.Count);
        Assert.AreEqual(2550, sum);
        Assert.AreEqual(2, filtered.First());
        Assert.AreEqual(100, filtered.Last());
#endif
    }

#if TUNIT
    [Test]
    public async Task StringManipulationTest()
#elif XUNIT
    [Fact]
    public void StringManipulationTest()
#elif NUNIT
    [Test]
    public void StringManipulationTest()
#elif MSTEST
    [TestMethod]
    public void StringManipulationTest()
#endif
    {
#if TUNIT
        var input = "  Hello, TUnit Testing Framework!  ";
#elif XUNIT
        var input = "  Hello, xUnit Testing Framework!  ";
#elif NUNIT
        var input = "  Hello, NUnit Testing Framework!  ";
#elif MSTEST
        var input = "  Hello, MSTest Testing Framework!  ";
#endif
        var trimmed = input.Trim();
        var upper = trimmed.ToUpper();
        var words = trimmed.Split(' ');

#if TUNIT
        await Assert.That(trimmed).IsEqualTo("Hello, TUnit Testing Framework!");
        await Assert.That(upper).IsEqualTo("HELLO, TUNIT TESTING FRAMEWORK!");
        await Assert.That(words).HasCount(4);
        await Assert.That(words[1]).IsEqualTo("TUnit");
#elif XUNIT
        Assert.Equal("Hello, xUnit Testing Framework!", trimmed);
        Assert.Equal("HELLO, XUNIT TESTING FRAMEWORK!", upper);
        Assert.Equal(4, words.Length);
        Assert.Equal("xUnit", words[1]);
#elif NUNIT
        Assert.That(trimmed, Is.EqualTo("Hello, NUnit Testing Framework!"));
        Assert.That(upper, Is.EqualTo("HELLO, NUNIT TESTING FRAMEWORK!"));
        Assert.That(words.Length, Is.EqualTo(4));
        Assert.That(words[1], Is.EqualTo("NUnit"));
#elif MSTEST
        Assert.AreEqual("Hello, MSTest Testing Framework!", trimmed);
        Assert.AreEqual("HELLO, MSTEST TESTING FRAMEWORK!", upper);
        Assert.AreEqual(4, words.Length);
        Assert.AreEqual("MSTest", words[1]);
#endif
    }

#if TUNIT
    [Test]
    public async Task DictionaryOperationsTest()
#elif XUNIT
    [Fact]
    public void DictionaryOperationsTest()
#elif NUNIT
    [Test]
    public void DictionaryOperationsTest()
#elif MSTEST
    [TestMethod]
    public void DictionaryOperationsTest()
#endif
    {
        var dictionary = new Dictionary<string, int>();
        for (var i = 0; i < 50; i++)
        {
            dictionary[$"key{i}"] = i * i;
        }

#if TUNIT
        await Assert.That(dictionary).HasCount(50);
        await Assert.That(dictionary["key10"]).IsEqualTo(100);
        await Assert.That(dictionary.ContainsKey("key25")).IsTrue();
        await Assert.That(dictionary.Values.Sum()).IsEqualTo(40425);
#elif XUNIT
        Assert.Equal(50, dictionary.Count);
        Assert.Equal(100, dictionary["key10"]);
        Assert.True(dictionary.ContainsKey("key25"));
        Assert.Equal(40425, dictionary.Values.Sum());
#elif NUNIT
        Assert.That(dictionary.Count, Is.EqualTo(50));
        Assert.That(dictionary["key10"], Is.EqualTo(100));
        Assert.That(dictionary.ContainsKey("key25"), Is.True);
        Assert.That(dictionary.Values.Sum(), Is.EqualTo(40425));
#elif MSTEST
        Assert.AreEqual(50, dictionary.Count);
        Assert.AreEqual(100, dictionary["key10"]);
        Assert.IsTrue(dictionary.ContainsKey("key25"));
        Assert.AreEqual(40425, dictionary.Values.Sum());
#endif
    }

    private int CalculateSum(int a, int b) => a + b;
}