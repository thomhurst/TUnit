using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Helper class for determining test execution mode (source generation vs reflection).
/// </summary>
internal static class ExecutionModeHelper
{
    /// <summary>
    /// Determines whether to use source generation mode for test discovery and execution.
    /// </summary>
    /// <param name="commandLineOptions">Command line options from the test platform.</param>
    /// <returns>
    /// True if source generation mode should be used; false if reflection mode should be used.
    /// </returns>
    /// <remarks>
    /// Priority order:
    /// 1. AOT platform check (forces source generation)
    /// 2. Command line --reflection flag
    /// 3. Command line --tunit-execution-mode
    /// 4. Assembly-level [ReflectionMode] attribute
    /// 5. Environment variable TUNIT_EXECUTION_MODE
    /// 6. Default (SourceRegistrar.IsEnabled)
    /// </remarks>
    public static bool IsSourceGenerationMode(ICommandLineOptions commandLineOptions)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return true; // Force source generation on AOT platforms
        }
#endif

        if (commandLineOptions.TryGetOptionArgumentList(ReflectionModeCommandProvider.ReflectionMode, out _))
        {
            return false; // Reflection mode explicitly requested
        }

        // Check for command line option
        if (commandLineOptions.TryGetOptionArgumentList("tunit-execution-mode", out var modes) && modes.Length > 0)
        {
            var mode = modes[0].ToLowerInvariant();
            if (mode == "sourcegeneration" || mode == "aot")
            {
                return true;
            }
            else if (mode == "reflection")
            {
                return false;
            }
        }

        // Check for assembly-level ReflectionMode attribute
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var hasReflectionModeAttribute = entryAssembly.GetCustomAttributes(typeof(ReflectionModeAttribute), inherit: false).Length > 0;
            if (hasReflectionModeAttribute)
            {
                return false; // Assembly is marked for reflection mode
            }
        }

        // Check environment variable
        var envMode = EnvironmentVariableCache.Get("TUNIT_EXECUTION_MODE");
        if (!string.IsNullOrEmpty(envMode))
        {
            var mode = envMode!.ToLowerInvariant();
            if (mode == "sourcegeneration" || mode == "aot")
            {
                return true;
            }
            else if (mode == "reflection")
            {
                return false;
            }
        }

        return SourceRegistrar.IsEnabled;
    }
}
