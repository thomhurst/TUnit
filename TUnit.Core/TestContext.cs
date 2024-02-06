namespace TUnit.Core;

public class TestContext
{
    public CancellationToken CancellationToken { get; internal set; } = CancellationToken.None;
    
    internal readonly StringWriter OutputWriter = new();
    
    private readonly List<object> _assertions = []; 
    
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

    public void StoreObject(object obj)
    {
        _assertions.Add(obj);
    }
    
    public void RemoveObject(object obj)
    {
        _assertions.Remove(obj);
    }
    
    public void ClearObjects<T>()
    {
        _assertions.RemoveAll(x => x is T);
    }
    
    public IReadOnlyList<T> GetObjects<T>()
    {
        return _assertions.OfType<T>().ToArray();
    }
}