namespace TUnit.Core.Interfaces;

public interface ITestExecutor
{
    Task ExecuteTest(TestContext context, Func<Task> action);
}