namespace TUnitTimer;

public class BasicTests
{
    [Test]
    public void SimpleTest()
    {
        var result = CalculateSum(5, 10);
        Assert.That(result).IsEqualTo(15);
    }

    [Test]
    public void MultipleAssertionsTest()
    {
        var text = "Hello, World!";
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.That(text).IsNotNull();
        Assert.That(text.Length).IsEqualTo(13);
        Assert.That(text).Contains("World");
        
        Assert.That(numbers).HasCount(5);
        Assert.That(numbers.Sum()).IsEqualTo(15);
        Assert.That(numbers).Contains(3);
    }

    [Test]
    public void CollectionOperationsTest()
    {
        var items = Enumerable.Range(1, 100).ToList();
        var filtered = items.Where(x => x % 2 == 0).ToList();
        var sum = filtered.Sum();
        
        Assert.That(filtered).HasCount(50);
        Assert.That(sum).IsEqualTo(2550);
        Assert.That(filtered.First()).IsEqualTo(2);
        Assert.That(filtered.Last()).IsEqualTo(100);
    }

    [Test]
    public void StringManipulationTest()
    {
        var input = "  Hello, TUnit Testing Framework!  ";
        var trimmed = input.Trim();
        var upper = trimmed.ToUpper();
        var words = trimmed.Split(' ');
        
        Assert.That(trimmed).IsEqualTo("Hello, TUnit Testing Framework!");
        Assert.That(upper).IsEqualTo("HELLO, TUNIT TESTING FRAMEWORK!");
        Assert.That(words).HasCount(4);
        Assert.That(words[1]).IsEqualTo("TUnit");
    }

    [Test]
    public void DictionaryOperationsTest()
    {
        var dictionary = new Dictionary<string, int>();
        for (int i = 0; i < 50; i++)
        {
            dictionary[$"key{i}"] = i * i;
        }
        
        Assert.That(dictionary).HasCount(50);
        Assert.That(dictionary["key10"]).IsEqualTo(100);
        Assert.That(dictionary.ContainsKey("key25")).IsTrue();
        Assert.That(dictionary.Values.Sum()).IsEqualTo(40425);
    }

    private int CalculateSum(int a, int b) => a + b;
}