using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate;
using Mockolate.Verify;
using Moq;
using NSubstitute;

namespace TUnit.Mocks.Benchmarks;

/// <summary>
/// Simulates a real-world test scenario: create mock, setup, invoke, verify.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
public class CombinedWorkflowBenchmarks
{
    [Benchmark(Description = "TUnit.Mocks")]
    public void TUnitMocks_FullWorkflow()
    {
        // Create
        var repoMock = Mock.Of<IUserRepository>();
        var loggerMock = Mock.Of<ILogger>();

        // Setup
        repoMock.GetById(1).Returns(new User { Id = 1, Name = "Alice", Email = "alice@test.com" });
        repoMock.Exists(1).Returns(true);
        loggerMock.IsEnabled(TUnitArg.Any<string>()).Returns(true);

        // Invoke
        var repo = repoMock.Object;
        var logger = loggerMock.Object;

        var user = repo.GetById(1);
        var exists = repo.Exists(1);
        logger.Log("INFO", $"User {user!.Name} exists: {exists}");
        repo.Save(new User { Id = 2, Name = "Bob" });

        // Verify
        repoMock.GetById(1).WasCalled(Times.Once);
        repoMock.Exists(1).WasCalled(Times.Once);
        repoMock.Save(TUnitArg.Any<User>()).WasCalled(Times.Once);
        loggerMock.Log(TUnitArg.Any<string>(), TUnitArg.Any<string>()).WasCalled(Times.Once);
    }

    [Benchmark(Description = "Imposter")]
    public void Imposter_FullWorkflow()
    {
        // Create
        var repoImposter = IUserRepository.Imposter();
        var loggerImposter = ILogger.Imposter();

        // Setup
        repoImposter.GetById(1).Returns(new User { Id = 1, Name = "Alice", Email = "alice@test.com" });
        repoImposter.Exists(1).Returns(true);
        loggerImposter.IsEnabled(Arg<string>.Any()).Returns(true);

        // Invoke
        var repo = repoImposter.Instance();
        var logger = loggerImposter.Instance();
        var user = repo.GetById(1);
        var exists = repo.Exists(1);
        logger.Log("INFO", $"User {user!.Name} exists: {exists}");
        repo.Save(new User { Id = 2, Name = "Bob" });

        // Verify
        repoImposter.GetById(1).Called(Count.Once());
        repoImposter.Exists(1).Called(Count.Once());
        repoImposter.Save(Arg<User>.Any()).Called(Count.Once());
        loggerImposter.Log(Arg<string>.Any(), Arg<string>.Any()).Called(Count.Once());
    }

    [Benchmark(Description = "Mockolate")]
    public void Mockolate_FullWorkflow()
    {
        // Create
        var repo = IUserRepository.CreateMock();
        var logger = ILogger.CreateMock();

        // Setup
        ((Mockolate.Mock.IMockForIUserRepository)repo).Setup.GetById(1).Returns(new User { Id = 1, Name = "Alice", Email = "alice@test.com" });
        ((Mockolate.Mock.IMockForIUserRepository)repo).Setup.Exists(1).Returns(true);
        ((Mockolate.Mock.IMockForILogger)logger).Setup.IsEnabled(Mockolate.It.IsAny<string>()).Returns(true);

        // Invoke
        var user = repo.GetById(1);
        var exists = repo.Exists(1);
        logger.Log("INFO", $"User {user!.Name} exists: {exists}");
        repo.Save(new User { Id = 2, Name = "Bob" });

        // Verify
        ((Mockolate.Mock.IMockForIUserRepository)repo).Verify.GetById(1).Once();
        ((Mockolate.Mock.IMockForIUserRepository)repo).Verify.Exists(1).Once();
        ((Mockolate.Mock.IMockForIUserRepository)repo).Verify.Save(Mockolate.It.IsAny<User>()).Once();
        ((Mockolate.Mock.IMockForILogger)logger).Verify.Log(Mockolate.It.IsAny<string>(), Mockolate.It.IsAny<string>()).Once();
    }

    [Benchmark(Description = "Moq")]
    public void Moq_FullWorkflow()
    {
        // Create
        var repoMock = new Moq.Mock<IUserRepository>();
        var loggerMock = new Moq.Mock<ILogger>();

        // Setup
        repoMock.Setup(x => x.GetById(1)).Returns(new User { Id = 1, Name = "Alice", Email = "alice@test.com" });
        repoMock.Setup(x => x.Exists(1)).Returns(true);
        loggerMock.Setup(x => x.IsEnabled(Moq.It.IsAny<string>())).Returns(true);

        // Invoke
        var repo = repoMock.Object;
        var logger = loggerMock.Object;

        var user = repo.GetById(1);
        var exists = repo.Exists(1);
        logger.Log("INFO", $"User {user!.Name} exists: {exists}");
        repo.Save(new User { Id = 2, Name = "Bob" });

        // Verify
        repoMock.Verify(x => x.GetById(1), Moq.Times.Once());
        repoMock.Verify(x => x.Exists(1), Moq.Times.Once());
        repoMock.Verify(x => x.Save(Moq.It.IsAny<User>()), Moq.Times.Once());
        loggerMock.Verify(x => x.Log(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()), Moq.Times.Once());
    }

    [Benchmark(Description = "NSubstitute")]
    public void NSubstitute_FullWorkflow()
    {
        // Create
        var repo = Substitute.For<IUserRepository>();
        var logger = Substitute.For<ILogger>();

        // Setup
        repo.GetById(1).Returns(new User { Id = 1, Name = "Alice", Email = "alice@test.com" });
        repo.Exists(1).Returns(true);
        logger.IsEnabled(NSubstitute.Arg.Any<string>()).Returns(true);

        // Invoke
        var user = repo.GetById(1);
        var exists = repo.Exists(1);
        logger.Log("INFO", $"User {user!.Name} exists: {exists}");
        repo.Save(new User { Id = 2, Name = "Bob" });

        // Verify
        repo.Received(1).GetById(1);
        repo.Received(1).Exists(1);
        repo.Received(1).Save(NSubstitute.Arg.Any<User>());
        logger.Received(1).Log(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<string>());
    }

    [Benchmark(Description = "FakeItEasy")]
    public void FakeItEasy_FullWorkflow()
    {
        // Create
        var repo = A.Fake<IUserRepository>();
        var logger = A.Fake<ILogger>();

        // Setup
        A.CallTo(() => repo.GetById(1)).Returns(new User { Id = 1, Name = "Alice", Email = "alice@test.com" });
        A.CallTo(() => repo.Exists(1)).Returns(true);
        A.CallTo(() => logger.IsEnabled(A<string>.Ignored)).Returns(true);

        // Invoke
        var user = repo.GetById(1);
        var exists = repo.Exists(1);
        logger.Log("INFO", $"User {user!.Name} exists: {exists}");
        repo.Save(new User { Id = 2, Name = "Bob" });

        // Verify
        A.CallTo(() => repo.GetById(1)).MustHaveHappenedOnceExactly();
        A.CallTo(() => repo.Exists(1)).MustHaveHappenedOnceExactly();
        A.CallTo(() => repo.Save(A<User>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => logger.Log(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
    }
}
