using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

internal static class Rules
{
    private const string UsageCategory = "Usage";
    
    public static readonly DiagnosticDescriptor WrongArgumentTypeTestData =
        CreateDescriptor("TUnit0001", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoTestDataProvided =
        CreateDescriptor("TUnit0002", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoTestDataSourceProvided =
        CreateDescriptor("TUnit0003", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoDataSourceMethodFound =
        CreateDescriptor("TUnit0004", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodParameterBadNullability =
        CreateDescriptor("TUnit0005", UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor WrongArgumentTypeTestDataSource =
        CreateDescriptor("TUnit0006", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodNotStatic =
        CreateDescriptor("TUnit0007", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodNotPublic =
        CreateDescriptor("TUnit0008", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodAbstract =
        CreateDescriptor("TUnit0009", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodNotParameterless =
        CreateDescriptor("TUnit0010", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodNotReturnsNothing =
        CreateDescriptor("TUnit0011", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor NoArgumentInTestMethod =
        CreateDescriptor("TUnit0012", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor TooManyArgumentsInTestMethod =
        CreateDescriptor("TUnit0013", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor ConflictingTestAttributes =
        CreateDescriptor("TUnit0014", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MissingTimeoutCancellationTokenAttributes =
        CreateDescriptor("TUnit0015", UsageCategory, DiagnosticSeverity.Error);

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