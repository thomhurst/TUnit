using System.Reflection;

namespace TUnit.Core;

public class TestContext : IDisposable
{
    internal EventHandler? OnDispose;

    internal CancellationTokenSource? CancellationTokenSource { get; set; }
    
    public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? default;

    internal readonly StringWriter OutputWriter = new();
    
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