#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using TUnit.Mocks;
using TUnit.Mocks.Generated;
using TUnit.Mocks.Generated.TUnit.Mocks.Tests;

// Discovery: typeof() does not trigger CS8920, so this is safe for interfaces
// with static abstract members. The generator produces a bridge interface
// (IAmazonServiceMockable) that resolves the static abstracts.
[assembly: TUnit.Mocks.GenerateMock(typeof(TUnit.Mocks.Tests.IAmazonService))]

namespace TUnit.Mocks.Tests;

public class ClientConfig
{
    public string Region { get; set; } = "us-east-1";
}

public class AWSCredentials
{
    public string AccessKey { get; set; } = "";
    public string AuthSignature { get; set; } = "";
}

public interface IClientConfig
{
    string Region { get; }
}

public interface IServiceConfig
{
    static abstract ClientConfig CreateDefaultConfig();
    static abstract string ServiceId { get; set; }
}

public interface IAmazonService : IServiceConfig
{
    /// <summary>A readonly view of the configuration for the service client.</summary>
    IClientConfig Config { get; }

    string GetEndpoint();
    void Initialize(string region);

    /// <summary>Factory method for creating the service client config object.</summary>
    static abstract ClientConfig CreateDefaultClientConfig();

    /// <summary>
    /// Factory method for creating the default implementation of the AWS service interface.
    /// Returns <see cref="IAmazonService"/>, which itself has static abstract members —
    /// this is the CS8920 transitive scenario our fix addresses.
    /// </summary>
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026")]
    static abstract IAmazonService CreateDefaultServiceClient(
        AWSCredentials awsCredentials,
        ClientConfig clientConfig);
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
        var mock = IAmazonServiceMockable.Mock();
        var expectedConfig = new ClientConfig { Region = "eu-west-1" };
        mock.CreateDefaultConfig().Returns(expectedConfig);

        // Act — call through constrained generic using the bridge type
        var result = CallStaticAbstract<IAmazonServiceMockable>();

