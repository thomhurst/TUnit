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
public class SetupBenchmarks
{
    [Benchmark(Description = "TUnit.Mocks")]
    public object TUnitMocks_Setup()
    {
        var mock = Mock.Of<ICalculatorService>();
        mock.Add(TUnitArg.Any<int>(), TUnitArg.Any<int>()).Returns(42);
        mock.Format(TUnitArg.Any<int>()).Returns("formatted");
        mock.Divide(TUnitArg.Any<double>(), TUnitArg.Any<double>()).Returns(1.5);
        return mock.Object;
    }

    [Benchmark(Description = "Imposter")]
    public object Imposter_Setup()
    {
        var imposter = ICalculatorService.Imposter();
        imposter.Add(Arg<int>.Any(), Arg<int>.Any()).Returns(42);
        imposter.Format(Arg<int>.Any()).Returns("formatted");
        imposter.Divide(Arg<double>.Any(), Arg<double>.Any()).Returns(1.5);
        return imposter.Instance();
    }

    [Benchmark(Description = "Mockolate")]
    public object Mockolate_Setup()
    {
        var sut = ICalculatorService.CreateMock();
        ((Mockolate.Mock.IMockForICalculatorService)sut).Setup.Add(Mockolate.It.IsAny<int>(), Mockolate.It.IsAny<int>()).Returns(42);
        ((Mockolate.Mock.IMockForICalculatorService)sut).Setup.Format(Mockolate.It.IsAny<int>()).Returns("formatted");
        ((Mockolate.Mock.IMockForICalculatorService)sut).Setup.Divide(Mockolate.It.IsAny<double>(), Mockolate.It.IsAny<double>()).Returns(1.5);
        return sut;
    }

    [Benchmark(Description = "Moq")]
    public object Moq_Setup()
    {
        var mock = new Moq.Mock<ICalculatorService>();
        mock.Setup(x => x.Add(Moq.It.IsAny<int>(), Moq.It.IsAny<int>())).Returns(42);
        mock.Setup(x => x.Format(Moq.It.IsAny<int>())).Returns("formatted");
        mock.Setup(x => x.Divide(Moq.It.IsAny<double>(), Moq.It.IsAny<double>())).Returns(1.5);
        return mock.Object;
    }

    [Benchmark(Description = "NSubstitute")]
    public object NSubstitute_Setup()
    {
        var sub = Substitute.For<ICalculatorService>();
        sub.Add(NSubstitute.Arg.Any<int>(), NSubstitute.Arg.Any<int>()).Returns(42);
        sub.Format(NSubstitute.Arg.Any<int>()).Returns("formatted");
        sub.Divide(NSubstitute.Arg.Any<double>(), NSubstitute.Arg.Any<double>()).Returns(1.5);
        return sub;
    }

    [Benchmark(Description = "FakeItEasy")]
    public object FakeItEasy_Setup()
    {
        var fake = A.Fake<ICalculatorService>();
        A.CallTo(() => fake.Add(A<int>.Ignored, A<int>.Ignored)).Returns(42);
        A.CallTo(() => fake.Format(A<int>.Ignored)).Returns("formatted");
        A.CallTo(() => fake.Divide(A<double>.Ignored, A<double>.Ignored)).Returns(1.5);
        return fake;
    }

    [Benchmark(Description = "TUnit.Mocks (Multiple)")]
    public object TUnitMocks_MultipleSetups()
    {
        var mock = Mock.Of<IUserRepository>();
        mock.GetById(1).Returns(new User { Id = 1, Name = "Alice" });
        mock.GetById(2).Returns(new User { Id = 2, Name = "Bob" });
        mock.GetById(3).Returns(new User { Id = 3, Name = "Charlie" });
        mock.Exists(TUnitArg.Any<int>()).Returns(true);
        mock.GetAll().Returns(new List<User>());
        return mock.Object;
    }

    [Benchmark(Description = "Imposter (Multiple)")]
    public object Imposter_MultipleSetups()
    {
        var imposter = IUserRepository.Imposter();
        imposter.GetById(1).Returns(new User { Id = 1, Name = "Alice" });
        imposter.GetById(2).Returns(new User { Id = 2, Name = "Bob" });
        imposter.GetById(3).Returns(new User { Id = 3, Name = "Charlie" });
        imposter.Exists(Arg<int>.Any()).Returns(true);
        imposter.GetAll().Returns(new List<User>());
        return imposter.Instance();
    }

    [Benchmark(Description = "Mockolate (Multiple)")]
    public object Mockolate_MultipleSetups()
    {
        var sut = IUserRepository.CreateMock();
        ((Mockolate.Mock.IMockForIUserRepository)sut).Setup.GetById(1).Returns(new User { Id = 1, Name = "Alice" });
        ((Mockolate.Mock.IMockForIUserRepository)sut).Setup.GetById(2).Returns(new User { Id = 2, Name = "Bob" });
        ((Mockolate.Mock.IMockForIUserRepository)sut).Setup.GetById(3).Returns(new User { Id = 3, Name = "Charlie" });
        ((Mockolate.Mock.IMockForIUserRepository)sut).Setup.Exists(Mockolate.It.IsAny<int>()).Returns(true);
        ((Mockolate.Mock.IMockForIUserRepository)sut).Setup.GetAll().Returns(new List<User>());
        return sut;
    }

    [Benchmark(Description = "Moq (Multiple)")]
    public object Moq_MultipleSetups()
    {
        var mock = new Moq.Mock<IUserRepository>();
        mock.Setup(x => x.GetById(1)).Returns(new User { Id = 1, Name = "Alice" });
        mock.Setup(x => x.GetById(2)).Returns(new User { Id = 2, Name = "Bob" });
        mock.Setup(x => x.GetById(3)).Returns(new User { Id = 3, Name = "Charlie" });
        mock.Setup(x => x.Exists(Moq.It.IsAny<int>())).Returns(true);
        mock.Setup(x => x.GetAll()).Returns(new List<User>());
        return mock.Object;
    }

    [Benchmark(Description = "NSubstitute (Multiple)")]
    public object NSubstitute_MultipleSetups()
    {
        var sub = Substitute.For<IUserRepository>();
        sub.GetById(1).Returns(new User { Id = 1, Name = "Alice" });
        sub.GetById(2).Returns(new User { Id = 2, Name = "Bob" });
        sub.GetById(3).Returns(new User { Id = 3, Name = "Charlie" });
        sub.Exists(NSubstitute.Arg.Any<int>()).Returns(true);
        sub.GetAll().Returns(new List<User>());
        return sub;
    }

    [Benchmark(Description = "FakeItEasy (Multiple)")]
    public object FakeItEasy_MultipleSetups()
    {
        var fake = A.Fake<IUserRepository>();
        A.CallTo(() => fake.GetById(1)).Returns(new User { Id = 1, Name = "Alice" });
        A.CallTo(() => fake.GetById(2)).Returns(new User { Id = 2, Name = "Bob" });
        A.CallTo(() => fake.GetById(3)).Returns(new User { Id = 3, Name = "Charlie" });
        A.CallTo(() => fake.Exists(A<int>.Ignored)).Returns(true);
        A.CallTo(() => fake.GetAll()).Returns(new List<User>());
        return fake;
    }
}
