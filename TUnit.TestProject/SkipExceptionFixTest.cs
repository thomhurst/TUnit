using TUnit.Core.Exceptions;

namespace TUnit.TestProject;

public class SkipExceptionFixTest
{
    [Test]
    public void ThrowSkipTestException_ShouldMarkAsSkipped()
    {
        throw new SkipTestException("This test should be skipped, not failed");
    }
}