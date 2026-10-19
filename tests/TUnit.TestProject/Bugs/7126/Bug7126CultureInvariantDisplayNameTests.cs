using System.Globalization;
using System.Runtime.CompilerServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7126;

/// <summary>
/// Repro for https://github.com/thomhurst/TUnit/issues/7126 — numeric arguments in display names
/// were formatted with the current culture, so under a decimal-comma culture (sv-SE, de-DE, ...)
/// <c>[Arguments(1.5, -2)]</c> became <c>Test(1,5, −2)</c>: ambiguous against the argument separator
/// and different from machine to machine.
/// TUnit.Engine.Tests sets <c>TUNIT_BUG_7126_CULTURE</c> to run this project under such a culture.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Bug7126CultureInvariantDisplayNameTests
{
    internal const string CultureEnvironmentVariable = "TUNIT_BUG_7126_CULTURE";

    // Runs for the whole TestProject module, but does nothing unless TUnit.Engine.Tests sets the env var,
    // so other tests in this project are unaffected.
    [ModuleInitializer]
    internal static void ApplyRequestedCulture()
    {
        if (Environment.GetEnvironmentVariable(CultureEnvironmentVariable) is { Length: > 0 } cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }

    [Test]
    [Arguments(1.5, -2, 2.25f, 3.75)]
    public async Task NumericArguments(double value, int count, float ratio, decimal amount)
    {
        if (Environment.GetEnvironmentVariable(CultureEnvironmentVariable) is { Length: > 0 } cultureName)
        {
            await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo(cultureName);
        }

        await Assert.That(TestContext.Current!.Metadata.DisplayName)
            .IsEqualTo("NumericArguments(1.5, -2, 2.25, 3.75)");
    }
}