        // Assert
        await Assert.That(result).IsSameReferenceAs(expectedConfig);
    }

    [Test]
    public async Task Static_Abstract_Method_Returns_Default_When_No_Setup()
    {
        // Arrange
        var mock = IAmazonServiceMockable.Mock();

        // Act — no setup, should return default
        var result = CallStaticAbstract<IAmazonServiceMockable>();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Static_Abstract_Method_Throws_Configured_Exception()
    {
        // Arrange
        var mock = IAmazonServiceMockable.Mock();
        mock.CreateDefaultConfig().Throws(new InvalidOperationException("not available"));

        // Act & Assert
        await Assert.That(() => CallStaticAbstract<IAmazonServiceMockable>())
            .ThrowsExactly<InvalidOperationException>()
            .WithMessage("not available");
    }

    [Test]
    public async Task Static_Abstract_Method_Verification()
    {
        // Arrange
        var mock = IAmazonServiceMockable.Mock();
        mock.CreateDefaultConfig().Returns(new ClientConfig());

        // Act
        CallStaticAbstract<IAmazonServiceMockable>();
        CallStaticAbstract<IAmazonServiceMockable>();

        // Assert
        mock.CreateDefaultConfig().WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Static_Abstract_Property_Getter_Returns_Configured_Value()
    {
        // Arrange
        var mock = IAmazonServiceMockable.Mock();
        mock.ServiceId.Getter.Returns("s3");

        // Act
        var result = GetStaticAbstractProperty<IAmazonServiceMockable>();

        // Assert
        await Assert.That(result).IsEqualTo("s3");
    }

    [Test]
    public async Task Static_Abstract_Property_Setter_Can_Be_Verified()
    {
        // Arrange
        var mock = IAmazonServiceMockable.Mock();

        // Act
        SetStaticAbstractProperty<IAmazonServiceMockable>("dynamodb");

        // Assert
        mock.ServiceId.Setter.WasCalled();
    }

    [Test]
    public async Task Instance_And_Static_Members_Coexist()
    {
        // Arrange
        var mock = IAmazonServiceMockable.Mock();
        mock.GetEndpoint().Returns("https://s3.amazonaws.com");
        mock.CreateDefaultConfig().Returns(new ClientConfig { Region = "ap-southeast-1" });

        // Act
        var endpoint = mock.Object.GetEndpoint();
        var config = CallStaticAbstract<IAmazonServiceMockable>();

        // Assert
        await Assert.That(endpoint).IsEqualTo("https://s3.amazonaws.com");
        await Assert.That(config!.Region).IsEqualTo("ap-southeast-1");
    }

    // --- CreateDefaultClientConfig (static abstract, returns concrete ClientConfig) ---

    [Test]
    public async Task Static_Abstract_CreateDefaultClientConfig_Returns_Configured_Value()
    {
        var mock = IAmazonServiceMockable.Mock();
        var expected = new ClientConfig { Region = "sa-east-1" };
        mock.CreateDefaultClientConfig().Returns(expected);

        var result = CallCreateDefaultClientConfig<IAmazonServiceMockable>();

        await Assert.That(result).IsSameReferenceAs(expected);
    }

    [Test]
    public async Task Static_Abstract_CreateDefaultClientConfig_Verification()
    {
        var mock = IAmazonServiceMockable.Mock();
        mock.CreateDefaultClientConfig().Returns(new ClientConfig());

        CallCreateDefaultClientConfig<IAmazonServiceMockable>();
        CallCreateDefaultClientConfig<IAmazonServiceMockable>();

        mock.CreateDefaultClientConfig().WasCalled(Times.Exactly(2));
    }

    // --- CreateDefaultServiceClient (static abstract, returns IAmazonService — the CS8920 transitive scenario) ---

    [Test]
    public async Task Static_Abstract_CreateDefaultServiceClient_Returns_Null_By_Default()
    {
        // No setup — the generator uses HandleCallWithReturn<object?> + cast,
        // so the default null value is cast to IAmazonService and returned.
        var mock = IAmazonServiceMockable.Mock();

        var result = CallCreateDefaultServiceClient<IAmazonServiceMockable>(
            new AWSCredentials(), new ClientConfig());

        // Cast to object? — using IAmazonService directly as a type argument triggers CS8920.
        await Assert.That((object?)result).IsNull();
    }

    [Test]
    public async Task Static_Abstract_CreateDefaultServiceClient_Returns_Configured_Value()
    {
        // Arrange — .Returns() works via MockMethodCall<object?> (not VoidMockMethodCall)
        // because the return type (IAmazonService) has static abstract members (CS8920).
        var mock = IAmazonServiceMockable.Mock();
        var expectedService = mock.Object;
        mock.CreateDefaultServiceClient(Arg.Any<AWSCredentials>(), Arg.Any<ClientConfig>())
            .Returns(expectedService);

        // Act
        var result = CallCreateDefaultServiceClient<IAmazonServiceMockable>(
            new AWSCredentials(), new ClientConfig());

        // Assert — the configured value is returned through the object? → IAmazonService cast
        await Assert.That((object?)result).IsSameReferenceAs(expectedService);
    }

    [Test]
    public async Task Static_Abstract_CreateDefaultServiceClient_Verification()
    {
        var mock = IAmazonServiceMockable.Mock();
        var creds = new AWSCredentials { AccessKey = "AKID", AuthSignature = "test-sig" };
        var config = new ClientConfig { Region = "us-west-2" };

        CallCreateDefaultServiceClient<IAmazonServiceMockable>(creds, config);

        // MockMethodCall<object?> supports verification even though the return type
        // (IAmazonService) cannot be used as a generic type argument (CS8920).
        mock.CreateDefaultServiceClient(Arg.Any<AWSCredentials>(), Arg.Any<ClientConfig>()).WasCalled();
    }

    [Test]
    public async Task Static_Abstract_CreateDefaultServiceClient_Throws_Configured_Exception()
    {
        var mock = IAmazonServiceMockable.Mock();
        mock.CreateDefaultServiceClient(Arg.Any<AWSCredentials>(), Arg.Any<ClientConfig>())
            .Throws(new InvalidOperationException("service unavailable"));

        await Assert.That(() => CallCreateDefaultServiceClient<IAmazonServiceMockable>(
                new AWSCredentials(), new ClientConfig()))
            .ThrowsExactly<InvalidOperationException>()
            .WithMessage("service unavailable");
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

    private static ClientConfig? CallCreateDefaultClientConfig<T>() where T : IAmazonService
        => T.CreateDefaultClientConfig();

    private static IAmazonService? CallCreateDefaultServiceClient<T>(AWSCredentials creds, ClientConfig config)
        where T : IAmazonService
        => T.CreateDefaultServiceClient(creds, config);
}
#endif
