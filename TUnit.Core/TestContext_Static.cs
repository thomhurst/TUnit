using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

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

    public static IConfiguration Configuration { get; internal set; } = null!;
    
    public static string? OutputDirectory
    {
        [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Dynamic code check implemented")]
        get
        {
#if NET
            
            if (RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                       ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            return AppContext.BaseDirectory;
        }
    }

    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
}