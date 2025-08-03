namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of mode detector that determines execution mode based on runtime capabilities.
/// </summary>
public static class ModeDetector
{
    public static bool IsSourceGenerationAvailable => SourceRegistrar.IsEnabled;
}
