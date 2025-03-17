using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

public static class Rules
{
    private const string UsageCategory = "Usage";
    
    public static readonly DiagnosticDescriptor WrongArgumentTypeTestData =
        CreateDescriptor("TUnit0001", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoTestDataProvided =
        CreateDescriptor("TUnit0002", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoMethodFound =
        CreateDescriptor("TUnit0004", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodParameterBadNullability =
        CreateDescriptor("TUnit0005", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor MethodMustBeStatic =
        CreateDescriptor("TUnit0007", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodMustBePublic =
        CreateDescriptor("TUnit0008", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodMustNotBeAbstract =
        CreateDescriptor("TUnit0009", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodMustBeParameterless =
        CreateDescriptor("TUnit0010", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodMustReturnData =
        CreateDescriptor("TUnit0011", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor TooManyArgumentsInTestMethod =
        CreateDescriptor("TUnit0013", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor PublicMethodMissingTestAttribute =
        CreateDescriptor("TUnit0014", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor MissingTimeoutCancellationTokenAttributes =
        CreateDescriptor("TUnit0015", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MethodMustNotBeStatic =
        CreateDescriptor("TUnit0016", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor ConflictingExplicitAttributes =
        CreateDescriptor("TUnit0017", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor InstanceAssignmentInTestClass =
        CreateDescriptor("TUnit0018", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor MissingTestAttribute =
        CreateDescriptor("TUnit0019", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor Dispose_Member_In_Cleanup =
        CreateDescriptor("TUnit0023", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor UnknownParameters =
        CreateDescriptor("TUnit0027", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor DoNotOverrideAttributeUsageMetadata =
        CreateDescriptor("TUnit0028", UsageCategory, DiagnosticSeverity.Error);
        
    public static readonly DiagnosticDescriptor DuplicateSingleAttribute =
        CreateDescriptor("TUnit0029", UsageCategory, DiagnosticSeverity.Error);
            
    public static readonly DiagnosticDescriptor DoesNotInheritTestsWarning =
        CreateDescriptor("TUnit0030", UsageCategory, DiagnosticSeverity.Warning);
                
    public static readonly DiagnosticDescriptor AsyncVoidMethod =
        CreateDescriptor("TUnit0031", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor DependsOnNotInParallelConflict =
        CreateDescriptor("TUnit0032", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor DependsOnConflicts =
        CreateDescriptor("TUnit0033", UsageCategory, DiagnosticSeverity.Error);
        
    public static readonly DiagnosticDescriptor NoMainMethod =
        CreateDescriptor("TUnit0034", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor NoDataSourceProvided =
        CreateDescriptor("TUnit0038", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor SingleTestContextParameterRequired =
        CreateDescriptor("TUnit0039", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor SingleClassHookContextParameterRequired =
        CreateDescriptor("TUnit0040", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SingleAssemblyHookContextParameterRequired =
        CreateDescriptor("TUnit0041", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor GlobalHooksSeparateClass =
        CreateDescriptor("TUnit0042", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor PropertyRequiredNotSet =
        CreateDescriptor("TUnit0043", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor MustHavePropertySetter =
        CreateDescriptor("TUnit0044", UsageCategory, DiagnosticSeverity.Error);
        
    public static readonly DiagnosticDescriptor TooManyDataAttributes =
        CreateDescriptor("TUnit0045", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor ReturnFunc =
        CreateDescriptor("TUnit0046", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor AsyncLocalCallFlowValues =
        CreateDescriptor("TUnit0047", UsageCategory, DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor InstanceTestMethod =
        CreateDescriptor("TUnit0048", UsageCategory, DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MatrixDataSourceAttributeRequired =
        CreateDescriptor("TUnit0049", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor TooManyArguments =
        CreateDescriptor("TUnit0050", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor TypeMustBePublic =
        CreateDescriptor("TUnit0051", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor XunitAttributes =
        CreateDescriptor("TUnit0052", UsageCategory, DiagnosticSeverity.Info);
    
    public static readonly DiagnosticDescriptor XunitClassFixtures =
        CreateDescriptor("TUnit0053", UsageCategory, DiagnosticSeverity.Info);
    
    public static readonly DiagnosticDescriptor XunitUsingDirectives =
        CreateDescriptor("TUnit0054", UsageCategory, DiagnosticSeverity.Info);
    
    public static readonly DiagnosticDescriptor OverwriteConsole =
        CreateDescriptor("TUnit0055", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor InstanceMethodSource =
        CreateDescriptor("TUnit0056", UsageCategory, DiagnosticSeverity.Error);
    
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