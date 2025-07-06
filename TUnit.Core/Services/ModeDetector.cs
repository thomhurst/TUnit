using System.Runtime.CompilerServices;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of mode detector that determines execution mode based on runtime capabilities.
/// </summary>
public class ModeDetector : IModeDetector
{
    private readonly bool _isReflectionModeRequested;

    public ModeDetector(bool isReflectionModeRequested = false)
    {
        _isReflectionModeRequested = isReflectionModeRequested;
    }

    /// <inheritdoc />
    public TestExecutionMode DetectMode()
    {
        // Priority 1: If reflection is explicitly requested, use reflection mode
        if (IsReflectionModeRequested)
        {
            return TestExecutionMode.Reflection;
        }

        // Priority 2: If dynamic code is not supported (AOT scenarios), must use source generation
        if (!IsDynamicCodeSupported)
        {
            if (!IsSourceGenerationAvailable)
            {
                throw new NotSupportedException(
                    "AOT runtime detected but source generation is not available. " +
                    "Ensure TUnit source generators are properly configured.");
            }
            return TestExecutionMode.SourceGeneration;
        }

        // Priority 3: If source generation is available and enabled, prefer it for performance
        if (IsSourceGenerationAvailable)
        {
            return TestExecutionMode.SourceGeneration;
        }

        // Priority 4: Fall back to reflection mode
        return TestExecutionMode.Reflection;
    }

    /// <inheritdoc />
    public bool IsSourceGenerationAvailable => SourceRegistrar.IsEnabled;

    /// <inheritdoc />
    public bool IsDynamicCodeSupported
    {
        get
        {
#if NET
            return RuntimeFeature.IsDynamicCodeSupported;
#else
            return true;
#endif
        }
    }

    /// <inheritdoc />
    public bool IsReflectionModeRequested => _isReflectionModeRequested;

    /// <summary>
    /// Static helper to get the current execution mode
    /// </summary>
    public static TestExecutionMode Mode
    {
        get
        {
            // Check environment variable first
            var envMode = Environment.GetEnvironmentVariable("TUNIT_EXECUTION_MODE");
            if (!string.IsNullOrEmpty(envMode) &&
                Enum.TryParse<TestExecutionMode>(envMode, ignoreCase: true, out var mode))
            {
                return mode;
            }

            // Use default detector logic
            var detector = new ModeDetector();
            return detector.DetectMode();
        }
    }
}
