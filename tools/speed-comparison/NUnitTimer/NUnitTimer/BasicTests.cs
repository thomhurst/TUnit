namespace NUnitTimer;

[TestFixture]
public class BasicTests
{
    [Test]
    public void SimpleTest()
    {
        var result = CalculateSum(5, 10);
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void MultipleAssertionsTest()
    {
        var text = "Hello, World!";
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.That(text, Is.Not.Null);
        Assert.That(text.Length, Is.EqualTo(13));
        Assert.That(text, Does.Contain("World"));
        
        Assert.That(numbers.Length, Is.EqualTo(5));
        Assert.That(numbers.Sum(), Is.EqualTo(15));
        Assert.That(numbers, Does.Contain(3));
    }

    [Test]
    public void CollectionOperationsTest()
    {
        var items = Enumerable.Range(1, 100).ToList();
        var filtered = items.Where(x => x % 2 == 0).ToList();
        var sum = filtered.Sum();
        
        Assert.That(filtered.Count, Is.EqualTo(50));
        Assert.That(sum, Is.EqualTo(2550));
        Assert.That(filtered.First(), Is.EqualTo(2));
        Assert.That(filtered.Last(), Is.EqualTo(100));
    }

    [Test]
    public void StringManipulationTest()
    {
        var input = "  Hello, NUnit Testing Framework!  ";
        var trimmed = input.Trim();
        var upper = trimmed.ToUpper();
        var words = trimmed.Split(' ');
        
        Assert.That(trimmed, Is.EqualTo("Hello, NUnit Testing Framework!"));
        Assert.That(upper, Is.EqualTo("HELLO, NUNIT TESTING FRAMEWORK!"));
        Assert.That(words.Length, Is.EqualTo(4));
        Assert.That(words[1], Is.EqualTo("NUnit"));
    }

    [Test]
    public void DictionaryOperationsTest()
    {
        var dictionary = new Dictionary<string, int>();
        for (int i = 0; i < 50; i++)
        {
            dictionary[$"key{i}"] = i * i;
        }
        
        Assert.That(dictionary.Count, Is.EqualTo(50));
        Assert.That(dictionary["key10"], Is.EqualTo(100));
        Assert.That(dictionary.ContainsKey("key25"), Is.True);
        Assert.That(dictionary.Values.Sum(), Is.EqualTo(40425));
    }

    private int CalculateSum(int a, int b) => a + b;
}