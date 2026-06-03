namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Diagnostic ID constants for all TUnit assertions analyzer rules.
/// </summary>
/// <remarks>
/// Code fix providers (TUnit.Assertions.Analyzers.CodeFixers) MUST reference these constants instead
/// of <c>Rules.X.Id</c>. Constants are baked into the consuming assembly at compile time, so the
/// code fixers carry no runtime reference to the <see cref="Rules"/> type. A runtime reference
/// can bind against a stale TUnit.Assertions.Analyzers.dll already loaded in Visual Studio
/// (analyzers cannot be unloaded), throwing <see cref="System.MissingFieldException"/> for newly
/// added rules. See https://github.com/thomhurst/TUnit/issues/6157.
/// </remarks>
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
