namespace xUnitTimerV3;

public class BasicTests
{
    [Fact]
    public void SimpleTest()
    {
        var result = CalculateSum(5, 10);
        Assert.Equal(15, result);
    }

    [Fact]
    public void MultipleAssertionsTest()
    {
        var text = "Hello, World!";
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.NotNull(text);
        Assert.Equal(13, text.Length);
        Assert.Contains("World", text);
        
        Assert.Equal(5, numbers.Length);
        Assert.Equal(15, numbers.Sum());
        Assert.Contains(3, numbers);
    }

    [Fact]
    public void CollectionOperationsTest()
    {
        var items = Enumerable.Range(1, 100).ToList();
        var filtered = items.Where(x => x % 2 == 0).ToList();
        var sum = filtered.Sum();
        
        Assert.Equal(50, filtered.Count);
        Assert.Equal(2550, sum);
        Assert.Equal(2, filtered.First());
        Assert.Equal(100, filtered.Last());
    }

    [Fact]
    public void StringManipulationTest()
    {
        var input = "  Hello, xUnit Testing Framework!  ";
        var trimmed = input.Trim();
        var upper = trimmed.ToUpper();
        var words = trimmed.Split(' ');
        
        Assert.Equal("Hello, xUnit Testing Framework!", trimmed);
        Assert.Equal("HELLO, XUNIT TESTING FRAMEWORK!", upper);
        Assert.Equal(4, words.Length);
        Assert.Equal("xUnit", words[1]);
    }

    [Fact]
    public void DictionaryOperationsTest()
    {
        var dictionary = new Dictionary<string, int>();
        for (int i = 0; i < 50; i++)
        {
            dictionary[$"key{i}"] = i * i;
        }
        
        Assert.Equal(50, dictionary.Count);
        Assert.Equal(100, dictionary["key10"]);
        Assert.True(dictionary.ContainsKey("key25"));
        Assert.Equal(40425, dictionary.Values.Sum());
    }

    private int CalculateSum(int a, int b) => a + b;
}