using System.Threading;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6192;

// Regression test for #6192: lifecycle hooks must run BEFORE the test class is constructed.
// Previously the constructor ran before Before(Assembly)/Before(Class) (and their BeforeEvery
// counterparts), so the assertions in the constructors below would throw.

public class Bug6192HookOrderProbe
{
    // volatile to make the happens-before intent explicit (the framework's task-caching already
    // provides the edge between hook completion and the constructor that reads these).
    public static volatile bool BeforeEveryClassRan;
    public static volatile bool BeforeEveryAssemblyRan;
    public static volatile bool BeforeClassRan;
    public static volatile bool BeforeAssemblyRan;

    // AsyncLocal set inside a [Before(Class)] hook (which opts in via context.AddAsyncLocalValues()).
    // Its value must flow into the constructor, which proves ClassContext.RestoreExecutionContext()
    // runs before construction in the pre-construction hook gate (#6192 / review feedback).
    public static readonly AsyncLocal<string?> ClassHookAsyncLocal = new();

    [BeforeEvery(Class)]
    public static void BeforeEveryClass(ClassHookContext context)
    {
        if (context.ClassType == typeof(Bug6192Tests) || context.ClassType == typeof(Bug6192RetryTests))
        {
            BeforeEveryClassRan = true;
        }
    }

    [BeforeEvery(Assembly)]
    public static void BeforeEveryAssembly(AssemblyHookContext context)
    {
        if (context.Assembly == typeof(Bug6192Tests).Assembly)
        {
            BeforeEveryAssemblyRan = true;
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class Bug6192Tests
{
    // Non-BeforeEvery hooks go through the same cache path; assert they precede the constructor too.
    [Before(Assembly)]
    public static void BeforeAssembly() => Bug6192HookOrderProbe.BeforeAssemblyRan = true;

    [Before(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        Bug6192HookOrderProbe.BeforeClassRan = true;
        Bug6192HookOrderProbe.ClassHookAsyncLocal.Value = "set-by-before-class";
        // Opt in to flowing this AsyncLocal forward (otherwise TUnit doesn't capture it).
        context.AddAsyncLocalValues();
    }

    public Bug6192Tests()
    {
        // The constructor must run AFTER the lifecycle hooks (issue #6192).
        if (!Bug6192HookOrderProbe.BeforeEveryAssemblyRan)
        {
            throw new InvalidOperationException("Constructor ran before [BeforeEvery(Assembly)] hook");
        }

        if (!Bug6192HookOrderProbe.BeforeEveryClassRan)
        {
            throw new InvalidOperationException("Constructor ran before [BeforeEvery(Class)] hook");
        }

        if (!Bug6192HookOrderProbe.BeforeAssemblyRan)
        {
            throw new InvalidOperationException("Constructor ran before [Before(Assembly)] hook");
        }

        if (!Bug6192HookOrderProbe.BeforeClassRan)
        {
            throw new InvalidOperationException("Constructor ran before [Before(Class)] hook");
        }

        // AsyncLocals captured by a class-level hook must flow into the constructor, otherwise the
        // hook ran but ClassContext.RestoreExecutionContext() did not run before construction.
        if (Bug6192HookOrderProbe.ClassHookAsyncLocal.Value != "set-by-before-class")
        {
            throw new InvalidOperationException("Before(Class) AsyncLocal did not flow into the constructor");
        }
    }

    [Test]
    public void TestA()
    {
    }

    [Test]
    public void TestB()
    {
    }
}

// Same contract must hold on the retry code path (ExecuteTestLifecycleAsync), which has its own
// pre-construction hook gate separate from the no-retry fast path.
[EngineTest(ExpectedResult.Pass)]
public class Bug6192RetryTests
{
    public static readonly AsyncLocal<string?> ClassHookAsyncLocal = new();

    [Before(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        ClassHookAsyncLocal.Value = "set-by-before-class-retry";
        context.AddAsyncLocalValues();
    }

    public Bug6192RetryTests()
    {
        if (!Bug6192HookOrderProbe.BeforeEveryClassRan)
        {
            throw new InvalidOperationException("Constructor ran before [BeforeEvery(Class)] hook (retry path)");
        }

        if (ClassHookAsyncLocal.Value != "set-by-before-class-retry")
        {
            throw new InvalidOperationException("Before(Class) AsyncLocal did not flow into the constructor (retry path)");
        }
    }

    [Retry(1)]
    [Test]
    public void RetriedTest()
    {
    }
}
