namespace TUnit.Core.Interfaces;

public interface ITestEndEvent
{
    ValueTask OnTestEnd(TestContext testContext);
}