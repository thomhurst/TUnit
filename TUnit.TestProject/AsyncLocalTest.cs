using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Logging;

namespace TUnit.TestProject;

public class AsyncLocalTest
{
    private readonly AsyncLocal<string> _asyncLocalValue = new();

    [Before(Test)]
    public void Before(TestContext context)
    {
        context.FlowAsyncLocalValues();
        _asyncLocalValue.Value = "123";
    }

    [After(Test)]
    public void After()
    {
        TestContext.Current?.GetDefaultLogger().LogInformation($"Message: {_asyncLocalValue.Value}");
    }

    [Test]
    public async Task Test_method()
    {
        await Assert.That(_asyncLocalValue.Value).IsEqualTo("123");
    }
}