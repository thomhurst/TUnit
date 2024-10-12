namespace TUnit.Core.Interfaces;

public interface ITestStartEvent
{
    ValueTask OnTestStart(BeforeTestContext beforeTestContext);
}