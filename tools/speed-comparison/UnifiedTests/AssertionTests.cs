using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class AssertionTests
{
#if TUNIT
    [Test]
    public async Task NumericAssertionsTest()
#elif XUNIT
    [Fact]
    public void NumericAssertionsTest()
#elif NUNIT
    [Test]
    public void NumericAssertionsTest()
#elif MSTEST
    [TestMethod]
    public void NumericAssertionsTest()
#endif
    {
        var value = 42;
        var pi = 3.14159;
        var negative = -10;

#if TUNIT
        await Assert.That(value).IsEqualTo(42);
        await Assert.That(value).IsGreaterThan(40);
        await Assert.That(value).IsLessThan(50);
        await Assert.That(value).IsBetween(40, 45).WithInclusiveBounds();

        await Assert.That(pi).IsEqualTo(3.14159);
        await Assert.That(pi).IsGreaterThan(3.0);
        await Assert.That(pi).IsNotEqualTo(3.14);

        await Assert.That(negative).IsNegative();
        await Assert.That(negative).IsLessThan(0);
#elif XUNIT
        Assert.Equal(42, value);
        Assert.True(value > 40);
        Assert.True(value < 50);
        Assert.InRange(value, 40, 45);

        Assert.Equal(3.14159, pi);
        Assert.True(pi > 3.0);
        Assert.NotEqual(3.14, pi);

        Assert.True(negative < 0);
#elif NUNIT
        Assert.That(value, Is.EqualTo(42));
        Assert.That(value, Is.GreaterThan(40));
        Assert.That(value, Is.LessThan(50));
        Assert.That(value, Is.InRange(40, 45));

        Assert.That(pi, Is.EqualTo(3.14159));
        Assert.That(pi, Is.GreaterThan(3.0));
        Assert.That(pi, Is.Not.EqualTo(3.14));

        Assert.That(negative, Is.Negative);
        Assert.That(negative, Is.LessThan(0));
#elif MSTEST
        Assert.AreEqual(42, value);
        Assert.IsTrue(value > 40);
        Assert.IsTrue(value < 50);
        Assert.IsTrue(value >= 40 && value <= 45);

        Assert.AreEqual(3.14159, pi);
        Assert.IsTrue(pi > 3.0);
        Assert.AreNotEqual(3.14, pi);

        Assert.IsTrue(negative < 0);
#endif
    }

#if TUNIT
    [Test]
    public async Task StringAssertionsTest()
#elif XUNIT
    [Fact]
    public void StringAssertionsTest()
#elif NUNIT
    [Test]
    public void StringAssertionsTest()
#elif MSTEST
    [TestMethod]
    public void StringAssertionsTest()
#endif
    {
#if TUNIT
        var text = "Hello, TUnit Framework!";
#elif XUNIT
        var text = "Hello, xUnit Framework!";
#elif NUNIT
        var text = "Hello, NUnit Framework!";
#elif MSTEST
        var text = "Hello, MSTest Framework!";
#endif
        var empty = "";
        var whitespace = "   ";

#if TUNIT
        await Assert.That(text).IsNotNull();
        await Assert.That(text).IsNotEmpty();
        await Assert.That(text).Contains("TUnit");
        await Assert.That(text).StartsWith("Hello");
        await Assert.That(text).EndsWith("!");
        await Assert.That(text).HasLength().EqualTo(23);

        await Assert.That(empty).IsEmpty();
        await Assert.That(whitespace).IsNotEmpty();
        await Assert.That(text).DoesNotContain("XUnit");
#elif XUNIT
        Assert.NotNull(text);
        Assert.NotEmpty(text);
        Assert.Contains("xUnit", text);
        Assert.StartsWith("Hello", text);
        Assert.EndsWith("!", text);
        Assert.Equal(23, text.Length);

        Assert.Empty(empty);
        Assert.NotEmpty(whitespace);
        Assert.DoesNotContain("TUnit", text);
#elif NUNIT
        Assert.That(text, Is.Not.Null);
        Assert.That(text, Is.Not.Empty);
        Assert.That(text, Does.Contain("NUnit"));
        Assert.That(text, Does.StartWith("Hello"));
        Assert.That(text, Does.EndWith("!"));
        Assert.That(text.Length, Is.EqualTo(23));

        Assert.That(empty, Is.Empty);
        Assert.That(whitespace, Is.Not.Empty);
        Assert.That(text, Does.Not.Contain("XUnit"));
#elif MSTEST
        Assert.IsNotNull(text);
        Assert.IsTrue(!string.IsNullOrEmpty(text));
        Assert.IsTrue(text.Contains("MSTest"));
        Assert.IsTrue(text.StartsWith("Hello"));
        Assert.IsTrue(text.EndsWith("!"));
        Assert.AreEqual(24, text.Length);

        Assert.AreEqual(string.Empty, empty);
        Assert.IsTrue(!string.IsNullOrEmpty(whitespace));
        Assert.IsFalse(text.Contains("XUnit"));
#endif
    }

#if TUNIT
    [Test]
    public async Task CollectionAssertionsTest()
#elif XUNIT
    [Fact]
    public void CollectionAssertionsTest()
#elif NUNIT
    [Test]
    public void CollectionAssertionsTest()
#elif MSTEST
    [TestMethod]
    public void CollectionAssertionsTest()
#endif
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var empty = new List<int>();
        var duplicates = new[] { 1, 2, 2, 3, 3, 3 };

#if TUNIT
        await Assert.That(numbers).IsNotNull();
        await Assert.That(numbers).IsNotEmpty();
        await Assert.That(numbers).HasCount(5);
        await Assert.That(numbers).Contains(3);
        await Assert.That(numbers).DoesNotContain(10);

        await Assert.That(empty).IsEmpty();
        await Assert.That(empty).HasCount(0);

        await Assert.That(duplicates).HasCount(6);
        await Assert.That(duplicates).Contains(2);
#elif XUNIT
        Assert.NotNull(numbers);
        Assert.NotEmpty(numbers);
        Assert.Equal(5, numbers.Count);
        Assert.Contains(3, numbers);
        Assert.DoesNotContain(10, numbers);

        Assert.Empty(empty);
        Assert.Equal(0, empty.Count);

        Assert.Equal(6, duplicates.Length);
        Assert.Contains(2, duplicates);
#elif NUNIT
        Assert.That(numbers, Is.Not.Null);
        Assert.That(numbers, Is.Not.Empty);
        Assert.That(numbers.Count, Is.EqualTo(5));
        Assert.That(numbers, Does.Contain(3));
        Assert.That(numbers, Does.Not.Contain(10));

        Assert.That(empty, Is.Empty);
        Assert.That(empty.Count, Is.EqualTo(0));

        Assert.That(duplicates.Length, Is.EqualTo(6));
        Assert.That(duplicates, Does.Contain(2));
#elif MSTEST
        Assert.IsNotNull(numbers);
        Assert.IsTrue(numbers.Count > 0);
        Assert.AreEqual(5, numbers.Count);
        CollectionAssert.Contains(numbers, 3);
        CollectionAssert.DoesNotContain(numbers, 10);

        Assert.AreEqual(0, empty.Count);

        Assert.AreEqual(6, duplicates.Length);
        CollectionAssert.Contains(duplicates, 2);
#endif
    }

