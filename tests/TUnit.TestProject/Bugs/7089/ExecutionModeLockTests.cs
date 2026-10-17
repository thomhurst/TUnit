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

        try
        {
            // What a late-loading assembly's generated module initializer does.
            SourceRegistrar.IsEnabled = true;
            await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(selectedSourceGeneration);

            SourceRegistrar.IsEnabled = false;
            await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(selectedSourceGeneration);
        }
        finally
        {
            // Without the fix the writes above stick; put the value back for the rest of the run.
            SourceRegistrar.IsEnabled = selectedSourceGeneration;
        }
    }
}
