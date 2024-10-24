namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IAssemblyHookSource
{
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeAssemblyHooks();
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterAssemblyHooks();
    
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryAssemblyHooks();
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryAssemblyHooks();
}