using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

internal class ContextManager(string sessionId, string? testFilter)
{
    [field: AllowNull, MaybeNull]
    public BeforeTestDiscoveryContext BeforeTestDiscoveryContext => field ??= new BeforeTestDiscoveryContext
    {
        TestFilter = testFilter
    };

    [field: AllowNull, MaybeNull]
    public TestDiscoveryContext AfterTestDiscoveryContext => field ??= new TestDiscoveryContext(BeforeTestDiscoveryContext)
    {
        TestFilter = testFilter
    };

    [field: AllowNull, MaybeNull]
    public TestSessionContext TestSessionContext => field ??= new TestSessionContext(AfterTestDiscoveryContext)
    {
        TestFilter = testFilter,
        Id = sessionId
    };

    private readonly GetOnlyDictionary<Assembly, AssemblyHookContext> _assemblyHookContexts = new ();
    private readonly GetOnlyDictionary<Type, ClassHookContext> _classHookContexts = new ();

    public AssemblyHookContext GetAssemblyHookContext(Assembly assembly) =>
        _assemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext(TestSessionContext)
        {
            Assembly = assembly
        });

    public ClassHookContext GetClassHookContext(Type type) =>
        _classHookContexts.GetOrAdd(type, _ => new ClassHookContext(GetAssemblyHookContext(type.Assembly))
        {
            ClassType = type
        });
}
