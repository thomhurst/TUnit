namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IEveryClassHookSource
{
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterHooks();
}