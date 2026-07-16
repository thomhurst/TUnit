using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers;

internal static class Rules
{
    private const string UsageCategory = "Usage";

    public static readonly DiagnosticDescriptor MixAndOrConditionsAssertion =
        CreateDescriptor(DiagnosticIds.MixAndOrConditionsAssertion, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor AwaitAssertion =
        CreateDescriptor(DiagnosticIds.AwaitAssertion, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor CompilerArgumentsPopulated =
        CreateDescriptor(DiagnosticIds.CompilerArgumentsPopulated, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor DisposableUsingMultiple =
        CreateDescriptor(DiagnosticIds.DisposableUsingMultiple, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor ConstantValueInAssertThat =
        CreateDescriptor(DiagnosticIds.ConstantValueInAssertThat, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor ObjectEqualsBaseMethod =
        CreateDescriptor(DiagnosticIds.ObjectEqualsBaseMethod, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor DynamicValueInAssertThat =
        CreateDescriptor(DiagnosticIds.DynamicValueInAssertThat, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor AwaitValueTaskInAssertThat =
        CreateDescriptor(DiagnosticIds.AwaitValueTaskInAssertThat, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor XUnitAssertion =
        CreateDescriptor(DiagnosticIds.XUnitAssertion, UsageCategory, DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor GenerateAssertionMethodMustBeStatic =
        CreateDescriptor(DiagnosticIds.GenerateAssertionMethodMustBeStatic, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor GenerateAssertionMethodMustHaveParameter =
        CreateDescriptor(DiagnosticIds.GenerateAssertionMethodMustHaveParameter, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor GenerateAssertionInvalidReturnType =
        CreateDescriptor(DiagnosticIds.GenerateAssertionInvalidReturnType, UsageCategory, DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor GenerateAssertionShouldBeExtensionMethod =
        CreateDescriptor(DiagnosticIds.GenerateAssertionShouldBeExtensionMethod, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor PreferIsNullOverIsEqualToNull =
        CreateDescriptor(DiagnosticIds.PreferIsNullOverIsEqualToNull, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor PreferIsTrueOrIsFalseOverIsEqualToBool =
        CreateDescriptor(DiagnosticIds.PreferIsTrueOrIsFalseOverIsEqualToBool, UsageCategory, DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor CollectionIsEqualToUsesReferenceEquality =
        CreateDescriptor(DiagnosticIds.CollectionIsEqualToUsesReferenceEquality, UsageCategory, DiagnosticSeverity.Info);

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
