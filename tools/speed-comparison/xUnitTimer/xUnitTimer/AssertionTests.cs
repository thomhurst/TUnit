namespace xUnitTimer;

public class AssertionTests
{
    [Fact]
    public void NumericAssertionsTest()
    {
        var value = 42;
        var pi = 3.14159;
        var negative = -10;
        
        Assert.Equal(42, value);
        Assert.True(value > 40);
        Assert.True(value < 50);
        Assert.InRange(value, 40, 45);
        
        Assert.Equal(3.14159, pi);
        Assert.True(pi > 3.0);
        Assert.NotEqual(3.14, pi);
        
        Assert.True(negative < 0);
        Assert.True(negative < 0);
    }
    
    [Fact]
    public void StringAssertionsTest()
    {
        var text = "Hello, xUnit Framework!";
        var empty = "";
        var whitespace = "   ";
        
        Assert.NotNull(text);
        Assert.NotEmpty(text);
        Assert.Contains("xUnit", text);
        Assert.StartsWith("Hello", text);
        Assert.EndsWith("!", text);
        Assert.Equal(23, text.Length);
        
        Assert.Empty(empty);
        Assert.NotEmpty(whitespace);
        Assert.DoesNotContain("TUnit", text);
    }
    
    [Fact]
    public void CollectionAssertionsTest()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var empty = new List<int>();
        var duplicates = new[] { 1, 2, 2, 3, 3, 3 };
        
        Assert.NotNull(numbers);
        Assert.NotEmpty(numbers);
        Assert.Equal(5, numbers.Count);
        Assert.Contains(3, numbers);
        Assert.DoesNotContain(10, numbers);
        
        Assert.Empty(empty);
        Assert.Equal(0, empty.Count);
        
        Assert.Contains(2, duplicates);
        Assert.Equal(3, duplicates.Distinct().Count());
    }
    
    [Fact]
    public void BooleanAssertionsTest()
    {
        var isTrue = true;
        var isFalse = false;
        var condition = 10 > 5;
        
        Assert.True(isTrue);
        Assert.False(isFalse);
        Assert.True(condition);
        Assert.False(!condition);
        
        Assert.True(string.IsNullOrEmpty(""));
        Assert.False(string.IsNullOrEmpty("text"));
    }
    
    [Fact]
    public void ObjectAssertionsTest()
    {
        var obj1 = new TestObject { Id = 1, Name = "Test" };
        var obj2 = new TestObject { Id = 1, Name = "Test" };
        var obj3 = obj1;
        TestObject? nullObj = null;
        
        Assert.NotNull(obj1);
        Assert.Null(nullObj);
        Assert.Equal(obj1, obj2); // Equals comparison
        Assert.Same(obj3, obj1);
        Assert.NotSame(obj2, obj1);
        
        Assert.Equal(typeof(TestObject), obj1.GetType());
    }
    
    [Fact]
    public void ComplexAssertionsTest()
    {
        var data = GenerateTestData();
        
        Assert.NotNull(data);
        Assert.True(data.Count > 0);
        
        var firstItem = data.First();
        Assert.Equal(1, firstItem.Id);
        Assert.NotEmpty(firstItem.Values);
        Assert.Equal(15, firstItem.Values.Sum());
        
        var allValid = data.All(x => x.IsValid);
        Assert.True(allValid);
        
        var totalSum = data.SelectMany(x => x.Values).Sum();
        Assert.Equal(165, totalSum);
    }
    
    [Theory]
    [InlineData(new[] { 1, 2, 3 }, 6)]
    [InlineData(new[] { 10, 20, 30 }, 60)]
    [InlineData(new[] { -5, 0, 5 }, 0)]
    public void ParameterizedAssertionsTest(int[] values, int expectedSum)
    {
        Assert.NotNull(values);
        Assert.NotEmpty(values);
        Assert.Equal(expectedSum, values.Sum());
        Assert.True(values.Length > 0);
        Assert.Equal((double)expectedSum / values.Length, values.Average());
    }
    
    private List<ComplexTestObject> GenerateTestData()
    {
        return new List<ComplexTestObject>
        {
            new() { Id = 1, Name = "First", Values = new[] { 1, 2, 3, 4, 5 }, IsValid = true },
            new() { Id = 2, Name = "Second", Values = new[] { 6, 7, 8, 9, 10 }, IsValid = true },
            new() { Id = 3, Name = "Third", Values = new[] { 11, 12, 13, 14, 15 }, IsValid = true }
        };
    }
    
    private class TestObject : IEquatable<TestObject>
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        
        public bool Equals(TestObject? other)
        {
            if (other is null) return false;
            return Id == other.Id && Name == other.Name;
        }
        
        public override bool Equals(object? obj) => Equals(obj as TestObject);
        public override int GetHashCode() => HashCode.Combine(Id, Name);
    }
    
    private class ComplexTestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int[] Values { get; set; } = Array.Empty<int>();
        public bool IsValid { get; set; }
    }
}