using TUnit.Analyzers.CodeFixers;
using TUnit.Tests.Shared;

namespace TUnit.Analyzers.Tests;

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
            typeof(MSTestMigrationCodeFixProvider).Assembly, "TUnit.Analyzers");

        await TUnit.Assertions.Assert.That(rulesReferences)
            .IsEmpty()
            .Because("TUnit.Analyzers.CodeFixers must not reference TUnit.Analyzers.Rules at runtime - " +
                     "use DiagnosticIds constants instead (see issue #6157)");
    }
}
