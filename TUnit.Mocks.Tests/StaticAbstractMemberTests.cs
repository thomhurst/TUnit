#if NET7_0_OR_GREATER
using TUnit.Mocks;
using TUnit.Mocks.Generated;

// Discovery: typeof() does not trigger CS8920, so this is safe for interfaces
// with static abstract members. The generator produces a bridge interface
// (TUnit_Mocks_Tests_IAmazonService_Mockable) that resolves the static abstracts.
[assembly: TUnit.Mocks.GenerateMock(typeof(TUnit.Mocks.Tests.IAmazonService))]

namespace TUnit.Mocks.Tests;

public class ClientConfig
{
    public string Region { get; set; } = "us-east-1";
}

public interface IServiceConfig
{
    static abstract ClientConfig CreateDefaultConfig();
    static abstract string ServiceId { get; set; }
}

public interface IAmazonService : IServiceConfig
{
    string GetEndpoint();
    void Initialize(string region);
}

/// <summary>
/// Integration tests for static abstract interface member mock support.
/// Uses the generated bridge interface (_Mockable) because C# CS8920 prevents
/// using interfaces with unresolved static abstract members as generic type arguments.
/// </summary>
public class StaticAbstractMemberTests
{
    [Test]
    public async Task Static_Abstract_Method_Returns_Configured_Value()
    {
        // Arrange — use the bridge type which resolves static abstract members
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();
        var expectedConfig = new ClientConfig { Region = "eu-west-1" };
        mock.CreateDefaultConfig().Returns(expectedConfig);

        // Act — call through constrained generic using the bridge type
        var result = CallStaticAbstract<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Assert
        await Assert.That(result).IsSameReferenceAs(expectedConfig);
    }

    [Test]
    public async Task Static_Abstract_Method_Returns_Default_When_No_Setup()
    {
        // Arrange
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Act — no setup, should return default
        var result = CallStaticAbstract<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Static_Abstract_Method_Throws_Configured_Exception()
    {
        // Arrange
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();
        mock.CreateDefaultConfig().Throws(new InvalidOperationException("not available"));

        // Act & Assert
        await Assert.That(() => CallStaticAbstract<TUnit_Mocks_Tests_IAmazonService_Mockable>())
            .ThrowsExactly<InvalidOperationException>()
            .WithMessage("not available");
    }

    [Test]
    public async Task Static_Abstract_Method_Verification()
    {
        // Arrange
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();
        mock.CreateDefaultConfig().Returns(new ClientConfig());

        // Act
        CallStaticAbstract<TUnit_Mocks_Tests_IAmazonService_Mockable>();
        CallStaticAbstract<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Assert
        mock.CreateDefaultConfig().WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Static_Abstract_Property_Getter_Returns_Configured_Value()
    {
        // Arrange
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();
        mock.ServiceId.Getter.Returns("s3");

        // Act
        var result = GetStaticAbstractProperty<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Assert
        await Assert.That(result).IsEqualTo("s3");
    }

    [Test]
    public async Task Static_Abstract_Property_Setter_Can_Be_Verified()
    {
        // Arrange
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Act
        SetStaticAbstractProperty<TUnit_Mocks_Tests_IAmazonService_Mockable>("dynamodb");

        // Assert
        mock.ServiceId.Setter.WasCalled();
    }

    [Test]
    public async Task Instance_And_Static_Members_Coexist()
    {
        // Arrange
        var mock = Mock.Of<TUnit_Mocks_Tests_IAmazonService_Mockable>();
        mock.GetEndpoint().Returns("https://s3.amazonaws.com");
        mock.CreateDefaultConfig().Returns(new ClientConfig { Region = "ap-southeast-1" });

        // Act
        var endpoint = mock.Object.GetEndpoint();
        var config = CallStaticAbstract<TUnit_Mocks_Tests_IAmazonService_Mockable>();

        // Assert
        await Assert.That(endpoint).IsEqualTo("https://s3.amazonaws.com");
        await Assert.That(config!.Region).IsEqualTo("ap-southeast-1");
    }

    /// <summary>
    /// Calls the static abstract method through a constrained generic, which is the only
    /// way to invoke static abstract members in C#.
    /// </summary>
    private static ClientConfig? CallStaticAbstract<T>() where T : IServiceConfig
        => T.CreateDefaultConfig();

    private static string? GetStaticAbstractProperty<T>() where T : IServiceConfig
        => T.ServiceId;

    private static void SetStaticAbstractProperty<T>(string value) where T : IServiceConfig
        => T.ServiceId = value;
}
#endif
