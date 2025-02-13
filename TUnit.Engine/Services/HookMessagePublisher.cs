using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core.Hooks;

namespace TUnit.Engine.Services;

public class HookMessagePublisher(IExtension extension, IMessageBus messageBus) : IDataProducer, IHookMessagePublisher
{
    public async Task Discover(string sessionId, string displayName, StaticHookMethod hookMethod)
    {
        TestNodeUid testNodeUid =
            $"{hookMethod.Assembly.FullName}_{hookMethod.ClassType.FullName}_{hookMethod.MethodInfo.Name}_{displayName}";

        await messageBus.PublishAsync(this, new TestNodeUpdateMessage(new SessionUid(sessionId),
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
                        hookMethod.MethodInfo.Parameters.Select(x => x.Type.FullName!).ToArray(),
                        hookMethod.MethodInfo.ReturnType.FullName!
                    )
                ),
            }));
    }

    public async Task Push(string sessionId, string displayName, StaticHookMethod hookMethod, Func<Task> func)
    {
        await PublishAsync(sessionId, displayName, hookMethod, DateTimeOffset.Now, DateTimeOffset.Now, InProgressTestNodeStateProperty.CachedInstance);

        var start = DateTimeOffset.Now;
        DateTimeOffset end;
        
        try
        {
            await func();
            end = DateTime.UtcNow;
            await PublishAsync(sessionId, displayName, hookMethod, start, end, PassedTestNodeStateProperty.CachedInstance);
        }
        catch (Exception e)
        {
            end = DateTime.UtcNow;
            await PublishAsync(sessionId, displayName, hookMethod, start, end, new FailedTestNodeStateProperty(e));
            throw;
        }
    }

    private async Task PublishAsync(string sessionId, string displayName, StaticHookMethod hookMethod, DateTimeOffset start, DateTimeOffset end, TestNodeStateProperty stateProperty)
    {
        TestNodeUid testNodeUid = $"{hookMethod.Assembly.FullName}_{hookMethod.ClassType.FullName}_{hookMethod.MethodInfo.Name}_{displayName}";
        
        await messageBus.PublishAsync(this, new TestNodeUpdateMessage(new SessionUid(sessionId),
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
                        hookMethod.MethodInfo.Parameters.Select(x => x.Type.FullName!).ToArray(),
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