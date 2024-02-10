using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

internal static class Rules
{
    private const string UsageCategory = "Usage";

    public static readonly DiagnosticDescriptor MixAndOrConditionsAssertion =
        MakeResult("TUnit0001", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor AwaitAssertion =
        MakeResult("TUnit0002", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor InvalidDataAssertion =
        MakeResult("TUnit0003", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoDataProvidedAssertion =
        MakeResult("TUnit0004", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor InvalidDataSourceAssertion =
        MakeResult("TUnit0005", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoDataSourceMethodFoundAssertion =
        MakeResult("TUnit0006", UsageCategory, DiagnosticSeverity.Error);

    private static DiagnosticDescriptor MakeResult(string diagnosticId, string category, DiagnosticSeverity severity)
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