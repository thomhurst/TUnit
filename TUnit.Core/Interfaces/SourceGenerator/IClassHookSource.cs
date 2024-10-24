namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IClassHookSource
{
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterHooks();
    
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeEveryHooks();
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterEveryHooks();
}