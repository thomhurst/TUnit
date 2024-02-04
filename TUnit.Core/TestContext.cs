using System.Text;

namespace TUnit.Core;

public class TestContext
{
    public CancellationToken CancellationToken { get; internal set; } = CancellationToken.None;
    internal readonly StringWriter OutputWriter = new();
    
    private readonly TestDetails _testDetails;
    private readonly object? _classInstance;

    private static readonly AsyncLocal<TestContext> AsyncLocal = new();

    public TestInformation TestInformation { get; }
    
    internal TestContext(TestDetails testDetails, object? classInstance)
    {
        _testDetails = testDetails;
        _classInstance = classInstance;
        TestInformation = new(_testDetails, _classInstance);
    }

    public static TestContext Current
    {
        get => AsyncLocal.Value!;
        set => AsyncLocal.Value = value;
    }
    
    public TUnitTestResult? Result { get; internal set; }

    public string GetOutput()
    {
        return OutputWriter.ToString().Trim();
    }
}