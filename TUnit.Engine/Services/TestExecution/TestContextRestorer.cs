using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Restores execution context for AsyncLocal support.
/// Single Responsibility: Execution context management.
/// </summary>
internal sealed class TestContextRestorer
{
    public void RestoreContext(AbstractExecutableTest test)
    {
        test.Context.RestoreExecutionContext();
        test.Context.ClassContext?.RestoreExecutionContext();
        test.Context.ClassContext?.AssemblyContext?.RestoreExecutionContext();
        test.Context.ClassContext?.AssemblyContext?.TestSessionContext?.RestoreExecutionContext();
    }
}