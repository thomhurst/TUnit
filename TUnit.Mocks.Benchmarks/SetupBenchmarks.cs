using BenchmarkDotNet.Attributes;

using FakeItEasy;
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

    [Benchmark(Description = "Moq")]
    public object Moq_Setup()
    {
        var mock = new Moq.Mock<ICalculatorService>();
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
        mock.Setup(x => x.Format(It.IsAny<int>())).Returns("formatted");
        mock.Setup(x => x.Divide(It.IsAny<double>(), It.IsAny<double>())).Returns(1.5);
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

    [Benchmark(Description = "Moq (Multiple)")]
    public object Moq_MultipleSetups()
    {
        var mock = new Moq.Mock<IUserRepository>();
        mock.Setup(x => x.GetById(1)).Returns(new User { Id = 1, Name = "Alice" });
        mock.Setup(x => x.GetById(2)).Returns(new User { Id = 2, Name = "Bob" });
        mock.Setup(x => x.GetById(3)).Returns(new User { Id = 3, Name = "Charlie" });
        mock.Setup(x => x.Exists(It.IsAny<int>())).Returns(true);
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
