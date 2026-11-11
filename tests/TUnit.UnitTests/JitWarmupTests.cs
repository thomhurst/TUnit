using System.Reflection;
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
    [Arguments("TUnit.Engine.TestExecutor+<>c__DisplayClass3_0+<<ExecuteAsync>b__0>d", "MoveNext", "TUnit.Engine.TestExecutor::ExecuteAsync")]
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

    // Renames and removals only make entries stop matching, so this is what notices the list
    // going stale. Re-record it with TUNIT_JIT_WARMUP_RECORD when this fails.
    [Test]
    public async Task Embedded_Entries_Still_Match_The_Code()
    {
        var membersByType = new Dictionary<Type, JitWarmup.TypeMembers>();
        var entries = JitWarmup.ReadEntries().ToList();
        var resolved = entries.Count(entry => JitWarmup.ResolveEntry(entry, membersByType).Any());

        await Assert.That(entries.Count).IsGreaterThan(0);
        await Assert.That((double) resolved / entries.Count).IsGreaterThanOrEqualTo(0.9);
    }

    [Test]
    public async Task FindMethods_Includes_State_Machines_Nested_In_Closures()
    {
        // Local() captures a local, so its iterator state machine is nested inside the closure class.
        var stateMachineMethods = GeneratedTypes(typeof(Sample))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method.Name == "MoveNext")
            .ToList();

        var found = JitWarmup.FindMethods(new JitWarmup.TypeMembers(typeof(Sample)), nameof(Sample.Numbers)).ToList();

        await Assert.That(stateMachineMethods).IsNotEmpty();
        await Assert.That(stateMachineMethods.All(found.Contains)).IsTrue();
        await Assert.That(found).Contains(typeof(Sample).GetMethod(nameof(Sample.Numbers))!);
    }

    private static IEnumerable<Type> GeneratedTypes(Type type)
        => type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(nested => GeneratedTypes(nested).Prepend(nested));

    private static class Sample
    {
        public static List<int> Numbers(int seed)
        {
            var captured = seed;
            return Local().ToList();

            IEnumerable<int> Local()
            {
                yield return captured;
                yield return captured + 1;
            }
        }
    }
}
