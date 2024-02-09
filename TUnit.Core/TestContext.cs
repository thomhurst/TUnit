namespace TUnit.Core;

public class TestContext
{
    public CancellationToken CancellationToken { get; internal set; } = CancellationToken.None;
    
    internal readonly StringWriter OutputWriter = new();
    
    private static readonly AsyncLocal<TestContext> AsyncLocal = new();

    public TestInformation TestInformation { get; }
    
    internal TestContext(TestInformation testInformation)
    {
        TestInformation = testInformation;
    }

    public static TestContext Current
    {
        get => AsyncLocal.Value!;
        set => AsyncLocal.Value = value;
    }

    public string? SkipReason { get; private set; }
    public void SkipTest(string reason)
    {
        SkipReason = reason;
    }
    
    public string? FailReason { get; private set; }
    public void FailTest(string reason)
    {
        FailReason = reason;
    }
    
    public TUnitTestResult? Result { get; internal set; }

    public string GetOutput()
    {
        return OutputWriter.ToString().Trim();
    }
}