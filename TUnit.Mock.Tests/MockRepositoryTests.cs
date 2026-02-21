using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Tests;

/// <summary>
/// Interfaces for MockRepository testing.
/// </summary>
public interface IRepoService
{
    string GetData(int id);
    void Save(string data);
}

public interface IRepoLogger
{
    void Log(string message);
    string LastEntry { get; }
}

/// <summary>
/// US14 Tests: Mock Repository &amp; Batch Operations.
/// </summary>
public class MockRepositoryTests
{
    [Test]
    public async Task Repository_Creates_And_Tracks_Mocks()
    {
        // Arrange
        var repo = new MockRepository();

        // Act
        var serviceMock = repo.Of<IRepoService>();
        var loggerMock = repo.Of<IRepoLogger>();

        // Assert
        await Assert.That(repo.Mocks).Count().IsEqualTo(2);
        await Assert.That(serviceMock.Object).IsNotNull();
        await Assert.That(loggerMock.Object).IsNotNull();
    }

    [Test]
    public async Task Repository_VerifyAll_Passes_When_All_Setups_Invoked()
    {
        // Arrange
        var repo = new MockRepository();
        var serviceMock = repo.Of<IRepoService>();
        var loggerMock = repo.Of<IRepoLogger>();

        serviceMock.Setup.GetData(Arg.Any<int>()).Returns("result");
        loggerMock.Setup.Log(Arg.Any<string>());

        // Act — invoke all setups
        serviceMock.Object.GetData(1);
        loggerMock.Object.Log("hello");

        // Assert — VerifyAll should not throw
        repo.VerifyAll();
    }

    [Test]
    public async Task Repository_VerifyAll_Throws_When_Setup_Not_Invoked()
    {
        // Arrange
        var repo = new MockRepository();
        var serviceMock = repo.Of<IRepoService>();
        var loggerMock = repo.Of<IRepoLogger>();

        serviceMock.Setup.GetData(Arg.Any<int>()).Returns("result");
        loggerMock.Setup.Log(Arg.Any<string>());

        // Act — only invoke one mock's setup
        serviceMock.Object.GetData(1);
        // loggerMock.Object.Log("hello") is NOT called

        // Assert — VerifyAll should throw for loggerMock's uninvoked setup
        await Assert.That(() => repo.VerifyAll()).Throws<MockVerificationException>();
    }

    [Test]
    public async Task Repository_VerifyNoOtherCalls_Passes_When_All_Verified()
    {
        // Arrange
        var repo = new MockRepository();
        var serviceMock = repo.Of<IRepoService>();
        var loggerMock = repo.Of<IRepoLogger>();

        // Act
        serviceMock.Object.GetData(1);
        loggerMock.Object.Log("hello");

        // Verify each call
        serviceMock.Verify!.GetData(Arg.Is(1)).WasCalled();
        loggerMock.Verify!.Log(Arg.Is("hello")).WasCalled();

        // Assert — no unverified calls
        repo.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Repository_VerifyNoOtherCalls_Throws_When_Unverified_Calls_Exist()
    {
        // Arrange
        var repo = new MockRepository();
        var serviceMock = repo.Of<IRepoService>();
        var loggerMock = repo.Of<IRepoLogger>();

        // Act
        serviceMock.Object.GetData(1);
        loggerMock.Object.Log("hello");

        // Only verify one mock
        serviceMock.Verify!.GetData(Arg.Is(1)).WasCalled();

        // Assert — loggerMock has unverified calls
        await Assert.That(() => repo.VerifyNoOtherCalls()).Throws<MockVerificationException>();
    }

    [Test]
    public async Task Repository_Reset_Clears_All_Mocks()
    {
        // Arrange
        var repo = new MockRepository();
        var serviceMock = repo.Of<IRepoService>();
        var loggerMock = repo.Of<IRepoLogger>();

        serviceMock.Setup.GetData(Arg.Any<int>()).Returns("configured");
        loggerMock.Object.Log("call before reset");

        // Act
        repo.Reset();

        // Assert — setups and history are cleared
        await Assert.That(serviceMock.Object.GetData(1)).IsEmpty(); // no setup, returns smart default
        await Assert.That(serviceMock.Invocations).Count().IsEqualTo(1); // only the new call
        await Assert.That(loggerMock.Invocations).Count().IsEqualTo(0); // history cleared
    }

    [Test]
    public async Task Repository_Uses_Default_Behavior()
    {
        // Arrange — create repository with strict behavior
        var repo = new MockRepository(MockBehavior.Strict);
        var mock = repo.Of<IRepoService>();

        // Assert — mock inherits strict behavior
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Strict);
    }

    [Test]
    public async Task Repository_Behavior_Can_Be_Overridden_Per_Mock()
    {
        // Arrange — strict repository, loose individual mock
        var repo = new MockRepository(MockBehavior.Strict);
        var looseMock = repo.Of<IRepoService>(MockBehavior.Loose);

        // Assert — specific behavior overrides repository default
        await Assert.That(looseMock.Behavior).IsEqualTo(MockBehavior.Loose);
    }

    [Test]
    public async Task Repository_Track_Adds_Existing_Mock()
    {
        // Arrange
        var repo = new MockRepository();
        var externalMock = Mock.Of<IRepoService>();

        // Act — track an externally-created mock
        repo.Track(externalMock);

        // Assert
        await Assert.That(repo.Mocks).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Repository_VerifyAll_On_Empty_Repository_Does_Not_Throw()
    {
        // Arrange
        var repo = new MockRepository();

        // Assert — no mocks tracked, nothing to verify
        repo.VerifyAll();
        repo.VerifyNoOtherCalls();
        repo.Reset();
    }
}
