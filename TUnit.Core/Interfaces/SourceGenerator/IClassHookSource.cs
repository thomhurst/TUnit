namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IClassHookSource
{
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeClassHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterClassHooks(string sessionId);
    
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeEveryClassHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterEveryClassHooks(string sessionId);
}