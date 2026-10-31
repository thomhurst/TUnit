namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Diagnostic ID constants for the TUnit.Mocks analyzer rules.
/// Code fix providers MUST reference these constants instead of <c>Rules.X.Id</c> — consts are
/// baked into the consuming IL at compile time, avoiding a runtime bind against a stale
/// analyzer assembly in Visual Studio. See https://github.com/thomhurst/TUnit/issues/6157.
/// Members MUST stay <c>const</c>: <c>static readonly</c> would reintroduce the runtime
/// reference (and fails the IL regression tests).
/// </summary>
public static class DiagnosticIds
{
    public const string CannotMockSealedType = "TM001";
    public const string CannotMockValueType = "TM002";
    public const string OfDelegateRequiresDelegateType = "TM003";
    public const string RequiresCSharp14 = "TM004";
    public const string ArgIsNullNonNullableValueType = "TM005";
    public const string CannotMockTypeWithoutAccessibleConstructor = "TM006";
    public const string CannotMockInterfaceWithInaccessibleMember = "TM007";
    // TM008 and TM009 are reported by TUnit.Mocks.SourceGenerator (see its Diagnostics.cs).
    public const string PreferStaticMockSyntax = "TM010";
}
