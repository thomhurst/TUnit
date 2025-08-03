using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Context for collecting and reporting diagnostics during source generation
/// </summary>
public class DiagnosticContext
{
    private readonly List<Diagnostic> _diagnostics =
    [
    ];
    private readonly SourceProductionContext _sourceProductionContext;

    public DiagnosticContext(SourceProductionContext sourceProductionContext)
    {
        _sourceProductionContext = sourceProductionContext;
    }

    /// <summary>
    /// Reports a diagnostic immediately
    /// </summary>
    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        _sourceProductionContext.ReportDiagnostic(diagnostic);
        _diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Creates and reports an error diagnostic
    /// </summary>
    public void ReportError(string id, string title, string message, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                title,
                message,
                "TUnit",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            location ?? Location.None);

        ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Creates and reports a warning diagnostic
    /// </summary>
    public void ReportWarning(string id, string title, string message, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                title,
                message,
                "TUnit",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            location ?? Location.None);

        ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Creates and reports an info diagnostic
    /// </summary>
    public void ReportInfo(string id, string title, string message, Location? location = null)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                title,
                message,
                "TUnit",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true),
            location ?? Location.None);

        ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Gets all diagnostics that have been reported
    /// </summary>
    public IReadOnlyList<Diagnostic> GetDiagnostics() => _diagnostics.AsReadOnly();

    /// <summary>
    /// Checks if any errors have been reported
    /// </summary>
    public bool HasErrors => _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error);
}
