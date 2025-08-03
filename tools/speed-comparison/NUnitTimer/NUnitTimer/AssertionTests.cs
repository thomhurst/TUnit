namespace NUnitTimer;

[TestFixture]
public class AssertionTests
{
    [Test]
    public void NumericAssertionsTest()
    {
        var value = 42;
        var pi = 3.14159;
        var negative = -10;
        
        Assert.That(value, Is.EqualTo(42));
        Assert.That(value, Is.GreaterThan(40));
        Assert.That(value, Is.LessThan(50));
        Assert.That(value, Is.InRange(40, 45));
        
        Assert.That(pi, Is.EqualTo(3.14159));
        Assert.That(pi, Is.GreaterThan(3.0));
        Assert.That(pi, Is.Not.EqualTo(3.14));
        
        Assert.That(negative, Is.Negative);
        Assert.That(negative, Is.LessThan(0));
    }
    
    [Test]
    public void StringAssertionsTest()
    {
        var text = "Hello, NUnit Framework!";
        var empty = "";
        var whitespace = "   ";
        
        Assert.That(text, Is.Not.Null);
        Assert.That(text, Is.Not.Empty);
        Assert.That(text, Does.Contain("NUnit"));
        Assert.That(text, Does.StartWith("Hello"));
        Assert.That(text, Does.EndWith("!"));
        Assert.That(text.Length, Is.EqualTo(23));
        
        Assert.That(empty, Is.Empty);
        Assert.That(whitespace, Is.Not.Empty);
        Assert.That(text, Does.Not.Contain("xUnit"));
    }
    
    [Test]
    public void CollectionAssertionsTest()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var empty = new List<int>();
        var duplicates = new[] { 1, 2, 2, 3, 3, 3 };
        
        Assert.That(numbers, Is.Not.Null);
        Assert.That(numbers, Is.Not.Empty);
        Assert.That(numbers, Has.Count.EqualTo(5));
        Assert.That(numbers, Does.Contain(3));
        Assert.That(numbers, Does.Not.Contain(10));
        
        Assert.That(empty, Is.Empty);
        Assert.That(empty, Has.Count.EqualTo(0));
        
        Assert.That(duplicates, Does.Contain(2));
        Assert.That(duplicates.Distinct(), Has.Count.EqualTo(3));
    }
    
    [Test]
    public void BooleanAssertionsTest()
    {
        var isTrue = true;
        var isFalse = false;
        var condition = 10 > 5;
        
        Assert.That(isTrue, Is.True);
        Assert.That(isFalse, Is.False);
        Assert.That(condition, Is.True);
        Assert.That(!condition, Is.False);
        
        Assert.That(string.IsNullOrEmpty(""), Is.True);
        Assert.That(string.IsNullOrEmpty("text"), Is.False);
    }
    
    [Test]
    public void ObjectAssertionsTest()
    {
        var obj1 = new TestObject { Id = 1, Name = "Test" };
        var obj2 = new TestObject { Id = 1, Name = "Test" };
        var obj3 = obj1;
        TestObject? nullObj = null;
        
        Assert.That(obj1, Is.Not.Null);
        Assert.That(nullObj, Is.Null);
        Assert.That(obj1, Is.EqualTo(obj2)); // Equals comparison
        Assert.That(obj1, Is.SameAs(obj3));
        Assert.That(obj1, Is.Not.SameAs(obj2));
        
        Assert.That(obj1.GetType(), Is.EqualTo(typeof(TestObject)));
    }
    
    [Test]
    public void ComplexAssertionsTest()
    {
        var data = GenerateTestData();
        
        Assert.That(data, Is.Not.Null);
        Assert.That(data.Count, Is.GreaterThan(0));
        
        var firstItem = data.First();
        Assert.That(firstItem.Id, Is.EqualTo(1));
        Assert.That(firstItem.Values, Is.Not.Empty);
        Assert.That(firstItem.Values.Sum(), Is.EqualTo(15));
        
        var allValid = data.All(x => x.IsValid);
        Assert.That(allValid, Is.True);
        
        var totalSum = data.SelectMany(x => x.Values).Sum();
        Assert.That(totalSum, Is.EqualTo(165));
    }
    
    [TestCase(new[] { 1, 2, 3 }, 6)]
    [TestCase(new[] { 10, 20, 30 }, 60)]
    [TestCase(new[] { -5, 0, 5 }, 0)]
    public void ParameterizedAssertionsTest(int[] values, int expectedSum)
    {
        Assert.That(values, Is.Not.Null);
        Assert.That(values, Is.Not.Empty);
        Assert.That(values.Sum(), Is.EqualTo(expectedSum));
        Assert.That(values.Length, Is.GreaterThan(0));
        Assert.That(values.Average(), Is.EqualTo((double)expectedSum / values.Length));
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