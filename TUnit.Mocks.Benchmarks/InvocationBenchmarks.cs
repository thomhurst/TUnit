using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Moq;
using NSubstitute;

namespace TUnit.Mocks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
public class InvocationBenchmarks
{
    private Mock<ICalculatorService>? _tunitMock;
    private ICalculatorService? _tunitObject;
    private Moq.Mock<ICalculatorService>? _moqMock;
    private ICalculatorService? _moqObject;
    private ICalculatorService? _nsubObject;
    private ICalculatorService? _fakeObject;

    [GlobalSetup]
    public void Setup()
    {
        // TUnit.Mocks
        _tunitMock = Mock.Of<ICalculatorService>();
        _tunitMock.Add(TUnitArg.Any<int>(), TUnitArg.Any<int>()).Returns(42);
        _tunitMock.Format(TUnitArg.Any<int>()).Returns("formatted");
        _tunitObject = _tunitMock.Object;

        // Moq
        _moqMock = new Moq.Mock<ICalculatorService>();
        _moqMock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
        _moqMock.Setup(x => x.Format(It.IsAny<int>())).Returns("formatted");
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

    [Benchmark(Description = "TUnit.Mocks (100 calls)")]
    public int TUnitMocks_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _tunitObject!.Add(i, i);
        }

        return sum;
    }

    [Benchmark(Description = "Moq (100 calls)")]
    public int Moq_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _moqObject!.Add(i, i);
        }

        return sum;
    }

    [Benchmark(Description = "NSubstitute (100 calls)")]
    public int NSubstitute_ManyInvocations()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += _nsubObject!.Add(i, i);
        }

        return sum;
    }

    [Benchmark(Description = "FakeItEasy (100 calls)")]
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
