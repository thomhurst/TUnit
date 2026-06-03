using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

public static class Rules
{
    private const string UsageCategory = "Usage";

    public static readonly DiagnosticDescriptor WrongArgumentTypeTestData =
        CreateDescriptor(DiagnosticIds.WrongArgumentTypeTestData, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoTestDataProvided =
        CreateDescriptor(DiagnosticIds.NoTestDataProvided, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoMethodFound =
        CreateDescriptor(DiagnosticIds.NoMethodFound, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodParameterBadNullability =
        CreateDescriptor(DiagnosticIds.MethodParameterBadNullability, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor MethodMustBeStatic =
        CreateDescriptor(DiagnosticIds.MethodMustBeStatic, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodMustBePublic =
        CreateDescriptor(DiagnosticIds.MethodMustBePublic, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodMustNotBeAbstract =
        CreateDescriptor(DiagnosticIds.MethodMustNotBeAbstract, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodMustBeParameterless =
        CreateDescriptor(DiagnosticIds.MethodMustBeParameterless, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MethodMustReturnData =
        CreateDescriptor(DiagnosticIds.MethodMustReturnData, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor TooManyArgumentsInTestMethod =
        CreateDescriptor(DiagnosticIds.TooManyArgumentsInTestMethod, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor PublicMethodMissingTestAttribute =
        CreateDescriptor(DiagnosticIds.PublicMethodMissingTestAttribute, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor MissingTimeoutCancellationTokenAttributes =
        CreateDescriptor(DiagnosticIds.MissingTimeoutCancellationTokenAttributes, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor CancellationTokenMustBeLastParameter =
        CreateDescriptor(DiagnosticIds.CancellationTokenMustBeLastParameter, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor MethodMustNotBeStatic =
        CreateDescriptor(DiagnosticIds.MethodMustNotBeStatic, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor ConflictingExplicitAttributes =
        CreateDescriptor(DiagnosticIds.ConflictingExplicitAttributes, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor InstanceAssignmentInTestClass =
        CreateDescriptor(DiagnosticIds.InstanceAssignmentInTestClass, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor MissingTestAttribute =
        CreateDescriptor(DiagnosticIds.MissingTestAttribute, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor Dispose_Member_In_Cleanup =
        CreateDescriptor(DiagnosticIds.Dispose_Member_In_Cleanup, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor UnknownParameters =
        CreateDescriptor(DiagnosticIds.UnknownParameters, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor DoNotOverrideAttributeUsageMetadata =
        CreateDescriptor(DiagnosticIds.DoNotOverrideAttributeUsageMetadata, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor DuplicateSingleAttribute =
        CreateDescriptor(DiagnosticIds.DuplicateSingleAttribute, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor DoesNotInheritTestsWarning =
        CreateDescriptor(DiagnosticIds.DoesNotInheritTestsWarning, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor AsyncVoidMethod =
        CreateDescriptor(DiagnosticIds.AsyncVoidMethod, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor DependsOnConflicts =
        CreateDescriptor(DiagnosticIds.DependsOnConflicts, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoMainMethod =
        CreateDescriptor(DiagnosticIds.NoMainMethod, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NoDataSourceProvided =
        CreateDescriptor(DiagnosticIds.NoDataSourceProvided, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SingleTestContextParameterRequired =
        CreateDescriptor(DiagnosticIds.SingleTestContextParameterRequired, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SingleClassHookContextParameterRequired =
        CreateDescriptor(DiagnosticIds.SingleClassHookContextParameterRequired, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor SingleAssemblyHookContextParameterRequired =
        CreateDescriptor(DiagnosticIds.SingleAssemblyHookContextParameterRequired, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor GlobalHooksSeparateClass =
        CreateDescriptor(DiagnosticIds.GlobalHooksSeparateClass, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor PropertyRequiredNotSet =
        CreateDescriptor(DiagnosticIds.PropertyRequiredNotSet, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor MustHavePropertySetter =
        CreateDescriptor(DiagnosticIds.MustHavePropertySetter, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor TooManyDataAttributes =
        CreateDescriptor(DiagnosticIds.TooManyDataAttributes, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor ReturnFunc =
        CreateDescriptor(DiagnosticIds.ReturnFunc, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor AsyncLocalCallFlowValues =
        CreateDescriptor(DiagnosticIds.AsyncLocalCallFlowValues, UsageCategory, DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor InstanceTestMethod =
        CreateDescriptor(DiagnosticIds.InstanceTestMethod, UsageCategory, DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MatrixDataSourceAttributeRequired =
        CreateDescriptor(DiagnosticIds.MatrixDataSourceAttributeRequired, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor TooManyArguments =
        CreateDescriptor(DiagnosticIds.TooManyArguments, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor TypeMustBePublic =
        CreateDescriptor(DiagnosticIds.TypeMustBePublic, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MultipleConstructorsWithoutTestConstructor =
        CreateDescriptor(DiagnosticIds.MultipleConstructorsWithoutTestConstructor, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor XunitMigration =
        CreateDescriptor(DiagnosticIds.XunitMigration, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor NUnitMigration =
        CreateDescriptor(DiagnosticIds.NUnitMigration, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor MSTestMigration =
        CreateDescriptor(DiagnosticIds.MSTestMigration, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor OverwriteConsole =
        CreateDescriptor(DiagnosticIds.OverwriteConsole, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor CombinedDataSourceAttributeRequired =
        CreateDescriptor(DiagnosticIds.CombinedDataSourceAttributeRequired, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor CombinedDataSourceMissingParameterDataSource =
        CreateDescriptor(DiagnosticIds.CombinedDataSourceMissingParameterDataSource, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor CombinedDataSourceConflictWithMatrix =
        CreateDescriptor(DiagnosticIds.CombinedDataSourceConflictWithMatrix, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor InstanceMethodSource =
        CreateDescriptor(DiagnosticIds.InstanceMethodSource, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor HookContextParameterOptional =
        CreateDescriptor(DiagnosticIds.HookContextParameterOptional, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor HookUnknownParameters =
        CreateDescriptor(DiagnosticIds.HookUnknownParameters, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor AbstractTestClassWithDataSources =
        CreateDescriptor(DiagnosticIds.AbstractTestClassWithDataSources, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor PotentialEmptyDataSource =
        CreateDescriptor(DiagnosticIds.PotentialEmptyDataSource, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor NoAccessibleConstructor =
        CreateDescriptor(DiagnosticIds.NoAccessibleConstructor, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor MissingPolyfillPackage =
        CreateDescriptor(DiagnosticIds.MissingPolyfillPackage, UsageCategory, DiagnosticSeverity.Error,
            customTags: [WellKnownDiagnosticTags.CompilationEnd],
            helpLinkUri: "https://www.nuget.org/packages/Polyfill");

    public static readonly DiagnosticDescriptor RedundantHookAttributeOnOverride =
        CreateDescriptor(DiagnosticIds.RedundantHookAttributeOnOverride, UsageCategory, DiagnosticSeverity.Error);

    private static DiagnosticDescriptor CreateDescriptor(string diagnosticId, string category, DiagnosticSeverity severity,
        string[]? customTags = null, string? helpLinkUri = null)
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
                typeof(Resources)),
            helpLinkUri: helpLinkUri,
            customTags: customTags ?? []
        );
    }
}
