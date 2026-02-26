using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Interface whose parameter is itself a Func — tests that generated Func overloads
/// don't cause ambiguity with the base Arg&lt;Func&lt;int, bool&gt;&gt; overload.
/// </summary>
public interface IFilterService
{
    int Apply(Func<int, bool> filter);
}

/// <summary>
/// Tests for Func&lt;T, bool&gt; parameter overloads that enable inline lambda syntax.
/// Verifies that mock.Method(x => predicate, Any()) compiles and works correctly.
/// </summary>
public class FuncOverloadTests
{
    [Test]
    public async Task Single_Param_Lambda_Setup_And_Match()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Greet(s => s.StartsWith("Hi")).Returns("matched");

        // Act
        var result = mock.Object.Greet("Hi there");

        // Assert
        await Assert.That(result).IsEqualTo("matched");
    }

    [Test]
    public async Task Single_Param_Lambda_No_Match_Returns_Default()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Greet(s => s.StartsWith("Hi")).Returns("matched");

        // Act
        var result = mock.Object.Greet("Hello");

        // Assert — doesn't match predicate, returns smart default (empty string for non-nullable string)
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task Mixed_Lambda_And_Any()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(x => x > 5, Any()).Returns(100);

        // Act
        var result = mock.Object.Add(10, 999);

        // Assert
        await Assert.That(result).IsEqualTo(100);
    }

    [Test]
    public async Task Mixed_Lambda_And_Any_No_Match()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(x => x > 5, Any()).Returns(100);

        // Act
        var result = mock.Object.Add(3, 999);

        // Assert — first param doesn't match predicate
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Mixed_Any_And_Lambda()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), y => y % 2 == 0).Returns(42);

        // Act
        var even = mock.Object.Add(1, 4);
        var odd = mock.Object.Add(1, 3);

        // Assert
        await Assert.That(even).IsEqualTo(42);
        await Assert.That(odd).IsEqualTo(0);
    }

    [Test]
    public async Task Both_Params_Lambda()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(x => x > 0, y => y > 0).Returns(99);

        // Act
        var bothPositive = mock.Object.Add(1, 2);
        var oneNegative = mock.Object.Add(-1, 2);

        // Assert
        await Assert.That(bothPositive).IsEqualTo(99);
        await Assert.That(oneNegative).IsEqualTo(0);
    }

    [Test]
    public async Task Mixed_Lambda_And_Value()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(x => x > 5, 3).Returns(50);

        // Act
        var matchBoth = mock.Object.Add(10, 3);
        var wrongSecond = mock.Object.Add(10, 4);

        // Assert
        await Assert.That(matchBoth).IsEqualTo(50);
        await Assert.That(wrongSecond).IsEqualTo(0);
    }

    [Test]
    public async Task Lambda_Verification_WasCalled()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        mock.Object.Add(10, 20);

        // Assert — verify with lambda predicates
        mock.Add(x => x > 0, Any()).WasCalled();
    }

    [Test]
    public async Task Lambda_Verification_WasNeverCalled()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        mock.Object.Add(10, 20);

        // Assert — verify negative case
        mock.Add(x => x > 100, Any()).WasNeverCalled();
    }

    [Test]
    public async Task Void_Method_Lambda()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        var logged = false;
        mock.Log(s => s.Contains("error")).Callback(() => logged = true);

        // Act
        mock.Object.Log("an error occurred");

        // Assert
        await Assert.That(logged).IsTrue();
    }

    [Test]
    public async Task Async_Method_Lambda()
    {
        // Arrange
        var mock = Mock.Of<IAsyncService>();
        mock.GetNameAsync(s => s.Length > 3).Returns("found");

        // Act
        var result = await mock.Object.GetNameAsync("hello");

        // Assert
        await Assert.That(result).IsEqualTo("found");
    }

    [Test]
    public async Task String_Predicate_Lambda()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Greet(name => name.Contains("World")).Returns("Hello World!");

        // Act
        var matched = mock.Object.Greet("World");
        var unmatched = mock.Object.Greet("Bob");

        // Assert
        await Assert.That(matched).IsEqualTo("Hello World!");
        await Assert.That(unmatched).IsEqualTo("");
    }

    [Test]
    public async Task Func_Typed_Parameter_No_Ambiguity()
    {
        // Arrange — IFilterService.Apply takes Func<int, bool> as a parameter.
        // The base overload is Arg<Func<int, bool>> and the generated Func overload
        // is Func<Func<int, bool>, bool>. Passing a Func<int, bool> value should
        // target the base overload (implicit T -> Arg<T>), not cause ambiguity.
        var mock = Mock.Of<IFilterService>();
        Func<int, bool> isPositive = x => x > 0;
        mock.Apply(isPositive).Returns(42);

        // Act
        var result = mock.Object.Apply(isPositive);

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Multiple_Lambda_Setups_Last_Wins()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(x => x > 0, Any()).Returns(50);
        mock.Add(x => x > 10, Any()).Returns(100);

        // Act & Assert — value matching only the first setup returns 50
        await Assert.That(mock.Object.Add(5, 0)).IsEqualTo(50);

        // Act & Assert — value matching both setups returns 100 (last-registered wins)
        await Assert.That(mock.Object.Add(15, 0)).IsEqualTo(100);
    }
}
