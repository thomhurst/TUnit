using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Tests for the custom default value provider feature.
/// </summary>
public class DefaultValueProviderTests
{
    private sealed class CustomProvider : IDefaultValueProvider
    {
        public bool CanProvide(Type type)
        {
            return type == typeof(string) || type == typeof(int);
        }

        public object? GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "custom-default";
            if (type == typeof(int)) return 42;
            return null;
        }
    }

    [Test]
    public async Task Custom_Provider_Returns_String_Default()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.DefaultValueProvider = new CustomProvider();

        IGreeter greeter = mock.Object;

        // Act — no setup, should use custom provider
        var result = greeter.Greet("Alice");

        // Assert
        await Assert.That(result).IsEqualTo("custom-default");
    }

    [Test]
    public async Task Custom_Provider_Returns_Int_Default()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.DefaultValueProvider = new CustomProvider();

        ICalculator calc = mock.Object;

        // Act — no setup, should use custom provider
        var result = calc.Add(1, 2);

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Setup_Takes_Precedence_Over_Provider()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.DefaultValueProvider = new CustomProvider();
        mock.Setup.Add(1, 2).Returns(100);

        ICalculator calc = mock.Object;

        // Act — has setup, should use setup value, not provider
        var result = calc.Add(1, 2);

        // Assert
        await Assert.That(result).IsEqualTo(100);
    }

    [Test]
    public async Task Factory_Method_Creates_Mock_With_Provider()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>(MockBehavior.Loose, new CustomProvider());

        IGreeter greeter = mock.Object;

        // Act
        var result = greeter.Greet("Bob");

        // Assert
        await Assert.That(result).IsEqualTo("custom-default");
    }

    [Test]
    public async Task BuiltIn_Provider_Returns_Empty_String()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.DefaultValueProvider = DefaultValueProvider.Instance;

        IGreeter greeter = mock.Object;

        // Act
        var result = greeter.Greet("Alice");

        // Assert
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task No_Provider_Uses_Smart_Default_For_String()
    {
        // Arrange — no provider set, source generator provides "" as smart default
        var mock = Mock.Of<IGreeter>();

        IGreeter greeter = mock.Object;

        // Act
        var result = greeter.Greet("Alice");

        // Assert — smart default for non-nullable string is ""
        await Assert.That(result).IsEqualTo("");
    }
}
