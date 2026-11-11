using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class JitWarmupTests
{
    [Test]
    [Arguments("TUnit.Engine.TestExecutor", "ExecuteAsync", "TUnit.Engine.TestExecutor::ExecuteAsync")]
    [Arguments("TUnit.Core.TestContext", ".ctor", "TUnit.Core.TestContext::.ctor")]
    [Arguments("TUnit.Engine.Building.TestBuilder+TestData", "get_Arguments", "TUnit.Engine.Building.TestBuilder+TestData::get_Arguments")]
    // Lambdas, local functions and state machines map to the method that declares them.
    [Arguments("TUnit.Engine.TestExecutor", "<ExecuteAsync>g__Local|3_0", "TUnit.Engine.TestExecutor::ExecuteAsync")]
    [Arguments("TUnit.Engine.TestExecutor+<>c", "<ExecuteAsync>b__3_0", "TUnit.Engine.TestExecutor::ExecuteAsync")]
    [Arguments("TUnit.Engine.TestExecutor+<>c__DisplayClass3_0", "<ExecuteAsync>b__1", "TUnit.Engine.TestExecutor::ExecuteAsync")]
    [Arguments("TUnit.Engine.TestExecutor+<ExecuteAsync>d__3", "MoveNext", "TUnit.Engine.TestExecutor::ExecuteAsync")]
    [Arguments("TUnit.Engine.TestExecutor+<<ExecuteAsync>g__Local|3_0>d", "MoveNext", "TUnit.Engine.TestExecutor::ExecuteAsync")]
    [Arguments("TUnit.Core.GlobalContext+<>c", "<.cctor>b__2_0", "TUnit.Core.GlobalContext::.cctor")]
    public async Task ToEntry_Maps_Compiled_Method_To_Source_Method(string typeName, string methodName, string expected)
    {
        await Assert.That(JitWarmup.ToEntry(typeName, methodName)).IsEqualTo(expected);
    }

    [Test]
    // Other assemblies, including generated test sources.
    [Arguments("System.String", "Concat")]
    [Arguments("TUnit.Generated.MyTests__TestSource", ".cctor")]
    // Generic instantiations cannot be prepared from an open type.
    [Arguments("TUnit.Core.TestEntry`1[System.__Canon]", ".ctor")]
    // Closure plumbing.
    [Arguments("TUnit.Engine.TestExecutor+<>c", ".ctor")]
    // The warm-up itself.
    [Arguments("TUnit.Engine.Helpers.JitWarmup", "Start")]
    [Arguments("TUnit.Engine.Helpers.JitWarmup+Recorder", "OnEventWritten")]
    public async Task ToEntry_Skips_Methods_That_Cannot_Be_Warmed(string typeName, string methodName)
    {
        await Assert.That(JitWarmup.ToEntry(typeName, methodName)).IsNull();
    }
}
