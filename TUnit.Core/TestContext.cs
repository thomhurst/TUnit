using System.Reflection;

namespace TUnit.Core;

public class TestContext : IDisposable
{
    internal EventHandler? OnDispose;
    private CancellationTokenSource? _cancellationTokenSource;
    
    internal readonly StringWriter OutputWriter = new();

    internal CancellationTokenSource? CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = value;
        }
    }

    public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? default;


    public TestInformation TestInformation { get; }

    public Dictionary<string, object> ObjectBag { get; } = new();

    public TestContext(TestInformation testInformation)
    {
        TestInformation = testInformation;
    }

    public static TestContext? Current => TestDictionary.TestContexts.Value;
    
    public TUnitTestResult? Result { get; internal set; }

    public string GetConsoleOutput()
    {
        return OutputWriter.ToString().Trim();
    }

    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
        OutputWriter.Dispose();
        CancellationTokenSource?.Dispose();
    }

    public static string OutputDirectory
        => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
           ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    
    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
}