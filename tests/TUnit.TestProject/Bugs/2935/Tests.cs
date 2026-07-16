using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2935;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    private string _taskHook = string.Empty;
    private string _valueTaskHook = string.Empty;

    [Before(Test)]
    public async Task BeforeTest_Task()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        _taskHook = "Task Hook Executed";
    }

    [Before(Test)]
    public async ValueTask BeforeTest_ValueTask()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        _valueTaskHook = "ValueTask Hook Executed";
    }

    [Test]
    public async Task MyTest()
    {
        await Assert.That(_taskHook).IsEqualTo("Task Hook Executed");
        await Assert.That(_valueTaskHook).IsEqualTo("ValueTask Hook Executed");
    }
}
