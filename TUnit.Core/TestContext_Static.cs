using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    private static readonly AsyncLocal<TestContext?> TestContexts = new();

    /// <summary>
    /// Gets or sets the current test context.
    /// </summary>
    public new static TestContext? Current
    {
        get => TestContexts.Value;
        internal set => TestContexts.Value = value;
    }

    internal static readonly Dictionary<string, string> InternalParametersDictionary = new();

    /// <summary>
    /// Gets the parameters for the test context.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Parameters => InternalParametersDictionary;

    /// <summary>
    /// Gets or sets the configuration for the test context.
    /// </summary>
    public static IConfiguration Configuration { get; internal set; } = null!;
    
    /// <summary>
    /// Gets the output directory for the test context.
    /// </summary>
    public static string? OutputDirectory
    {
        [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Dynamic code check implemented")]
        get
        {
#if NET
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                return AppContext.BaseDirectory;
            }
#endif

            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                   ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        }
    }

    /// <summary>
    /// Gets or sets the working directory for the test context.
    /// </summary>
    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
}