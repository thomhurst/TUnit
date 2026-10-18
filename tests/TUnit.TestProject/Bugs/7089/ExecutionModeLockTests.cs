using TUnit.Core.Services;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7089;

/// <summary>
/// Regression test for #7089: the generated module initializer of an assembly that loads mid-run
/// writes <c>SourceRegistrar.IsEnabled = true</c>. That write must not change the execution mode
/// the engine already selected.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ExecutionModeLockTests
{
    [Test]
    public async Task Late_IsEnabled_Write_Does_Not_Change_Selected_Mode()
    {
        // The engine has already selected and locked the mode by the time a test runs. Capture it
        // rather than inferring it from the command line, which is only one way to select it.
        var selectedSourceGeneration = SourceRegistrar.IsEnabled;
        var sourceGenerationAvailable = ModeDetector.IsSourceGenerationAvailable;

        try
        {
            // What a late-loading assembly's generated module initializer does. The write must not
            // change the selected mode, but it must still record that source generation is
            // available, so a later engine instance in the process can fall back to it.
            SourceRegistrar.IsEnabled = true;
            await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(selectedSourceGeneration);
            await Assert.That(ModeDetector.IsSourceGenerationAvailable).IsTrue();

            SourceRegistrar.IsEnabled = false;
            await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(selectedSourceGeneration);
            await Assert.That(ModeDetector.IsSourceGenerationAvailable).IsFalse();
        }
        finally
        {
            // Without the fix the writes above stick. Once the mode is locked this write only
            // restores the availability signal; the selected mode stays as it is.
            SourceRegistrar.IsEnabled = sourceGenerationAvailable;
        }
    }
}