#if TUNIT
    [Test]
    public async Task BooleanAssertionsTest()
#elif XUNIT
    [Fact]
    public void BooleanAssertionsTest()
#elif NUNIT
    [Test]
    public void BooleanAssertionsTest()
#elif MSTEST
    [TestMethod]
    public void BooleanAssertionsTest()
#endif
    {
        var isTrue = true;
        var isFalse = false;
        var condition = 10 > 5;

#if TUNIT
        await Assert.That(isTrue).IsTrue();
        await Assert.That(isFalse).IsFalse();
        await Assert.That(condition).IsTrue();
        await Assert.That(!condition).IsFalse();
#elif XUNIT
        Assert.True(isTrue);
        Assert.False(isFalse);
        Assert.True(condition);
        Assert.False(!condition);
#elif NUNIT
        Assert.That(isTrue, Is.True);
        Assert.That(isFalse, Is.False);
        Assert.That(condition, Is.True);
        Assert.That(!condition, Is.False);
#elif MSTEST
        Assert.IsTrue(isTrue);
        Assert.IsFalse(isFalse);
        Assert.IsTrue(condition);
        Assert.IsFalse(!condition);
#endif
    }

#if TUNIT
    [Test]
    public async Task ExceptionAssertionsTest()
#elif XUNIT
    [Fact]
    public void ExceptionAssertionsTest()
#elif NUNIT
    [Test]
    public void ExceptionAssertionsTest()
#elif MSTEST
    [TestMethod]
    public void ExceptionAssertionsTest()
#endif
    {
        Func<int> throwingFunc = () => throw new InvalidOperationException("Test exception");
        Func<int> nonThrowingFunc = () => 42;

#if TUNIT
        await Assert.That(throwingFunc).Throws<InvalidOperationException>();
        // TUnit doesn't have DoesNotThrow, just call the function
        var result = nonThrowingFunc();
        await Assert.That(result).IsEqualTo(42);
#elif XUNIT
        Assert.Throws<InvalidOperationException>(() => throwingFunc());
        var result = Record.Exception(() => nonThrowingFunc());
        Assert.Null(result);
#elif NUNIT
        Assert.That(throwingFunc, Throws.TypeOf<InvalidOperationException>());
        Assert.That(nonThrowingFunc, Throws.Nothing);
#elif MSTEST
        Assert.ThrowsException<InvalidOperationException>(() => throwingFunc());
        try
        {
            nonThrowingFunc();
        }
        catch
        {
            Assert.Fail("Should not throw exception");
        }
#endif
    }
}