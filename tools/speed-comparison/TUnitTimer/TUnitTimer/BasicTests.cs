using System.Threading.Tasks;

namespace TUnitTimer;

public class BasicTests
{
    [Test]
    public async Task SimpleTest()
    {
        var result = CalculateSum(5, 10);
        await Assert.That(result).IsEqualTo(15);
    }

    [Test]
    public async Task MultipleAssertionsTest()
    {
        var text = "Hello, World!";
        var numbers = new[] { 1, 2, 3, 4, 5 };

        await Assert.That(text).IsNotNull();
        await Assert.That(text.Length).IsEqualTo(13);
        await Assert.That(text).Contains("World");

        await Assert.That(numbers).HasCount(5);
        await Assert.That(numbers.Sum()).IsEqualTo(15);
        await Assert.That(numbers).Contains(3);
    }

    [Test]
    public async Task CollectionOperationsTest()
    {
        var items = Enumerable.Range(1, 100).ToList();
        var filtered = items.Where(x => x % 2 == 0).ToList();
        var sum = filtered.Sum();

        await Assert.That(filtered).HasCount(50);
        await Assert.That(sum).IsEqualTo(2550);
        await Assert.That(filtered.First()).IsEqualTo(2);
        await Assert.That(filtered.Last()).IsEqualTo(100);
    }

    [Test]
    public async Task StringManipulationTest()
    {
        var input = "  Hello, TUnit Testing Framework!  ";
        var trimmed = input.Trim();
        var upper = trimmed.ToUpper();
        var words = trimmed.Split(' ');

        await Assert.That(trimmed).IsEqualTo("Hello, TUnit Testing Framework!");
        await Assert.That(upper).IsEqualTo("HELLO, TUNIT TESTING FRAMEWORK!");
        await Assert.That(words).HasCount(4);
        await Assert.That(words[1]).IsEqualTo("TUnit");
    }

    [Test]
    public async Task DictionaryOperationsTest()
    {
        var dictionary = new Dictionary<string, int>();
        for (int i = 0; i < 50; i++)
        {
            dictionary[$"key{i}"] = i * i;
        }

        await Assert.That(dictionary).HasCount(50);
        await Assert.That(dictionary["key10"]).IsEqualTo(100);
        await Assert.That(dictionary.ContainsKey("key25")).IsTrue();
        await Assert.That(dictionary.Values.Sum()).IsEqualTo(40425);
    }

    private int CalculateSum(int a, int b) => a + b;
}
