using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;

namespace TUnit.Engine.Services;

public class HookMessagePublisher : IDataProducer
{
    private readonly IExtension _extension;

    public HookMessagePublisher(IExtension extension)
    {
        _extension = extension;
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
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid,
            new TestNode
            {
                Uid =
                    $"{hookMethod.Assembly.FullName}_{hookMethod.ClassType.FullName}_{hookMethod.MethodInfo.Name}_{displayName}",
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
        return _extension.IsEnabledAsync();
    }

    public string Uid => _extension.Uid;

    public string Version => _extension.Version;

    public string DisplayName => _extension.DisplayName;

    public string Description => _extension.Description;

    public Type[] DataTypesProduced { get; } = [ typeof(TestNodeUpdateMessage) ];
}