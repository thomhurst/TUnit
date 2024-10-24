namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IAssemblyHookSource
{
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterHooks();
    
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryHooks();
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryHooks();
}