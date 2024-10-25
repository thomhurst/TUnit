namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IClassHookSource
{
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeClassHooks();
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterClassHooks();
    
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeEveryClassHooks();
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterEveryClassHooks();
}