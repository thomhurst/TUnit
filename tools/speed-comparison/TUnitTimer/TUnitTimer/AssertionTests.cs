namespace TUnitTimer;

public class AssertionTests
{
    [Test]
    public void NumericAssertionsTest()
    {
        var value = 42;
        var pi = 3.14159;
        var negative = -10;
        
        Assert.That(value).IsEqualTo(42);
        Assert.That(value).IsGreaterThan(40);
        Assert.That(value).IsLessThan(50);
        Assert.That(value).IsInRange(40, 45);
        
        Assert.That(pi).IsEqualTo(3.14159);
        Assert.That(pi).IsGreaterThan(3.0);
        Assert.That(pi).IsNotEqualTo(3.14);
        
        Assert.That(negative).IsNegative();
        Assert.That(negative).IsLessThan(0);
    }
    
    [Test]
    public void StringAssertionsTest()
    {
        var text = "Hello, TUnit Framework!";
        var empty = "";
        var whitespace = "   ";
        
        Assert.That(text).IsNotNull();
        Assert.That(text).IsNotEmpty();
        Assert.That(text).Contains("TUnit");
        Assert.That(text).StartsWith("Hello");
        Assert.That(text).EndsWith("!");
        Assert.That(text).HasLength(23);
        
        Assert.That(empty).IsEmpty();
        Assert.That(whitespace).IsNotEmpty();
        Assert.That(text).DoesNotContain("XUnit");
    }
    
    [Test]
    public void CollectionAssertionsTest()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var empty = new List<int>();
        var duplicates = new[] { 1, 2, 2, 3, 3, 3 };
        
        Assert.That(numbers).IsNotNull();
        Assert.That(numbers).IsNotEmpty();
        Assert.That(numbers).HasCount(5);
        Assert.That(numbers).Contains(3);
        Assert.That(numbers).DoesNotContain(10);
        
        Assert.That(empty).IsEmpty();
        Assert.That(empty).HasCount(0);
        
        Assert.That(duplicates).Contains(2);
        Assert.That(duplicates.Distinct()).HasCount(3);
    }
    
    [Test]
    public void BooleanAssertionsTest()
    {
        var isTrue = true;
        var isFalse = false;
        var condition = 10 > 5;
        
        Assert.That(isTrue).IsTrue();
        Assert.That(isFalse).IsFalse();
        Assert.That(condition).IsTrue();
        Assert.That(!condition).IsFalse();
        
        Assert.That(string.IsNullOrEmpty("")).IsTrue();
        Assert.That(string.IsNullOrEmpty("text")).IsFalse();
    }
    
    [Test]
    public void ObjectAssertionsTest()
    {
        var obj1 = new TestObject { Id = 1, Name = "Test" };
        var obj2 = new TestObject { Id = 1, Name = "Test" };
        var obj3 = obj1;
        TestObject? nullObj = null;
        
        Assert.That(obj1).IsNotNull();
        Assert.That(nullObj).IsNull();
        Assert.That(obj1).IsEqualTo(obj2); // Equals comparison
        Assert.That(obj1).IsSameReferenceAs(obj3);
        Assert.That(obj1).IsNotSameReferenceAs(obj2);
        
        Assert.That(obj1.GetType()).IsEqualTo(typeof(TestObject));
    }
    
    [Test]
    public void ComplexAssertionsTest()
    {
        var data = GenerateTestData();
        
        Assert.That(data).IsNotNull();
        Assert.That(data.Count).IsGreaterThan(0);
        
        var firstItem = data.First();
        Assert.That(firstItem.Id).IsEqualTo(1);
        Assert.That(firstItem.Values).IsNotEmpty();
        Assert.That(firstItem.Values.Sum()).IsEqualTo(15);
        
        var allValid = data.All(x => x.IsValid);
        Assert.That(allValid).IsTrue();
        
        var totalSum = data.SelectMany(x => x.Values).Sum();
        Assert.That(totalSum).IsEqualTo(165);
    }
    
    [Test]
    [Arguments(new[] { 1, 2, 3 }, 6)]
    [Arguments(new[] { 10, 20, 30 }, 60)]
    [Arguments(new[] { -5, 0, 5 }, 0)]
    public void ParameterizedAssertionsTest(int[] values, int expectedSum)
    {
        Assert.That(values).IsNotNull();
        Assert.That(values).IsNotEmpty();
        Assert.That(values.Sum()).IsEqualTo(expectedSum);
        Assert.That(values.Length).IsGreaterThan(0);
        Assert.That(values.Average()).IsEqualTo((double)expectedSum / values.Length);
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