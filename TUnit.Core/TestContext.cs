using System.Text;

namespace TUnit.Core;

public class TestContext
{
    private readonly TestDetails _testDetails;
    private readonly object? _classInstance;
    private readonly StringBuilder _outputBuilder = new();

    private static readonly AsyncLocal<TestContext> AsyncLocal = new();

    public TestInformation TestInformation { get; }
    
    internal TestContext(TestDetails testDetails, object? classInstance)
    {
        _testDetails = testDetails;
        _classInstance = classInstance;
        TestInformation = new(_testDetails, _classInstance);
    }

    internal void Write(char c)
    {
        _outputBuilder.Append(c);
    }

    public static TestContext Current
    {
        get => AsyncLocal.Value!;
        set => AsyncLocal.Value = value;
    }
    
    public bool HasFailed { get; init; }
    public TUnitTestResult? Result { get; internal set; }

    public string GetOutput()
    {
        return _outputBuilder.ToString();
    }
}