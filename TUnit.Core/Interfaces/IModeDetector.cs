using TUnit.Core.Enums;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Detects the appropriate test execution mode based on runtime capabilities and configuration.
/// </summary>
public interface IModeDetector
{
    /// <summary>
    /// Determines the test execution mode based on runtime capabilities and configuration.
    /// </summary>
    /// <returns>The detected execution mode</returns>
    TestExecutionMode DetectMode();
    
    /// <summary>
    /// Gets whether source generation is available and enabled.
    /// </summary>
    bool IsSourceGenerationAvailable { get; }
    
    /// <summary>
    /// Gets whether dynamic code generation is supported by the runtime.
    /// </summary>
    bool IsDynamicCodeSupported { get; }
    
    /// <summary>
    /// Gets whether reflection mode was explicitly requested via command line.
    /// </summary>
    bool IsReflectionModeRequested { get; }
}