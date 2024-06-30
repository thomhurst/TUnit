using System.Reflection;

namespace TUnit.Core;

public partial class TestContext
{
    internal static readonly AsyncLocal<TestContext> TestContexts = new();
    public static TestContext? Current => TestContexts.Value;
    
    public static string OutputDirectory
        => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
           ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    
    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
}