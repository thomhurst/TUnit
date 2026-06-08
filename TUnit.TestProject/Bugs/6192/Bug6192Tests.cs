using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6192;

// Regression test for #6192: lifecycle hooks must run BEFORE the test class is constructed.
// Previously the constructor ran before BeforeEvery(Assembly)/BeforeEvery(Class), so the
// assertions in the constructor below would throw.

public class Bug6192HookOrderProbe
{
    public static bool BeforeEveryClassRan;
    public static bool BeforeEveryAssemblyRan;

    [BeforeEvery(Class)]
    public static void BeforeEveryClass(ClassHookContext context)
    {
        if (context.ClassType == typeof(Bug6192Tests))
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
