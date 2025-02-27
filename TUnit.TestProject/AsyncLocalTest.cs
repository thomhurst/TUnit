using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Logging;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[SkipNetFramework("ExecutionContext.Restore is not supported on .NET Framework")]
public class AsyncLocalTest
{
    private readonly AsyncLocal<string> _asyncLocalValue = new();

    [Before(Test)]
    public void Before(TestContext context)
    {
        _asyncLocalValue.Value = "123";
        context.AddAsyncLocalValues();
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