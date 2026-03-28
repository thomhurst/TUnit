using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Moq;
using NSubstitute;
using TUnit.Mocks;

namespace TUnit.Mocks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
public class MockCreationBenchmarks
{
    [Benchmark(Description = "TUnit.Mocks")]
    public object TUnitMocks_CreateMock()
    {
        var mock = Mock.Of<ICalculatorService>();
        return mock.Object;
    }

    [Benchmark(Description = "Moq")]
    public object Moq_CreateMock()
    {
        var mock = new Moq.Mock<ICalculatorService>();
        return mock.Object;
    }

    [Benchmark(Description = "NSubstitute")]
    public object NSubstitute_CreateMock()
    {
        var sub = Substitute.For<ICalculatorService>();
        return sub;
    }

    [Benchmark(Description = "FakeItEasy")]
    public object FakeItEasy_CreateMock()
    {
        var fake = A.Fake<ICalculatorService>();
        return fake;
    }

    [Benchmark(Description = "TUnit.Mocks (Repository)")]
    public object TUnitMocks_CreateMock_Repository()
    {
        var mock = Mock.Of<IUserRepository>();
        return mock.Object;
    }

    [Benchmark(Description = "Moq (Repository)")]
    public object Moq_CreateMock_Repository()
    {
        var mock = new Moq.Mock<IUserRepository>();
        return mock.Object;
    }

    [Benchmark(Description = "NSubstitute (Repository)")]
    public object NSubstitute_CreateMock_Repository()
    {
        var sub = Substitute.For<IUserRepository>();
        return sub;
    }

    [Benchmark(Description = "FakeItEasy (Repository)")]
    public object FakeItEasy_CreateMock_Repository()
    {
        var fake = A.Fake<IUserRepository>();
        return fake;
    }
}
