using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers;

internal static class Rules
{
    private const string UsageCategory = "Usage";

    public static readonly DiagnosticDescriptor MixAndOrConditionsAssertion =
        CreateDescriptor("TUnitAnalyzers0001", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor AwaitAssertion =
        CreateDescriptor("TUnitAnalyzers0002", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor CompilerArgumentsPopulated =
        CreateDescriptor("TUnitAnalyzers0004", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor DisposableUsingMultiple =
        CreateDescriptor("TUnitAnalyzers0004", UsageCategory, DiagnosticSeverity.Error);
        
    public static readonly DiagnosticDescriptor ConstantValueInAssertThat =
        CreateDescriptor("TUnitAnalyzers0005", UsageCategory, DiagnosticSeverity.Warning);
        
    public static readonly DiagnosticDescriptor ObjectEqualsBaseMethod =
        CreateDescriptor("TUnitAnalyzers0006", UsageCategory, DiagnosticSeverity.Error);


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