using TUnit.Core.Enums;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of mode detector that determines execution mode based on runtime capabilities.
/// </summary>
public static class ModeDetector
{
    public static bool IsSourceGenerationAvailable => SourceRegistrar.IsEnabled;

    /// <summary>
    /// Static helper to get the current execution mode
    /// </summary>
    public static TestExecutionMode Mode => SourceRegistrar.IsEnabled ? TestExecutionMode.SourceGeneration : TestExecutionMode.Reflection;
}
