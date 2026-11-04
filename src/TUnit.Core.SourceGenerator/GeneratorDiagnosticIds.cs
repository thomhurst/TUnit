namespace TUnit.SourceGeneration;

// Shared by the core and assertion generators. Never reuse an allocated ID for another rule.
// Keep docs/docs/reference/generator-diagnostics.md in sync when adding a rule.
internal static class GeneratorDiagnosticIds
{
    internal const string AssertionMethodNotFound = "TU0001";
    internal const string HookMetadataGenerationFailed = "THG001";
    internal const string TestMetadataGenerationFailed = "TUNIT0999";
    internal const string PerClassTestSourceGenerationFailed = "TUNIT0998";
    internal const string DynamicTestGenerationFailed = "TUnit0000";
    internal const string AotConverterFailed = "TUNITGEN001";
    internal const string NullConversionType = "TUNITGEN002";
    internal const string ConversionTypeCheckFailed = "TUNITGEN003";
    internal const string RefStructRequiresInlining = "TUNITGEN004";
    internal const string InaccessibleStaticPropertyContainingType = "TUNITGEN005";
    internal const string AssertionMethodMustBeStatic = "TUNITGEN006";
    internal const string AssertionMethodMustHaveParameters = "TUNITGEN007";
    internal const string AssertionUnsupportedReturnType = "TUNITGEN008";
}
