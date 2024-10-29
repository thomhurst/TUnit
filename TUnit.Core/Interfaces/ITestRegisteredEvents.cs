namespace TUnit.Core.Interfaces;

public interface ITestRegisteredEvents
{
    public ValueTask OnTestRegistered(TestRegisterContext context);
}