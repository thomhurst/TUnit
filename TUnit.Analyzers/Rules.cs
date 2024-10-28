using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

public static class Rules
{
    private const string UsageCategory = "Usage";
    
    public static readonly DiagnosticDescriptor WrongArgumentTypeTestData =
        CreateDescriptor("TUnit0001", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoTestDataProvided =
        CreateDescriptor("TUnit0002", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoTestDataSourceProvided =
        CreateDescriptor("TUnit0003", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoMethodFound =
        CreateDescriptor("TUnit0004", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodParameterBadNullability =
        CreateDescriptor("TUnit0005", UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor WrongArgumentTypeTestDataSource =
        CreateDescriptor("TUnit0006", UsageCategory, DiagnosticSeverity.Error);
    
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
    
    public static readonly DiagnosticDescriptor NoArgumentInTestMethod =
        CreateDescriptor("TUnit0012", UsageCategory, DiagnosticSeverity.Error);

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
    
    public static readonly DiagnosticDescriptor RequiredPair_Attributes_DataDrivenTest_Arguments =
        CreateDescriptor("TUnit0020", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor RequiredCombinations_Attributes_DataSourceDrivenTest_MethodData_EnumerableMethodData_ClassData =
        CreateDescriptor("TUnit0021", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor RequiredPair_Attributes_MatrixTest_Matrix =
        CreateDescriptor("TUnit0022", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor Dispose_Member_In_Cleanup =
        CreateDescriptor("TUnit0023", UsageCategory, DiagnosticSeverity.Warning);
    
    public static readonly DiagnosticDescriptor Wrong_Category_Attribute =
        CreateDescriptor("TUnit0024", UsageCategory, DiagnosticSeverity.Error);
        
    public static readonly DiagnosticDescriptor Argument_Count_Not_Matching_Parameter_Count =
        CreateDescriptor("TUnit0025", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NotIEnumerable =
        CreateDescriptor("TUnit0026", UsageCategory, DiagnosticSeverity.Error);
    
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
    
    public static readonly DiagnosticDescriptor NoMatchingParameterClassDataSource =
        CreateDescriptor("TUnit0035", UsageCategory, DiagnosticSeverity.Error);
    
    public static readonly DiagnosticDescriptor TypeMustBePublic =
        CreateDescriptor("TUnit0036", UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor ConstructorMustBeParameterless =
        CreateDescriptor("TUnit0037", UsageCategory, DiagnosticSeverity.Error);
    
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