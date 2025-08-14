using TUnit.Core.Exceptions;

namespace TUnit.TestProject;

public class SkipExceptionFixTest
{
    [Test]
    public async Task ThrowSkipTestException_ShouldMarkAsSkipped()
    {
        await Task.Yield();
        throw new SkipTestException("This test should be skipped, not failed");
    }
}