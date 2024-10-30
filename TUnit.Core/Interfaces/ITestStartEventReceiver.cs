namespace TUnit.Core.Interfaces;

public interface ITestStartEventReceiver : IEventReceiver
{
    ValueTask OnTestStart(BeforeTestContext beforeTestContext);
}