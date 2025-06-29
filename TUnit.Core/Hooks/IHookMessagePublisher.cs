namespace TUnit.Core.Hooks;

public interface IHookMessagePublisher
{
    Task Discover(string sessionId, string displayName, StaticHookMethod hookMethod);
    Task Push(string sessionId, string displayName, StaticHookMethod hookMethod, Func<Task> func);
}
