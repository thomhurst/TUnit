namespace TUnit.Core.Interfaces;

public interface ITestEndEventReceiver : IEventReceiver
{
    ValueTask OnTestEnd(AfterTestContext afterTestContext);
}