using TUnit.Core.Logging;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines the core contextual capabilities for the TUnit testing framework.
/// </summary>
/// <remarks>
/// The <see cref="IContext"/> interface provides access to output writers and logging mechanisms used
/// throughout the TUnit testing pipeline. Implementations of this interface serve as a foundation
/// for various context types in the testing framework, including test contexts, hook contexts,
/// and discovery contexts.
/// </remarks>
public interface IContext
{
    /// <summary>
    /// Gets the standard output writer for the context.
    /// </summary>
    /// <remarks>
    /// The output writer captures standard output messages during test execution.
    /// These messages can be retrieved later for verification or reporting purposes.
    /// </remarks>
    /// <value>A <see cref="TextWriter"/> instance for writing standard output.</value>
    TextWriter OutputWriter { get; }
    
    /// <summary>
    /// Gets the error output writer for the context.
    /// </summary>
    /// <remarks>
    /// The error output writer captures error messages during test execution.
    /// These messages can be retrieved later for verification or reporting purposes.
    /// </remarks>
    /// <value>A <see cref="TextWriter"/> instance for writing error output.</value>
    TextWriter ErrorOutputWriter { get; }
    
    /// <summary>
    /// Gets the default logger for the context.
    /// </summary>
    /// <remarks>
    /// The default logger provides a unified logging mechanism for the test execution environment.
    /// It uses the context's output writers to record messages at various log levels, and supports
    /// structured logging with properties.
    /// </remarks>
    /// <returns>A <see cref="DefaultLogger"/> instance configured for this context.</returns>
    DefaultLogger GetDefaultLogger();
}