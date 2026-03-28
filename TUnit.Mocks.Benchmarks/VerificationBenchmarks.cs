using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Moq;
using NSubstitute;

namespace TUnit.Mocks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
public class VerificationBenchmarks
{
    [Benchmark(Description = "TUnit.Mocks")]
    public void TUnitMocks_Verify()
    {
        var mock = Mock.Of<ICalculatorService>();
        mock.Add(TUnitArg.Any<int>(), TUnitArg.Any<int>()).Returns(42);
        var calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(3, 4);

        mock.Add(TUnitArg.Any<int>(), TUnitArg.Any<int>()).WasCalled(Times.Exactly(2));
    }

    [Benchmark(Description = "Moq")]
    public void Moq_Verify()
    {
        var mock = new Moq.Mock<ICalculatorService>();
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
        var calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(3, 4);

        mock.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Moq.Times.Exactly(2));
    }

    [Benchmark(Description = "NSubstitute")]
    public void NSubstitute_Verify()
    {
        var sub = Substitute.For<ICalculatorService>();
        sub.Add(NSubstitute.Arg.Any<int>(), NSubstitute.Arg.Any<int>()).Returns(42);
        sub.Add(1, 2);
        sub.Add(3, 4);

        sub.Received(2).Add(NSubstitute.Arg.Any<int>(), NSubstitute.Arg.Any<int>());
    }

    [Benchmark(Description = "FakeItEasy")]
    public void FakeItEasy_Verify()
    {
        var fake = A.Fake<ICalculatorService>();
        A.CallTo(() => fake.Add(A<int>.Ignored, A<int>.Ignored)).Returns(42);
        fake.Add(1, 2);
        fake.Add(3, 4);

        A.CallTo(() => fake.Add(A<int>.Ignored, A<int>.Ignored)).MustHaveHappenedTwiceExactly();
    }

    [Benchmark(Description = "TUnit.Mocks (Never)")]
    public void TUnitMocks_VerifyNever()
    {
        var mock = Mock.Of<ICalculatorService>();
        mock.Format(TUnitArg.Any<int>()).WasNeverCalled();
    }

    [Benchmark(Description = "Moq (Never)")]
    public void Moq_VerifyNever()
    {
        var mock = new Moq.Mock<ICalculatorService>();
        mock.Verify(x => x.Format(It.IsAny<int>()), Moq.Times.Never());
    }

    [Benchmark(Description = "NSubstitute (Never)")]
    public void NSubstitute_VerifyNever()
    {
        var sub = Substitute.For<ICalculatorService>();
        sub.DidNotReceive().Format(NSubstitute.Arg.Any<int>());
    }

    [Benchmark(Description = "FakeItEasy (Never)")]
    public void FakeItEasy_VerifyNever()
    {
        var fake = A.Fake<ICalculatorService>();
        A.CallTo(() => fake.Format(A<int>.Ignored)).MustNotHaveHappened();
    }

    [Benchmark(Description = "TUnit.Mocks (Multiple)")]
    public void TUnitMocks_VerifyMultiple()
    {
        var mock = Mock.Of<IUserRepository>();
        mock.GetById(TUnitArg.Any<int>()).Returns(new User { Id = 1, Name = "Test" });
        mock.Exists(TUnitArg.Any<int>()).Returns(true);

        var repo = mock.Object;
        repo.GetById(1);
        repo.GetById(2);
        repo.Exists(1);
        repo.Save(new User { Id = 3, Name = "New" });

        mock.GetById(TUnitArg.Any<int>()).WasCalled(Times.Exactly(2));
        mock.Exists(TUnitArg.Any<int>()).WasCalled(Times.Once);
        mock.Save(TUnitArg.Any<User>()).WasCalled(Times.Once);
    }

    [Benchmark(Description = "Moq (Multiple)")]
    public void Moq_VerifyMultiple()
    {
        var mock = new Moq.Mock<IUserRepository>();
        mock.Setup(x => x.GetById(It.IsAny<int>())).Returns(new User { Id = 1, Name = "Test" });
        mock.Setup(x => x.Exists(It.IsAny<int>())).Returns(true);

        var repo = mock.Object;
        repo.GetById(1);
        repo.GetById(2);
        repo.Exists(1);
        repo.Save(new User { Id = 3, Name = "New" });

        mock.Verify(x => x.GetById(It.IsAny<int>()), Moq.Times.Exactly(2));
        mock.Verify(x => x.Exists(It.IsAny<int>()), Moq.Times.Once());
        mock.Verify(x => x.Save(It.IsAny<User>()), Moq.Times.Once());
    }

    [Benchmark(Description = "NSubstitute (Multiple)")]
    public void NSubstitute_VerifyMultiple()
    {
        var sub = Substitute.For<IUserRepository>();
        sub.GetById(NSubstitute.Arg.Any<int>()).Returns(new User { Id = 1, Name = "Test" });
        sub.Exists(NSubstitute.Arg.Any<int>()).Returns(true);

        sub.GetById(1);
        sub.GetById(2);
        sub.Exists(1);
        sub.Save(new User { Id = 3, Name = "New" });

        sub.Received(2).GetById(NSubstitute.Arg.Any<int>());
        sub.Received(1).Exists(NSubstitute.Arg.Any<int>());
        sub.Received(1).Save(NSubstitute.Arg.Any<User>());
    }

    [Benchmark(Description = "FakeItEasy (Multiple)")]
    public void FakeItEasy_VerifyMultiple()
    {
        var fake = A.Fake<IUserRepository>();
        A.CallTo(() => fake.GetById(A<int>.Ignored)).Returns(new User { Id = 1, Name = "Test" });
        A.CallTo(() => fake.Exists(A<int>.Ignored)).Returns(true);

        fake.GetById(1);
        fake.GetById(2);
        fake.Exists(1);
        fake.Save(new User { Id = 3, Name = "New" });

        A.CallTo(() => fake.GetById(A<int>.Ignored)).MustHaveHappenedTwiceExactly();
        A.CallTo(() => fake.Exists(A<int>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fake.Save(A<User>.Ignored)).MustHaveHappenedOnceExactly();
    }
}
