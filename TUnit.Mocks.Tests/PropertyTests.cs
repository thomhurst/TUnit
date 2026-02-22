using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Setup;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Test interface with properties for mocking.
/// </summary>
public interface IPropertyService
{
    /// <summary>Getter-only property.</summary>
    string Name { get; }

    /// <summary>Getter and setter property.</summary>
    int Count { get; set; }
}

/// <summary>
/// US5 Integration Tests: Property setup and verification.
/// </summary>
public class PropertyTests
{
    [Test]
    public async Task Getter_Returns_Default_When_Not_Configured()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act
        IPropertyService svc = mock.Object;
        var name = svc.Name;

        // Assert — smart default for non-nullable string is ""
        await Assert.That(name).IsNotNull();
    }

    [Test]
    public async Task Getter_Returns_Configured_Value()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();
        mock.Setup.Name.Returns("TestName");

        // Act
        IPropertyService svc = mock.Object;
        var name = svc.Name;

        // Assert
        await Assert.That(name).IsEqualTo("TestName");
    }

    [Test]
    public async Task Getter_Returns_Configured_Int_Value()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();
        mock.Setup.Count.Returns(42);

        // Act
        IPropertyService svc = mock.Object;
        var count = svc.Count;

        // Assert
        await Assert.That(count).IsEqualTo(42);
    }

    [Test]
    public async Task Setter_Records_Call()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act
        IPropertyService svc = mock.Object;
        svc.Count = 10;

        // Assert — verify setter was called
        mock.Verify.Count.Set(10).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Setter_Verify_With_Any_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act
        IPropertyService svc = mock.Object;
        svc.Count = 5;
        svc.Count = 10;
        svc.Count = 15;

        // Assert — verify setter was called 3 times with any value
        mock.Verify.Count.Setter.WasCalled(Times.Exactly(3));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Setter_Verify_With_Specific_Value()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act
        IPropertyService svc = mock.Object;
        svc.Count = 5;
        svc.Count = 10;

        // Assert — only one call with value 10
        mock.Verify.Count.Set(10).WasCalled(Times.Once);
        mock.Verify.Count.Set(5).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Setter_Verify_Never_Called()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act — don't set anything

        // Assert
        mock.Verify.Count.Setter.WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Getter_Verify_Was_Called()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act
        IPropertyService svc = mock.Object;
        _ = svc.Name;
        _ = svc.Name;

        // Assert — getter was called twice
        mock.Verify.Name.WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Getter_Verify_Never_Called()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act — don't access the property

        // Assert
        mock.Verify.Name.WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Getter_Returns_Default_Int_When_Not_Configured()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();

        // Act
        IPropertyService svc = mock.Object;
        var count = svc.Count;

        // Assert — default for int is 0
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Multiple_Getter_Setups_Last_Wins()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();
        mock.Setup.Name.Returns("First");
        mock.Setup.Name.Returns("Second");

        // Act
        IPropertyService svc = mock.Object;
        var name = svc.Name;

        // Assert — last setup wins
        await Assert.That(name).IsEqualTo("Second");
    }

    [Test]
    public async Task Setter_Setup_Callback()
    {
        // Arrange
        var mock = Mock.Of<IPropertyService>();
        var callbackCalled = false;
        Action callback = () => callbackCalled = true;
        mock.Setup.Count.Set(Arg.Any<int>()).Callback(callback);

        // Act
        IPropertyService svc = mock.Object;
        svc.Count = 42;

        // Assert
        await Assert.That(callbackCalled).IsTrue();
    }
}
