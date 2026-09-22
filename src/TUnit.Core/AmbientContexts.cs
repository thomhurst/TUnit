namespace TUnit.Core;

/// <summary>
/// Holds the ambient test/class/assembly/session/discovery contexts in a single AsyncLocal.
/// </summary>
/// <remarks>
/// <para>
/// These five contexts used to live in five separate AsyncLocals whose setters cascade outward
/// (setting the test context also sets its class, assembly, session and discovery contexts). Every
/// AsyncLocal write that changes a value allocates a new ExecutionContext plus a copied value map,
/// so restoring a test's contexts cost several of each per test. Keeping them in one immutable
/// snapshot turns each cascading assignment into a single AsyncLocal write.
/// </para>
/// <para>
/// Semantics are unchanged: each <c>Current</c> setter still replaces its own slot and cascades to
/// the enclosing contexts exactly as before, and ExecutionContext capture/restore carries all five
/// together, as it did when they were separate AsyncLocals.
/// </para>
/// </remarks>
internal sealed class AmbientContexts
{
    private static readonly AsyncLocal<AmbientContexts?> CurrentContexts = new();

    private AmbientContexts(
        TestContext? test,
        ClassHookContext? @class,
        AssemblyHookContext? assembly,
        TestSessionContext? session,
        TestDiscoveryContext? discovery)
    {
        Test = test;
        Class = @class;
        Assembly = assembly;
        Session = session;
        Discovery = discovery;
    }

    public TestContext? Test { get; }
    public ClassHookContext? Class { get; }
    public AssemblyHookContext? Assembly { get; }
    public TestSessionContext? Session { get; }
    public TestDiscoveryContext? Discovery { get; }

    public static AmbientContexts? Current => CurrentContexts.Value;

    /// <summary>Sets the test context only (no cascade).</summary>
    public static void SetTestOnly(TestContext? test)
    {
        var current = CurrentContexts.Value;
        Set(current, test, current?.Class, current?.Assembly, current?.Session, current?.Discovery);
    }

    /// <summary>Sets the test context and cascades to its class, assembly, session and discovery contexts.</summary>
    public static void SetTest(TestContext? test)
    {
        var @class = test?.ClassContext;
        var assembly = @class?.AssemblyContext;
        var session = assembly?.TestSessionContext;
        Set(CurrentContexts.Value, test, @class, assembly, session, session?.TestDiscoveryContext);
    }

    /// <summary>Sets the class context and cascades to its assembly, session and discovery contexts.</summary>
    public static void SetClass(ClassHookContext? @class)
    {
        var current = CurrentContexts.Value;
        var assembly = @class?.AssemblyContext;
        var session = assembly?.TestSessionContext;
        Set(current, current?.Test, @class, assembly, session, session?.TestDiscoveryContext);
    }

    /// <summary>Sets the assembly context and cascades to its session and discovery contexts.</summary>
    public static void SetAssembly(AssemblyHookContext? assembly)
    {
        var current = CurrentContexts.Value;
        var session = assembly?.TestSessionContext;
        Set(current, current?.Test, current?.Class, assembly, session, session?.TestDiscoveryContext);
    }

    /// <summary>Sets the session context and cascades to its discovery context.</summary>
    public static void SetSession(TestSessionContext? session)
    {
        var current = CurrentContexts.Value;
        Set(current, current?.Test, current?.Class, current?.Assembly, session, session?.TestDiscoveryContext);
    }

    /// <summary>Sets the discovery context only.</summary>
    public static void SetDiscovery(TestDiscoveryContext? discovery)
    {
        var current = CurrentContexts.Value;
        Set(current, current?.Test, current?.Class, current?.Assembly, current?.Session, discovery);
    }

    private static void Set(
        AmbientContexts? current,
        TestContext? test,
        ClassHookContext? @class,
        AssemblyHookContext? assembly,
        TestSessionContext? session,
        TestDiscoveryContext? discovery)
    {
        if (current is null)
        {
            if (test is null && @class is null && assembly is null && session is null && discovery is null)
            {
                return;
            }
        }
        else if (ReferenceEquals(current.Test, test)
                 && ReferenceEquals(current.Class, @class)
                 && ReferenceEquals(current.Assembly, assembly)
                 && ReferenceEquals(current.Session, session)
                 && ReferenceEquals(current.Discovery, discovery))
        {
            // Unchanged: skip the write (and its ExecutionContext allocation) entirely.
            return;
        }

        CurrentContexts.Value = new AmbientContexts(test, @class, assembly, session, discovery);
    }
}
