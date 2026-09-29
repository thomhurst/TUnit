using System.Collections.Concurrent;

namespace TUnit.TestProject;

// Hooks with equal Order run in registration order. The first class declares only a BeforeEvery(Assembly)
// hook, while the second also declares a Before(Class) hook. Registration must follow declaration order,
// as it does in reflection mode, and not move the second class ahead because it has a Before hook.
public static class HookRegistrationOrderFirstHooks
{
    [BeforeEvery(Assembly)]
    public static void RecordFirst(AssemblyHookContext context)
    {
        HookRegistrationOrderAcrossClassesTests.Record(context, "First");
    }
}

public static class HookRegistrationOrderSecondHooks
{
    [Before(Class)]
    public static void UnusedClassHook()
    {
    }

    [BeforeEvery(Assembly)]
    public static void RecordSecond(AssemblyHookContext context)
    {
        HookRegistrationOrderAcrossClassesTests.Record(context, "Second");
    }
}

public class HookRegistrationOrderAcrossClassesTests
{
    private static readonly ConcurrentQueue<string> Executed = new();

    // A host can run several test sessions in one process, re-running the assembly hooks each time.
    // Session hooks run before assembly hooks, so clearing here scopes the recorded entries to one session.
    [Before(TestSession)]
    public static void ResetRecordedHooks()
    {
        while (Executed.TryDequeue(out _))
        {
        }
    }

    internal static void Record(AssemblyHookContext context, string name)
    {
        if (context.Assembly == typeof(HookRegistrationOrderAcrossClassesTests).Assembly)
        {
            Executed.Enqueue(name);
        }
    }

    [Test]
    public async Task EqualOrderHooks_InDifferentClasses_RunInDeclarationOrder()
    {
        await Assert.That(Executed).IsEquivalentTo(["First", "Second"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
}
