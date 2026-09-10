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
public class MockCreationBenchmarks
{
    [Benchmark(Description = "TUnit.Mocks")]
    public object TUnitMocks_CreateMock()
    {
        var mock = ICalculatorService.Mock();
        return mock.Object;
    }

    [Benchmark(Description = "Imposter")]
    public object Imposter_CreateMock()
    {
        var imposter = ICalculatorService.Imposter();
        return imposter.Instance();
    }

    [Benchmark(Description = "Mockolate")]
    public object Mockolate_CreateMock()
    {
        var sut = ICalculatorService.CreateMock();
        return sut;
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
        var mock = IUserRepository.Mock();
        return mock.Object;
    }

    [Benchmark(Description = "Imposter (Repository)")]
    public object Imposter_CreateMock_Repository()
    {
        var imposter = IUserRepository.Imposter();
        return imposter.Instance();
    }

    [Benchmark(Description = "Mockolate (Repository)")]
    public object Mockolate_CreateMock_Repository()
    {
        var sut = IUserRepository.CreateMock();
        return sut;
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
