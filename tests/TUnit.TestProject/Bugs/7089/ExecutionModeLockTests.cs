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
        var expectedSourceGeneration = !Environment.GetCommandLineArgs().Contains("--reflection");

        await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(expectedSourceGeneration);

        try
        {
            // What a late-loading assembly's generated module initializer does.
            SourceRegistrar.IsEnabled = true;
            await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(expectedSourceGeneration);

            SourceRegistrar.IsEnabled = false;
            await Assert.That(SourceRegistrar.IsEnabled).IsEqualTo(expectedSourceGeneration);
        }
        finally
        {
            // Without the fix the writes above stick; put the value back for the rest of the run.
            SourceRegistrar.IsEnabled = expectedSourceGeneration;
        }
    }
}
