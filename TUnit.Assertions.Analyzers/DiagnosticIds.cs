namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Diagnostic ID constants for all TUnit assertions analyzer rules.
/// Code fix providers MUST reference these constants instead of <c>Rules.X.Id</c> — consts are
/// baked into the consuming IL at compile time, avoiding a runtime bind against a stale
/// analyzer assembly in Visual Studio. See https://github.com/thomhurst/TUnit/issues/6157.
/// Members MUST stay <c>const</c>: <c>static readonly</c> would reintroduce the runtime
/// reference (and fails the IL regression tests).
/// </summary>
public static class DiagnosticIds
{
    public const string MixAndOrConditionsAssertion = "TUnitAssertions0001";
    public const string AwaitAssertion = "TUnitAssertions0002";
    public const string CompilerArgumentsPopulated = "TUnitAssertions0003";
    public const string DisposableUsingMultiple = "TUnitAssertions0004";
    public const string ConstantValueInAssertThat = "TUnitAssertions0005";
    public const string ObjectEqualsBaseMethod = "TUnitAssertions0006";
    public const string DynamicValueInAssertThat = "TUnitAssertions0007";
    public const string AwaitValueTaskInAssertThat = "TUnitAssertions0008";
    public const string XUnitAssertion = "TUnitAssertions0009";
    public const string GenerateAssertionMethodMustBeStatic = "TUnitAssertions0010";
    public const string GenerateAssertionMethodMustHaveParameter = "TUnitAssertions0011";
    public const string GenerateAssertionInvalidReturnType = "TUnitAssertions0012";
    public const string GenerateAssertionShouldBeExtensionMethod = "TUnitAssertions0013";
    public const string PreferIsNullOverIsEqualToNull = "TUnitAssertions0014";
    public const string PreferIsTrueOrIsFalseOverIsEqualToBool = "TUnitAssertions0015";
    public const string CollectionIsEqualToUsesReferenceEquality = "TUnitAssertions0016";
}
