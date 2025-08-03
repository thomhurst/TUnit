namespace MSTestTimer;

[TestClass]
public class BasicTests
{
    [TestMethod]
    public void SimpleTest()
    {
        var result = CalculateSum(5, 10);
        Assert.AreEqual(15, result);
    }

    [TestMethod]
    public void MultipleAssertionsTest()
    {
        var text = "Hello, World!";
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.IsNotNull(text);
        Assert.AreEqual(13, text.Length);
        Assert.IsTrue(text.Contains("World"));
        
        Assert.AreEqual(5, numbers.Length);
        Assert.AreEqual(15, numbers.Sum());
        CollectionAssert.Contains(numbers, 3);
    }

    [TestMethod]
    public void CollectionOperationsTest()
    {
        var items = Enumerable.Range(1, 100).ToList();
        var filtered = items.Where(x => x % 2 == 0).ToList();
        var sum = filtered.Sum();
        
        Assert.AreEqual(50, filtered.Count);
        Assert.AreEqual(2550, sum);
        Assert.AreEqual(2, filtered.First());
        Assert.AreEqual(100, filtered.Last());
    }

    [TestMethod]
    public void StringManipulationTest()
    {
        var input = "  Hello, MSTest Testing Framework!  ";
        var trimmed = input.Trim();
        var upper = trimmed.ToUpper();
        var words = trimmed.Split(' ');
        
        Assert.AreEqual("Hello, MSTest Testing Framework!", trimmed);
        Assert.AreEqual("HELLO, MSTEST TESTING FRAMEWORK!", upper);
        Assert.AreEqual(4, words.Length);
        Assert.AreEqual("MSTest", words[1]);
    }

    [TestMethod]
    public void DictionaryOperationsTest()
    {
        var dictionary = new Dictionary<string, int>();
        for (int i = 0; i < 50; i++)
        {
            dictionary[$"key{i}"] = i * i;
        }
        
        Assert.AreEqual(50, dictionary.Count);
        Assert.AreEqual(100, dictionary["key10"]);
        Assert.IsTrue(dictionary.ContainsKey("key25"));
        Assert.AreEqual(40425, dictionary.Values.Sum());
    }

    private int CalculateSum(int a, int b) => a + b;
}