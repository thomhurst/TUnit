using Microsoft.CodeAnalysis;

namespace TUnit.AspNetCore.Analyzers;

public static class Rules
{
    private const string UsageCategory = "Usage";

    public static readonly DiagnosticDescriptor FactoryAccessedTooEarly =
        CreateDescriptor("TUnit0062", UsageCategory, DiagnosticSeverity.Error);

    private static DiagnosticDescriptor CreateDescriptor(string diagnosticId, string category, DiagnosticSeverity severity)
    {
        return new DiagnosticDescriptor(
            id: diagnosticId,
            title: new LocalizableResourceString(diagnosticId + "Title",
                Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString(diagnosticId + "MessageFormat", Resources.ResourceManager,
                typeof(Resources)),
            category: category,
            defaultSeverity: severity,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(diagnosticId + "Description", Resources.ResourceManager,
                typeof(Resources))
        );
    }
}
