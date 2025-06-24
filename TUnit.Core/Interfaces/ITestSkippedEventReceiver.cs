namespace TUnit.Core.Interfaces;

public interface ITestSkippedEventReceiver : IEventReceiver
{
    ValueTask OnTestSkipped(TestContext context);
}