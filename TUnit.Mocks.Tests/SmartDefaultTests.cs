using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Test interface for verifying smart default return values.
/// </summary>
public interface IDefaultsService
{
    string GetName();
    int GetCount();
    bool IsActive();
    object? GetNullable();
    IList<string> GetItems();
    double GetScore();
    long GetTimestamp();
}

/// <summary>
/// US6 Integration Tests: Smart defaults — unconfigured methods return sensible defaults based on type.
/// </summary>
public class SmartDefaultTests
{
    [Test]
    public async Task Unconfigured_String_Returns_Empty_String()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.GetName();

        // Assert — non-nullable string smart default is ""
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task Unconfigured_Int_Returns_Zero()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.GetCount();

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Unconfigured_Bool_Returns_False()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.IsActive();

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Unconfigured_Nullable_Returns_Null()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.GetNullable();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Unconfigured_List_Returns_Empty_Collection()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.GetItems();

        // Assert — IList<string> smart default is an empty array
        await Assert.That(result).IsNotNull();
        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Unconfigured_Double_Returns_Zero()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.GetScore();

        // Assert
        await Assert.That(result).IsEqualTo(0.0);
    }

    [Test]
    public async Task Unconfigured_Long_Returns_Zero()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();

        // Act
        IDefaultsService svc = mock.Object;
        var result = svc.GetTimestamp();

        // Assert
        await Assert.That(result).IsEqualTo(0L);
    }

    [Test]
    public async Task Configured_Overrides_Smart_Default()
    {
        // Arrange
        var mock = Mock.Of<IDefaultsService>();
        mock.Setup.GetName().Returns("custom");
        mock.Setup.GetCount().Returns(42);

        // Act
        IDefaultsService svc = mock.Object;

        // Assert — configured values override defaults
        await Assert.That(svc.GetName()).IsEqualTo("custom");
        await Assert.That(svc.GetCount()).IsEqualTo(42);
    }

    [Test]
    public async Task Smart_Defaults_Consistent_With_ICalculator()
    {
        // Verify that the existing ICalculator interface also has correct smart defaults
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // int -> 0
        await Assert.That(calc.Add(1, 2)).IsEqualTo(0);

        // string -> "" (non-nullable)
        await Assert.That(calc.GetName()).IsNotNull();
    }
}
