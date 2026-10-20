using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// The TRX className of a test on a generic class is the generic type definition's full name
/// (<c>Ns.Class`1</c>) in both modes, so every instantiation groups under one class (#7151).
/// Reflection mode used to report the closed type with assembly-qualified type arguments.
/// </summary>
public class Issue7151Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("GenericClassSameTypeRows", 3)]
    [Arguments("GenericClassMixedTypeRows", 4)]
    [Arguments("GenericClassMixedTypeMethodRows", 3)]
    [Arguments("GenerateGenericTestWithMethodRows", 2)]
    public async Task Generic_Class_Trx_ClassName_Is_The_Generic_Type_Definition(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7106/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.TestDefinitions.Select(x => x.TestMethod.ClassName).Distinct().ToArray()
                    .ShouldBe(new[] { $"TUnit.TestProject.Bugs._7106.{className}`1" })
            ]);
    }
}
