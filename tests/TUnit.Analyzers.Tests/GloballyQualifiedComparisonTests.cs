using AnalyzerTypeExtensions = TUnit.Analyzers.Extensions.TypeExtensions;
using Assert = TUnit.Assertions.Assert;
using TUnit.Tests.Shared;

namespace TUnit.Analyzers.Tests;

/// <summary>
/// <see cref="AnalyzerTypeExtensions.IsGloballyQualified"/> and
/// <see cref="AnalyzerTypeExtensions.IsGloballyQualifiedNonGeneric"/> reject on the symbol name before
/// building a display string. They must always agree with comparing the display string directly.
/// </summary>
public class GloballyQualifiedComparisonTests
{
    [Test]
    public async Task Fast_Comparison_Matches_Display_String_Comparison()
    {
        var mismatches = GloballyQualifiedComparisonCases.FindMismatches(
            typeof(TUnit.Core.TestAttribute).Assembly.Location,
            AnalyzerTypeExtensions.GloballyQualified,
            AnalyzerTypeExtensions.IsGloballyQualified,
            AnalyzerTypeExtensions.GloballyQualifiedNonGeneric,
            AnalyzerTypeExtensions.IsGloballyQualifiedNonGeneric,
            out var comparisons);

        await Assert.That(comparisons).IsGreaterThan(10_000);
        await Assert.That(mismatches).IsEmpty();
    }
}
