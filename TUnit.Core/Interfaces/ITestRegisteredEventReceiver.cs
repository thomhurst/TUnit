namespace TUnit.Core.Interfaces;

public interface ITestRegisteredEventReceiver : IEventReceiver
{
    public ValueTask OnTestRegistered(TestRegisteredContext context);
}