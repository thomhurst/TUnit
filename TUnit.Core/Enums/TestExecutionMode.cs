namespace TUnit.Core.Enums;

/// <summary>
/// Defines the test execution mode determining how tests are discovered and executed.
/// </summary>
public enum TestExecutionMode
{
    /// <summary>
    /// Source generation mode - 100% AOT-safe with compile-time test discovery.
    /// All test metadata, data sources, and execution logic are generated at compile-time.
    /// </summary>
    SourceGeneration,
    
    /// <summary>
    /// Reflection mode - Full runtime flexibility using reflection for test discovery and execution.
    /// Provides complete feature set but requires dynamic code generation capabilities.
    /// </summary>
    Reflection
}