using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;

namespace TUnit.Engine.Services;

public class HookMessagePublisher(IExtension extension) : IDataProducer
{
    public async Task Discover(ExecuteRequestContext context, string displayName, StaticHookMethod hookMethod)
    {
        TestNodeUid testNodeUid =
            $"{hookMethod.Assembly.FullName}_{hookMethod.ClassType.FullName}_{hookMethod.MethodInfo.Name}_{displayName}";

        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid,
            new TestNode
            {
                Uid = testNodeUid,
                DisplayName = displayName,
                Properties = new PropertyBag(
                    DiscoveredTestNodeStateProperty.CachedInstance,
                    new TestFileLocationProperty(hookMethod.FilePath,
                        new LinePositionSpan(new LinePosition(hookMethod.LineNumber, 0),
                            new LinePosition(hookMethod.LineNumber, 0))),
                    new TestMethodIdentifierProperty
                    (
                        hookMethod.Assembly.FullName!, hookMethod.ClassType.Namespace!,
                        hookMethod.ClassType.Name, hookMethod.Name,
                        hookMethod.MethodInfo.GetParameters().Select(x => x.ParameterType.FullName!).ToArray(),
                        hookMethod.MethodInfo.ReturnType.FullName!
                    )
                ),
            }));
    }

    public async Task Push(ExecuteRequestContext context, string displayName, StaticHookMethod hookMethod, Func<Task> func)
    {
        await PublishAsync(context, displayName, hookMethod, DateTimeOffset.Now, DateTimeOffset.Now, InProgressTestNodeStateProperty.CachedInstance);

        var start = DateTimeOffset.Now;
        DateTimeOffset end;
        
        try
        {
            await func();
            end = DateTime.UtcNow;
            await PublishAsync(context, displayName, hookMethod, start, end, PassedTestNodeStateProperty.CachedInstance);
        }
        catch (Exception e)
        {
            end = DateTime.UtcNow;
            await PublishAsync(context, displayName, hookMethod, start, end, new FailedTestNodeStateProperty(e));
            throw;
        }
    }

    private async Task PublishAsync(ExecuteRequestContext context, string displayName, StaticHookMethod hookMethod, DateTimeOffset start, DateTimeOffset end, TestNodeStateProperty stateProperty)
    {
        TestNodeUid testNodeUid = $"{hookMethod.Assembly.FullName}_{hookMethod.ClassType.FullName}_{hookMethod.MethodInfo.Name}_{displayName}";
        
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid,
            new TestNode
            {
                Uid = testNodeUid,
                DisplayName = displayName,
                Properties = new PropertyBag(
                    new TestFileLocationProperty(hookMethod.FilePath,
                        new LinePositionSpan(new LinePosition(hookMethod.LineNumber, 0),
                            new LinePosition(hookMethod.LineNumber, 0))),
                    new TimingProperty(new TimingInfo(start, end, end - start)),
                    new TestMethodIdentifierProperty
                    (
                        hookMethod.Assembly.FullName!, hookMethod.ClassType.Namespace!,
                        hookMethod.ClassType.Name, hookMethod.Name,
                        hookMethod.MethodInfo.GetParameters().Select(x => x.ParameterType.FullName!).ToArray(),
                        hookMethod.MethodInfo.ReturnType.FullName!
                    ),
                    stateProperty
                )
            }));
    }

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public Type[] DataTypesProduced { get; } = [ typeof(TestNodeUpdateMessage) ];
}