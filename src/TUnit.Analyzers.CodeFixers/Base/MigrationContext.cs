using Microsoft.CodeAnalysis.Text;

namespace TUnit.Analyzers.CodeFixers.Base;

/// <summary>
/// Tracks migration progress and failures during code fix execution.
/// Enables partial success by recording what failed without aborting the entire migration.
/// Failures are surfaced via TODO comments in the generated code.
/// </summary>
public class MigrationContext
{
    public List<MigrationFailure> Failures { get; } = new();

    /// <summary>
    /// Records a failure during a migration step.
    /// The failure will be reported as a TODO comment in the migrated file.
    /// </summary>
    public void RecordFailure(string step, Exception ex, TextSpan? span = null)
    {
        var failure = new MigrationFailure(
            Step: step,
            Description: ex.Message,
            Span: span,
            OriginalCode: null,
            StackTrace: ex.StackTrace);

        Failures.Add(failure);
    }

    /// <summary>
    /// Records a failure during item-level conversion (e.g., a specific assertion).
    /// Includes the original code snippet for context in the TODO comment.
    /// </summary>
    public void RecordItemFailure(string step, string originalCode, Exception ex, TextSpan? span = null)
    {
        var failure = new MigrationFailure(
            Step: step,
            Description: ex.Message,
            Span: span,
            OriginalCode: originalCode,
            StackTrace: ex.StackTrace);

        Failures.Add(failure);
    }

    /// <summary>
    /// Returns true if any failures were recorded.
    /// </summary>
    public bool HasFailures => Failures.Count > 0;
}

/// <summary>
/// Represents a single migration failure with context for debugging.
/// </summary>
public record MigrationFailure(
    string Step,
    string Description,
    TextSpan? Span = null,
    string? OriginalCode = null,
    string? StackTrace = null);
