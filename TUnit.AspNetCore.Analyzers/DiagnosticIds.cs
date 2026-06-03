namespace TUnit.AspNetCore.Analyzers;

/// <summary>
/// Diagnostic ID constants for all TUnit ASP.NET Core analyzer rules.
/// Code fix providers MUST reference these constants instead of <c>Rules.X.Id</c> — consts are
/// baked into the consuming IL at compile time, avoiding a runtime bind against a stale
/// analyzer assembly in Visual Studio. See https://github.com/thomhurst/TUnit/issues/6157.
/// Members MUST stay <c>const</c>: <c>static readonly</c> would reintroduce the runtime
/// reference (and fails the IL regression tests).
/// </summary>
public static class DiagnosticIds
{
    public const string FactoryAccessedTooEarly = "TUnit0062";
    public const string GlobalFactoryMemberAccess = "TUnit0063";
    public const string DirectWebApplicationFactoryInheritance = "TUnit0064";
}
