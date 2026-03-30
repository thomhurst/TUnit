using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate;
using Moq;
using NSubstitute;

namespace TUnit.Mocks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
public class CallbackBenchmarks
{
    [Benchmark(Description = "TUnit.Mocks")]
    public int TUnitMocks_Callback()
    {
        var count = 0;
        var mock = Mock.Of<INotificationService>();
        mock.Send(TUnitArg.Any<string>(), TUnitArg.Any<string>())
            .Callback(() => count++);

        var svc = mock.Object;
        svc.Send("user@test.com", "Hello");
        svc.Send("user@test.com", "World");
        return count;
    }

    [Benchmark(Description = "Imposter")]
    public int Imposter_Callback()
    {
        var count = 0;
        var imposter = INotificationService.Imposter();
        imposter.Send(Arg<string>.Any(), Arg<string>.Any())
            .Callback((_, _) => count++);

        var instance = imposter.Instance();
        instance.Send("user@test.com", "Hello");
        instance.Send("user@test.com", "World");
        return count;
    }

    [Benchmark(Description = "Mockolate")]
    public int Mockolate_Callback()
    {
        var count = 0;
        var svc = INotificationService.CreateMock();
        svc.Mock.Setup.Send(Mockolate.It.IsAny<string>(), Mockolate.It.IsAny<string>())
            .Do(() => count++);

        svc.Send("user@test.com", "Hello");
        svc.Send("user@test.com", "World");
        return count;
    }

    [Benchmark(Description = "Moq")]
    public int Moq_Callback()
    {
        var count = 0;
        var mock = new Moq.Mock<INotificationService>();
        mock.Setup(x => x.Send(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Callback(() => count++);

        var svc = mock.Object;
        svc.Send("user@test.com", "Hello");
        svc.Send("user@test.com", "World");
        return count;
    }

    [Benchmark(Description = "NSubstitute")]
    public int NSubstitute_Callback()
    {
        var count = 0;
        var sub = Substitute.For<INotificationService>();
        sub.When(x => x.Send(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<string>()))
            .Do(_ => count++);

        sub.Send("user@test.com", "Hello");
        sub.Send("user@test.com", "World");
        return count;
    }

    [Benchmark(Description = "FakeItEasy")]
    public int FakeItEasy_Callback()
    {
        var count = 0;
        var fake = A.Fake<INotificationService>();
        A.CallTo(() => fake.Send(A<string>.Ignored, A<string>.Ignored))
            .Invokes(() => count++);

        fake.Send("user@test.com", "Hello");
        fake.Send("user@test.com", "World");
        return count;
    }

    [Benchmark(Description = "TUnit.Mocks (with args)")]
    public string TUnitMocks_CallbackWithArgs()
    {
        var lastMessage = "";
        var mock = Mock.Of<ILogger>();
        mock.Log(TUnitArg.Any<string>(), TUnitArg.Any<string>())
            .Callback((string _, string msg) => lastMessage = msg);

        var logger = mock.Object;
        logger.Log("INFO", "Test message 1");
        logger.Log("WARN", "Test message 2");
        logger.Log("ERROR", "Test message 3");
        return lastMessage;
    }

    [Benchmark(Description = "Imposter (with args)")]
    public string Imposter_CallbackWithArgs()
    {
        var lastMessage = "";
        var imposter = ILogger.Imposter();
        imposter.Log(Arg<string>.Any(), Arg<string>.Any())
            .Callback((_, msg) => lastMessage = msg);

        var instance = imposter.Instance();
        instance.Log("INFO", "Test message 1");
        instance.Log("WARN", "Test message 2");
        instance.Log("ERROR", "Test message 3");
        return lastMessage;
    }

    [Benchmark(Description = "Mockolate (with args)")]
    public string Mockolate_CallbackWithArgs()
    {
        var lastMessage = "";
        var logger = ILogger.CreateMock();
        logger.Mock.Setup.Log(Mockolate.It.IsAny<string>(), Mockolate.It.IsAny<string>())
            .Do((_, msg) => lastMessage = msg);

        logger.Log("INFO", "Test message 1");
        logger.Log("WARN", "Test message 2");
        logger.Log("ERROR", "Test message 3");
        return lastMessage;
    }

    [Benchmark(Description = "Moq (with args)")]
    public string Moq_CallbackWithArgs()
    {
        var lastMessage = "";
        var mock = new Moq.Mock<ILogger>();
        mock.Setup(x => x.Log(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Callback<string, string>((level, msg) => lastMessage = msg);

        var logger = mock.Object;
        logger.Log("INFO", "Test message 1");
        logger.Log("WARN", "Test message 2");
        logger.Log("ERROR", "Test message 3");
        return lastMessage;
    }

    [Benchmark(Description = "NSubstitute (with args)")]
    public string NSubstitute_CallbackWithArgs()
    {
        var lastMessage = "";
        var sub = Substitute.For<ILogger>();
        sub.When(x => x.Log(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<string>()))
            .Do(callInfo => lastMessage = callInfo.ArgAt<string>(1));

        sub.Log("INFO", "Test message 1");
        sub.Log("WARN", "Test message 2");
        sub.Log("ERROR", "Test message 3");
        return lastMessage;
    }

    [Benchmark(Description = "FakeItEasy (with args)")]
    public string FakeItEasy_CallbackWithArgs()
    {
        var lastMessage = "";
        var fake = A.Fake<ILogger>();
        A.CallTo(() => fake.Log(A<string>.Ignored, A<string>.Ignored))
            .Invokes((string level, string msg) => lastMessage = msg);

        fake.Log("INFO", "Test message 1");
        fake.Log("WARN", "Test message 2");
        fake.Log("ERROR", "Test message 3");
        return lastMessage;
    }
}
