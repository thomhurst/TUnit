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
#if NET
        var context = test.Context;
        var classContext = context.ClassContext;
        var assemblyContext = classContext.AssemblyContext;
        var sessionContext = assemblyContext.TestSessionContext;

        // Common case: no hook captured AsyncLocal values anywhere in the chain, so restoring just
        // re-points the ambient contexts at this test. Setting TestContext.Current cascades to the
        // same class/assembly/session contexts in one AsyncLocal write, and yields exactly the state
        // the step-by-step restore below produces.
        if (context.ExecutionContext is null
            && classContext.ExecutionContext is null
            && assemblyContext.ExecutionContext is null
            && sessionContext.ExecutionContext is null)
        {
            TestContext.Current = context;
            return;
        }
#endif

        test.Context.RestoreExecutionContext();
        test.Context.ClassContext?.RestoreExecutionContext();
        test.Context.ClassContext?.AssemblyContext?.RestoreExecutionContext();
        test.Context.ClassContext?.AssemblyContext?.TestSessionContext?.RestoreExecutionContext();
    }
}