using TUnit.Core.Exceptions;

namespace TUnit.TestProject;

public class SkipExceptionTests
{
    [Test]
    public async Task TestA()
    {
        await Task.Yield();
        throw new SkipTestException("i should be skipped");
    }
}