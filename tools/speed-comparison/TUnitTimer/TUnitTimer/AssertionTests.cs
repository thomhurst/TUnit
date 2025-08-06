using System.Threading.Tasks;

namespace TUnitTimer;

public class AssertionTests
{
    [Test]
    public async Task NumericAssertionsTest()
    {
        var value = 42;
        var pi = 3.14159;
        var negative = -10;

        await Assert.That(value).IsEqualTo(42);
        await Assert.That(value).IsGreaterThan(40);
        await Assert.That(value).IsLessThan(50);
        await Assert.That(value).IsBetween(40, 45).WithInclusiveBounds();

        await Assert.That(pi).IsEqualTo(3.14159);
        await Assert.That(pi).IsGreaterThan(3.0);
        await Assert.That(pi).IsNotEqualTo(3.14);

        await Assert.That(negative).IsNegative();
        await Assert.That(negative).IsLessThan(0);
    }

    [Test]
    public async Task StringAssertionsTest()
    {
        var text = "Hello, TUnit Framework!";
        var empty = "";
        var whitespace = "   ";

        await Assert.That(text).IsNotNull();
        await Assert.That(text).IsNotEmpty();
        await Assert.That(text).Contains("TUnit");
        await Assert.That(text).StartsWith("Hello");
        await Assert.That(text).EndsWith("!");
        await Assert.That(text).HasLength().EqualTo(23);

        await Assert.That(empty).IsEmpty();
        await Assert.That(whitespace).IsNotEmpty();
        await Assert.That(text).DoesNotContain("XUnit");
    }

    [Test]
    public async Task CollectionAssertionsTest()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var empty = new List<int>();
        var duplicates = new[] { 1, 2, 2, 3, 3, 3 };

        await Assert.That(numbers).IsNotNull();
        await Assert.That(numbers).IsNotEmpty();
        await Assert.That(numbers).HasCount(5);
        await Assert.That(numbers).Contains(3);
        await Assert.That(numbers).DoesNotContain(10);

        await Assert.That(empty).IsEmpty();
        await Assert.That(empty).HasCount(0);

        await Assert.That(duplicates).Contains(2);
        await Assert.That(duplicates.Distinct()).HasCount(3);
    }

    [Test]
    public async Task BooleanAssertionsTest()
    {
        var isTrue = true;
        var isFalse = false;
        var condition = 10 > 5;

        await Assert.That(isTrue).IsTrue();
        await Assert.That(isFalse).IsFalse();
        await Assert.That(condition).IsTrue();
        await Assert.That(!condition).IsFalse();

        await Assert.That(string.IsNullOrEmpty("")).IsTrue();
        await Assert.That(string.IsNullOrEmpty("text")).IsFalse();
    }

    [Test]
    public async Task ObjectAssertionsTest()
    {
        var obj1 = new TestObject { Id = 1, Name = "Test" };
        var obj2 = new TestObject { Id = 1, Name = "Test" };
        var obj3 = obj1;
        TestObject? nullObj = null;

        await Assert.That(obj1).IsNotNull();
        await Assert.That(nullObj).IsNull();
        await Assert.That(obj1).IsEqualTo(obj2); // Equals comparison
        await Assert.That(obj1).IsSameReferenceAs(obj3);
        await Assert.That(obj1).IsNotSameReferenceAs(obj2);

        await Assert.That(obj1.GetType()).IsEqualTo(typeof(TestObject));
    }

    [Test]
    public async Task ComplexAssertionsTest()
    {
        var data = GenerateTestData();

        await Assert.That(data).IsNotNull();
        await Assert.That(data.Count).IsGreaterThan(0);

        var firstItem = data.First();
        await Assert.That(firstItem.Id).IsEqualTo(1);
        await Assert.That(firstItem.Values).IsNotEmpty();
        await Assert.That(firstItem.Values.Sum()).IsEqualTo(15);

        var allValid = data.All(x => x.IsValid);
        await Assert.That(allValid).IsTrue();

        var totalSum = data.SelectMany(x => x.Values).Sum();
        await Assert.That(totalSum).IsEqualTo(165);
    }

    [Test]
    [Arguments(new[] { 1, 2, 3 }, 6)]
    [Arguments(new[] { 10, 20, 30 }, 60)]
    [Arguments(new[] { -5, 0, 5 }, 0)]
    public async Task ParameterizedAssertionsTest(int[] values, int expectedSum)
    {
        await Assert.That(values).IsNotNull();
        await Assert.That(values).IsNotEmpty();
        await Assert.That(values.Sum()).IsEqualTo(expectedSum);
        await Assert.That(values.Length).IsGreaterThan(0);
        await Assert.That(values.Average()).IsEqualTo((double) expectedSum / values.Length);
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
