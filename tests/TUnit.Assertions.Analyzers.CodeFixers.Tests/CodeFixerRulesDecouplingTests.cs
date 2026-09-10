using TUnit.Tests.Shared;

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests;

/// <summary>
/// Code fixers must use <c>DiagnosticIds</c> constants, never <c>Rules.X</c> — see
/// <see cref="RulesDecouplingVerifier"/> for the full rationale (issue #6157).
/// </summary>
public class CodeFixerRulesDecouplingTests
{
    [Test]
    public async Task CodeFixers_Assembly_Has_No_Reference_To_Rules_Type()
    {
        var rulesReferences = RulesDecouplingVerifier.FindRulesTypeReferences(
            typeof(AwaitAssertionCodeFixProvider).Assembly, "TUnit.Assertions.Analyzers");

        await Assert.That(rulesReferences)
            .IsEmpty()
            .Because("TUnit.Assertions.Analyzers.CodeFixers must not reference TUnit.Assertions.Analyzers.Rules at runtime - " +
                     "use DiagnosticIds constants instead (see issue #6157)");
    }
}
