using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate;
using Moq;
using NSubstitute;

namespace TUnit.Mocks.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BoundedInvocationConfig))]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
public class InvocationBenchmarks
{
    private Mock<ICalculatorService>? _tunitMock;
    private ICalculatorService? _tunitObject;
    private Moq.Mock<ICalculatorService>? _moqMock;
    private ICalculatorServiceImposter? _imposterMock;
    private ICalculatorService? _imposterObject;
    private ICalculatorService? _mockolateObject;
    private ICalculatorService? _moqObject;
    private ICalculatorService? _nsubObject;
    private ICalculatorService? _fakeObject;

    // Recreate matchers as well as mocks: clearing calls does not clear captured arguments.
    // Fixed invocation counts bound history independently of BenchmarkDotNet's pilot stage.
    [IterationSetup]
    public void Setup()
    {
        // TUnit.Mocks
        _tunitMock = ICalculatorService.Mock();
        _tunitMock.Add(TUnitArg.Any<int>(), TUnitArg.Any<int>()).Returns(42);
        _tunitMock.Format(TUnitArg.Any<int>()).Returns("formatted");
        _tunitObject = _tunitMock.Object;

        // Imposter
        _imposterMock = ICalculatorService.Imposter();
        _imposterMock.Add(Arg<int>.Any(), Arg<int>.Any()).Returns(42);
        _imposterMock.Format(Arg<int>.Any()).Returns("formatted");
        _imposterObject = _imposterMock.Instance();

        // Mockolate
        _mockolateObject = ICalculatorService.CreateMock();
        ((Mockolate.Mock.IMockForICalculatorService)_mockolateObject).Setup.Add(Mockolate.It.IsAny<int>(), Mockolate.It.IsAny<int>()).Returns(42);
        ((Mockolate.Mock.IMockForICalculatorService)_mockolateObject).Setup.Format(Mockolate.It.IsAny<int>()).Returns("formatted");

        // Moq
        _moqMock = new Moq.Mock<ICalculatorService>();
        _moqMock.Setup(x => x.Add(Moq.It.IsAny<int>(), Moq.It.IsAny<int>())).Returns(42);
        _moqMock.Setup(x => x.Format(Moq.It.IsAny<int>())).Returns("formatted");
        _moqObject = _moqMock.Object;

        // NSubstitute
        _nsubObject = Substitute.For<ICalculatorService>();
        _nsubObject.Add(NSubstitute.Arg.Any<int>(), NSubstitute.Arg.Any<int>()).Returns(42);
        _nsubObject.Format(NSubstitute.Arg.Any<int>()).Returns("formatted");

        // FakeItEasy
        _fakeObject = A.Fake<ICalculatorService>();
        A.CallTo(() => _fakeObject.Add(A<int>.Ignored, A<int>.Ignored)).Returns(42);
        A.CallTo(() => _fakeObject.Format(A<int>.Ignored)).Returns("formatted");
    }

    [Benchmark(Description = "TUnit.Mocks")]
    public int TUnitMocks_Invoke()
    {
        return _tunitObject!.Add(1, 2);
    }

    [Benchmark(Description = "Imposter")]
    public int Imposter_Invoke()
    {
        return _imposterObject!.Add(1, 2);
    }

    [Benchmark(Description = "Mockolate")]
    public int Mockolate_Invoke()
    {
        return _mockolateObject!.Add(1, 2);
    }

    [Benchmark(Description = "Moq")]
    public int Moq_Invoke()
    {
        return _moqObject!.Add(1, 2);
    }

    [Benchmark(Description = "NSubstitute")]
    public int NSubstitute_Invoke()
    {
        return _nsubObject!.Add(1, 2);
    }

    [Benchmark(Description = "FakeItEasy")]
    public int FakeItEasy_Invoke()
    {
        return _fakeObject!.Add(1, 2);
    }

    [Benchmark(Description = "TUnit.Mocks (String)")]
    public string TUnitMocks_InvokeString()
    {
        return _tunitObject!.Format(42);
    }

    [Benchmark(Description = "Imposter (String)")]
    public string Imposter_InvokeString()
    {
        return _imposterObject!.Format(42);
    }

    [Benchmark(Description = "Mockolate (String)")]
    public string Mockolate_InvokeString()
    {
        return _mockolateObject!.Format(42);
    }

    [Benchmark(Description = "Moq (String)")]
    public string Moq_InvokeString()
    {
        return _moqObject!.Format(42);
    }

    [Benchmark(Description = "NSubstitute (String)")]
    public string NSubstitute_InvokeString()
    {
        return _nsubObject!.Format(42);
    }

    [Benchmark(Description = "FakeItEasy (String)")]
    public string FakeItEasy_InvokeString()
    {
        return _fakeObject!.Format(42);
    }

    [InvocationCount(256, unrollFactor: 1)]
    [Benchmark(Description = "TUnit.Mocks (100 calls)", OperationsPerInvoke = 100)]
    public int TUnitMocks_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _tunitObject!.Add(i, i);
        }

        return sum;
    }

    [InvocationCount(256, unrollFactor: 1)]
    [Benchmark(Description = "Imposter (100 calls)", OperationsPerInvoke = 100)]
    public int Imposter_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _imposterObject!.Add(i, i);
        }

        return sum;
    }

    [InvocationCount(256, unrollFactor: 1)]
    [Benchmark(Description = "Mockolate (100 calls)", OperationsPerInvoke = 100)]
    public int Mockolate_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _mockolateObject!.Add(i, i);
        }

        return sum;
    }

    [InvocationCount(256, unrollFactor: 1)]
    [Benchmark(Description = "Moq (100 calls)", OperationsPerInvoke = 100)]
    public int Moq_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _moqObject!.Add(i, i);
        }

        return sum;
    }

    [InvocationCount(256, unrollFactor: 1)]
    [Benchmark(Description = "NSubstitute (100 calls)", OperationsPerInvoke = 100)]
    public int NSubstitute_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _nsubObject!.Add(i, i);
        }

        return sum;
    }

    [InvocationCount(256, unrollFactor: 1)]
    [Benchmark(Description = "FakeItEasy (100 calls)", OperationsPerInvoke = 100)]
    public int FakeItEasy_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _fakeObject!.Add(i, i);
        }

        return sum;
    }
}

public sealed class BoundedInvocationConfig : ManualConfig
{
    public BoundedInvocationConfig()
    {
        AddJob(Job.ShortRun.WithInvocationCount(16384).WithUnrollFactor(1));
    }
}
