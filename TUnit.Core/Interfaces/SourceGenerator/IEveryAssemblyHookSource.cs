namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IEveryAssemblyHookSource
{
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterHooks();
}