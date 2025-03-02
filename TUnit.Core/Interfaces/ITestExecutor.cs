namespace TUnit.Core.Interfaces;

public interface ITestExecutor
{
    ValueTask ExecuteTest(TestContext context, Func<ValueTask> action);
}