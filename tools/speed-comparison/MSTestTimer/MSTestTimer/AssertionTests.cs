namespace MSTestTimer;

[TestClass]
public class AssertionTests
{
    [TestMethod]
    public void NumericAssertionsTest()
    {
        var value = 42;
        var pi = 3.14159;
        var negative = -10;
        
        Assert.AreEqual(42, value);
        Assert.IsTrue(value > 40);
        Assert.IsTrue(value < 50);
        Assert.IsTrue(value >= 40 && value <= 45);
        
        Assert.AreEqual(3.14159, pi);
        Assert.IsTrue(pi > 3.0);
        Assert.AreNotEqual(3.14, pi);
        
        Assert.IsTrue(negative < 0);
        Assert.IsTrue(negative < 0);
    }
    
    [TestMethod]
    public void StringAssertionsTest()
    {
        var text = "Hello, MSTest Framework!";
        var empty = "";
        var whitespace = "   ";
        
        Assert.IsNotNull(text);
        Assert.IsTrue(text.Length > 0);
        Assert.IsTrue(text.Contains("MSTest"));
        Assert.IsTrue(text.StartsWith("Hello"));
        Assert.IsTrue(text.EndsWith("!"));
        Assert.AreEqual(24, text.Length);
        
        Assert.AreEqual(0, empty.Length);
        Assert.IsTrue(whitespace.Length > 0);
        Assert.IsFalse(text.Contains("xUnit"));
    }
    
    [TestMethod]
    public void CollectionAssertionsTest()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var empty = new List<int>();
        var duplicates = new[] { 1, 2, 2, 3, 3, 3 };
        
        Assert.IsNotNull(numbers);
        Assert.IsTrue(numbers.Count > 0);
        Assert.AreEqual(5, numbers.Count);
        CollectionAssert.Contains(numbers, 3);
        CollectionAssert.DoesNotContain(numbers, 10);
        
        Assert.AreEqual(0, empty.Count);
        
        CollectionAssert.Contains(duplicates, 2);
        Assert.AreEqual(3, duplicates.Distinct().Count());
    }
    
    [TestMethod]
    public void BooleanAssertionsTest()
    {
        var isTrue = true;
        var isFalse = false;
        var condition = 10 > 5;
        
        Assert.IsTrue(isTrue);
        Assert.IsFalse(isFalse);
        Assert.IsTrue(condition);
        Assert.IsFalse(!condition);
        
        Assert.IsTrue(string.IsNullOrEmpty(""));
        Assert.IsFalse(string.IsNullOrEmpty("text"));
    }
    
    [TestMethod]
    public void ObjectAssertionsTest()
    {
        var obj1 = new TestObject { Id = 1, Name = "Test" };
        var obj2 = new TestObject { Id = 1, Name = "Test" };
        var obj3 = obj1;
        TestObject? nullObj = null;
        
        Assert.IsNotNull(obj1);
        Assert.IsNull(nullObj);
        Assert.AreEqual(obj1, obj2); // Equals comparison
        Assert.AreSame(obj3, obj1);
        Assert.AreNotSame(obj2, obj1);
        
        Assert.AreEqual(typeof(TestObject), obj1.GetType());
    }
    
    [TestMethod]
    public void ComplexAssertionsTest()
    {
        var data = GenerateTestData();
        
        Assert.IsNotNull(data);
        Assert.IsTrue(data.Count > 0);
        
        var firstItem = data.First();
        Assert.AreEqual(1, firstItem.Id);
        Assert.IsTrue(firstItem.Values.Length > 0);
        Assert.AreEqual(15, firstItem.Values.Sum());
        
        var allValid = data.All(x => x.IsValid);
        Assert.IsTrue(allValid);
        
        var totalSum = data.SelectMany(x => x.Values).Sum();
        Assert.AreEqual(165, totalSum);
    }
    
    [DataTestMethod]
    [DataRow(new[] { 1, 2, 3 }, 6)]
    [DataRow(new[] { 10, 20, 30 }, 60)]
    [DataRow(new[] { -5, 0, 5 }, 0)]
    public void ParameterizedAssertionsTest(int[] values, int expectedSum)
    {
        Assert.IsNotNull(values);
        Assert.IsTrue(values.Length > 0);
        Assert.AreEqual(expectedSum, values.Sum());
        Assert.IsTrue(values.Length > 0);
        Assert.AreEqual((double)expectedSum / values.Length, values.Average());
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