namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IAssemblyHookSource
{
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeAssemblyHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterAssemblyHooks(string sessionId);
    
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryAssemblyHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryAssemblyHooks(string sessionId);
}