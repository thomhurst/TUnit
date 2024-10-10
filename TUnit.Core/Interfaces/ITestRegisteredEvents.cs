namespace TUnit.Core.Interfaces;

public interface ITestRegisteredEvents
{
    public Task OnTestRegistered(TestContext testContext);
}