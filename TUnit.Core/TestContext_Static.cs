using System.Reflection;

namespace TUnit.Core;

public partial class TestContext
{
    private static readonly AsyncLocal<TestContext?> TestContexts = new();
    public new static TestContext? Current
    {
        get => TestContexts.Value;
        internal set => TestContexts.Value = value;
    }

    internal static readonly Dictionary<string, string> InternalParametersDictionary = new();

    public static IReadOnlyDictionary<string, string> Parameters => InternalParametersDictionary;
    
    public static string? OutputDirectory
        => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
           ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    
    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
}