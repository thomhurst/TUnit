using TUnit.TestProject.Attributes;
using TUnit.TestProject.Library;

namespace TUnit.TestProject.Bugs.Issue4583;

/// <summary>
/// Reproduction test for https://github.com/thomhurst/TUnit/issues/4583
/// Tests that [Before(TestSession)] hooks in REFERENCED LIBRARY projects are executed.
/// This is different from issue #4541 which tested hooks in the same project.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class LibraryTestSessionHookTests
{
    [Test]
    public async Task Verify_TestSession_Hook_In_Referenced_Library_Executed()
    {
        await Assert.That(LibraryTestSessionHooks.BeforeTestSessionWasExecuted).IsTrue()
            .Because("[Before(TestSession)] hook in referenced library project should have executed");
    }
}
