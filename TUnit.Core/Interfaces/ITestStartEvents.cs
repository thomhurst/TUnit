namespace TUnit.Core.Interfaces;

public interface ITestStartEvents
{
    Task OnTestStart(TestContext testContext);
}